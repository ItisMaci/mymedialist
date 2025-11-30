using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MyMediaList.System
{
    public sealed class User : Atom, IAtom
    {
        private string? _UserName = null;
        private bool _New;
        private string? _PasswordHash = null;

        public User(Session? session = null)
        {
            _EditingSession = session;
            _New = true;
        }

        public static User Get(string userName, Session? session = null)
        {
            try
            {
                using var connection = Database.Database.GetConnection();
                connection.Open();

                string sql = @"
                    SELECT username 
                    FROM users 
                    WHERE username = @username";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@username", userName);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    throw new InvalidOperationException($"User '{userName}' not found.");
                }

                User user = new(session) // Create user object and populate from database
                {
                    _UserName = reader.GetString(0),
                    _New = false
                };

                return user;
            }
            catch (PostgresException ex)
            {
                throw new InvalidOperationException($"Database error loading user: {ex.Message}");
            }
        }

        public string UserName
        {
            get { return _UserName ?? string.Empty; }
            set
            {
                if (!_New) { throw new InvalidOperationException("User name cannot be changed."); }
                if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException("User name must not be empty."); }
                _UserName = value;
            }
        }

        public static int GetId(string username)
        {
            using var connection = Database.Database.GetConnection();
            connection.Open();

            string sql = "SELECT user_id FROM users WHERE username = @username";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@username", username);

            object? result = cmd.ExecuteScalar();

            if (result == null)
            {
                throw new InvalidOperationException($"User '{username}' not found.");
            }

            return Convert.ToInt32(result);
        }

        internal static string _HashPassword(string userName, string password)
        {
            StringBuilder rval = new();
            foreach (byte i in SHA256.HashData(Encoding.UTF8.GetBytes(userName + password)))
            {
                rval.Append(i.ToString("x2"));
            }
            return rval.ToString();
        }

        public void SetPassword(string password)
        {
            _PasswordHash = _HashPassword(UserName, password);
        }

        public override void Save()
        {
            if (!_New) { _EnsureAdminOrOwner(UserName); }

            if (string.IsNullOrWhiteSpace(UserName))
            {
                throw new InvalidOperationException("Username cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(_PasswordHash))
            {
                throw new InvalidOperationException("Password must be set before saving.");
            }

            try
            {
                using var connection = Database.Database.GetConnection(); // Get connection through Database class
                connection.Open();

                if (_New) // Save user as a new user
                {
                    string sql = @"
                        INSERT INTO users (username, password_hash) 
                        VALUES (@username, @password_hash)";

                    using var cmd = new NpgsqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@username", UserName);
                    cmd.Parameters.AddWithValue("@password_hash", _PasswordHash);

                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"User '{UserName}' saved to database.");
                }
                else // Save existing user changes
                {
                    string sql = @"
                        UPDATE users 
                        SET password_hash = @password_hash
                        WHERE username = @username";

                    using var cmd = new NpgsqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@username", UserName);
                    cmd.Parameters.AddWithValue("@password_hash", _PasswordHash);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0) throw new InvalidOperationException("User no longer exists in database.");
                }
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505") // Unique violation
            {
                throw new InvalidOperationException($"Username '{UserName}' already exists.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                throw new InvalidOperationException("Failed to save user to database.", ex);
            }

            _New = false;
            _PasswordHash = null;
            _EndEdit();
        }

        public override void Delete()
        {
            _EnsureAdminOrOwner(UserName);

            try
            {
                using var connection = Database.Database.GetConnection();
                connection.Open();

                string sql = @"
                    DELETE FROM users 
                    WHERE username=@username";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@username", UserName);

                int rows = cmd.ExecuteNonQuery();
                if (rows == 0)
                {
                    Console.WriteLine($"Warning: User '{UserName}' was not found during delete.");
                }
                else
                {
                    Console.WriteLine($"User '{UserName}' deleted from database.");
                }
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState == "23503") // Foreign Key Violation
                {
                    throw new InvalidOperationException($"Cannot delete user '{UserName}' because they have related data (e.g., reviews, lists).", ex);
                }
                throw new InvalidOperationException($"Database error deleting user '{UserName}': {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error deleting user '{UserName}'.", ex);
            }

            _EndEdit();
        }

        public override void Refresh()
        {
            if (_New) { return; }

            try
            {
                using var connection = Database.Database.GetConnection();
                connection.Open();

                string sql = @"
                    SELECT username 
                    FROM users 
                    WHERE username = @username";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@username", UserName);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    throw new InvalidOperationException($"User '{UserName}' no longer exists in the database.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to refresh user '{UserName}'.", ex);
            }
            _EndEdit();
        }

        public JsonObject GetStatistics()
        {
            int totalRatings = 0;
            double averageScore = 0.0;
            int userId = GetId(UserName);

            try
            {
                using var connection = Database.Database.GetConnection();
                connection.Open();

                string statsSql = @"
                    SELECT COUNT(*), COALESCE(AVG(score), 0) 
                    FROM ratings 
                    WHERE user_id = @uid";

                using var cmd = new NpgsqlCommand(statsSql, connection);
                cmd.Parameters.AddWithValue("@uid", userId);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    totalRatings = reader.GetInt32(0);
                    averageScore = reader.GetDouble(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating stats for {UserName}: {ex.Message}");
            }

            return new JsonObject
            {
                ["total_ratings"] = totalRatings,
                ["average_score"] = Math.Round(averageScore, 2)
            };
        }
    }
}
