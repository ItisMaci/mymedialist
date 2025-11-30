using Npgsql;
using System;

namespace MyMediaList.System
{
    public sealed class Genre : Atom, IAtom
    {
        private int _Id;
        private string? _Name;
        private bool _New;

        public Genre(Session? session = null)
        {
            _EditingSession = session;
            _New = true;
        }

        public int Id => _Id;

        public string Name
        {
            get { return _Name ?? string.Empty; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Genre name cannot be empty.");
                _Name = value;
            }
        }

        public static Genre Get(string name, Session? session = null)
        {
            try
            {
                using var connection = Database.Database.GetConnection();
                connection.Open();

                string sql = "SELECT genre_id, name FROM genres WHERE name = @name";
                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@name", name);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read()) throw new InvalidOperationException($"Genre '{name}' not found.");

                return new Genre(session)
                {
                    _Id = reader.GetInt32(0),
                    _Name = reader.GetString(1),
                    _New = false
                };
            }
            catch (PostgresException ex)
            {
                throw new InvalidOperationException($"Database error loading genre: {ex.Message}");
            }
        }

        public override void Save()
        {
            if (string.IsNullOrWhiteSpace(Name)) throw new InvalidOperationException("Name cannot be empty.");

            try
            {
                using var connection = Database.Database.GetConnection();
                connection.Open();

                if (_New)
                {
                    string sql = "INSERT INTO genres (name) VALUES (@name) RETURNING genre_id";
                    using var cmd = new NpgsqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@name", Name);
                    _Id = (int)cmd.ExecuteScalar()!;
                }
                else
                {
                    string sql = "UPDATE genres SET name = @name WHERE genre_id = @id";
                    using var cmd = new NpgsqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@name", Name);
                    cmd.Parameters.AddWithValue("@id", Id);
                    if (cmd.ExecuteNonQuery() == 0) throw new InvalidOperationException("Genre no longer exists.");
                }
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                throw new InvalidOperationException($"Genre '{Name}' already exists.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save genre.", ex);
            }

            _New = false;
            _EndEdit();
        }

        public override void Delete()
        {
            try
            {
                using var connection = Database.Database.GetConnection();
                connection.Open();

                string sql = "DELETE FROM genres WHERE genre_id = @id";
                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@id", Id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to delete genre.", ex);
            }
            _EndEdit();
        }

        public override void Refresh()
        {
            if (_New) return;
            _EndEdit();
        }
    }
}