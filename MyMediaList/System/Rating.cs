using Npgsql;
using System;

namespace MyMediaList.System
{
    public sealed class Rating : Atom, IAtom
    {
        private int _Id;
        private int _UserId;
        private int _MediaId;
        private int _Score;
        private string? _Comment;
        private bool _IsConfirmed;
        private bool _New;

        public Rating(Session? session = null)
        {
            _EditingSession = session;
            _New = true;
        }

        public int Id => _Id;
        public int UserId => _UserId;
        public int MediaId => _MediaId;

        public int Score
        {
            get => _Score;
            set { if (value < 1 || value > 5) throw new ArgumentException("Score must be 1-5."); _Score = value; }
        }

        public string Comment
        {
            get => _Comment ?? string.Empty;
            set => _Comment = value;
        }

        public void Initialize(int userId, int mediaId)
        {
            if (!_New) throw new InvalidOperationException("Cannot change linking.");
            _UserId = userId;
            _MediaId = mediaId;
        }

        public static Rating Get(int id, Session? session = null)
        {
            try
            {
                using var connection = Database.Database.GetConnection();
                connection.Open();
                string sql = "SELECT rating_id, user_id, media_id, score, comment, is_confirmed FROM ratings WHERE rating_id = @id";
                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) throw new InvalidOperationException("Rating not found.");

                return new Rating(session)
                {
                    _Id = reader.GetInt32(0),
                    _UserId = reader.GetInt32(1),
                    _MediaId = reader.GetInt32(2),
                    _Score = reader.GetInt32(3),
                    _Comment = reader.IsDBNull(4) ? null : reader.GetString(4),
                    _IsConfirmed = reader.GetBoolean(5),
                    _New = false
                };
            }
            catch (Exception ex) { throw new InvalidOperationException("Error loading rating", ex); }
        }

        public override void Save()
        {
            try
            {
                using var connection = Database.Database.GetConnection();
                connection.Open();
                if (_New)
                {
                    string sql = "INSERT INTO ratings (user_id, media_id, score, comment) VALUES (@uid, @mid, @score, @comment) RETURNING rating_id";
                    using var cmd = new NpgsqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@uid", UserId);
                    cmd.Parameters.AddWithValue("@mid", MediaId);
                    cmd.Parameters.AddWithValue("@score", Score);
                    cmd.Parameters.AddWithValue("@comment", (object?)Comment ?? DBNull.Value);
                    _Id = (int)cmd.ExecuteScalar()!;
                }
                else
                {
                    string sql = "UPDATE ratings SET score=@score, comment=@comment WHERE rating_id=@id";
                    using var cmd = new NpgsqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@score", Score);
                    cmd.Parameters.AddWithValue("@comment", (object?)Comment ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", Id);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { throw new InvalidOperationException("Failed to save rating.", ex); }
            _New = false;
            _EndEdit();
        }

        public override void Delete()
        {
            try
            {
                using var connection = Database.Database.GetConnection();
                connection.Open();
                using var cmd = new NpgsqlCommand("DELETE FROM ratings WHERE rating_id=@id", connection);
                cmd.Parameters.AddWithValue("@id", Id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { throw new InvalidOperationException("Failed to delete rating.", ex); }
            _EndEdit();
        }

        public override void Refresh() { _EndEdit(); }
    }
}