using Npgsql;
using System;
using System.Text.Json.Nodes;

namespace MyMediaList.System
{
    public sealed class Media : Atom, IAtom
    {
        private int _Id;
        private string? _Title;
        private string? _Description;
        private string? _Type;
        private int _ReleaseYear;
        private int _AgeRestriction;
        private int _CreatorId;
        private bool _New;

        public Media(Session? session = null)
        {
            _EditingSession = session;
            _New = true;
        }

        public int Id => _Id;
        public int CreatorId => _CreatorId;

        public string Title
        {
            get => _Title ?? string.Empty;
            set => _Title = value;
        }

        public string Description
        {
            get => _Description ?? string.Empty;
            set => _Description = value;
        }

        public string Type
        {
            get => _Type ?? "Movie";
            set => _Type = value;
        }

        public int ReleaseYear
        {
            get => _ReleaseYear;
            set => _ReleaseYear = value;
        }

        public int AgeRestriction
        {
            get => _AgeRestriction;
            set => _AgeRestriction = value;
        }

        public void SetCreator(int creatorId)
        {
            if (!_New) throw new InvalidOperationException("Cannot change creator.");
            _CreatorId = creatorId;
        }

        public static Media Get(int id, Session? session = null)
        {
            try
            {
                using var connection = Database.Database.GetConnection();
                connection.Open();

                string sql = "SELECT media_id, title, description, type, release_year, age_restriction, creator_id FROM media_entries WHERE media_id = @id";
                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read()) throw new InvalidOperationException($"Media '{id}' not found.");

                return new Media(session)
                {
                    _Id = reader.GetInt32(0),
                    _Title = reader.GetString(1),
                    _Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    _Type = reader.GetString(3),
                    _ReleaseYear = reader.GetInt32(4),
                    _AgeRestriction = reader.GetInt32(5),
                    _CreatorId = reader.GetInt32(6),
                    _New = false
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading media: {ex.Message}");
            }
        }

        public override void Save()
        {
            if (string.IsNullOrWhiteSpace(Title)) throw new InvalidOperationException("Title cannot be empty.");

            try
            {
                using var connection = Database.Database.GetConnection();
                connection.Open();

                if (_New)
                {
                    using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM media_entries WHERE LOWER(title) = LOWER(@title)", connection))
                    {
                        checkCmd.Parameters.AddWithValue("@title", Title);
                        long count = (long)checkCmd.ExecuteScalar()!;
                        if (count > 0)
                        {
                            throw new InvalidOperationException($"A media entry with the title '{Title}' already exists.");
                        }
                    }

                    string sql = @"INSERT INTO media_entries (title, description, type, release_year, age_restriction, creator_id) 
                                   VALUES (@title, @desc, @type, @year, @age, @creator) RETURNING media_id";
                    using var cmd = new NpgsqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@title", Title);
                    cmd.Parameters.AddWithValue("@desc", (object?)Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@type", Type);
                    cmd.Parameters.AddWithValue("@year", ReleaseYear);
                    cmd.Parameters.AddWithValue("@age", AgeRestriction);
                    cmd.Parameters.AddWithValue("@creator", CreatorId);
                    _Id = (int)cmd.ExecuteScalar()!;
                }
                else
                {
                    using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM media_entries WHERE LOWER(title) = LOWER(@title) AND media_id != @id", connection))
                    {
                        checkCmd.Parameters.AddWithValue("@title", Title);
                        checkCmd.Parameters.AddWithValue("@id", Id);
                        long count = (long)checkCmd.ExecuteScalar()!;
                        if (count > 0)
                        {
                            throw new InvalidOperationException($"A media entry with the title '{Title}' already exists.");
                        }
                    }

                    string sql = @"UPDATE media_entries SET title=@title, description=@desc, type=@type, release_year=@year, age_restriction=@age 
                                   WHERE media_id=@id";
                    using var cmd = new NpgsqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@title", Title);
                    cmd.Parameters.AddWithValue("@desc", (object?)Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@type", Type);
                    cmd.Parameters.AddWithValue("@year", ReleaseYear);
                    cmd.Parameters.AddWithValue("@age", AgeRestriction);
                    cmd.Parameters.AddWithValue("@id", Id);
                    if (cmd.ExecuteNonQuery() == 0) throw new InvalidOperationException("Media no longer exists.");
                }
            }
            catch (PostgresException ex) when (ex.SqlState == "23505") // Unique constraint violation (if you add a DB constraint later)
            {
                throw new InvalidOperationException($"A media entry with the title '{Title}' already exists.");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("already exists")) throw;

                throw new InvalidOperationException("Failed to save media.", ex);
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
                using var cmd = new NpgsqlCommand("DELETE FROM media_entries WHERE media_id=@id", connection);
                cmd.Parameters.AddWithValue("@id", Id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to delete media.", ex);
            }
            _EndEdit();
        }

        public override void Refresh()
        {
            if (_New) return;
            _EndEdit();
        }

        public static JsonArray GetList(Session? session = null)
        {
            JsonArray list = new();
            try
            {
                using var connection = Database.Database.GetConnection();
                connection.Open();

                string sql = "SELECT media_id, title, type, release_year, creator_id FROM media_entries ORDER BY title ASC";

                using var cmd = new NpgsqlCommand(sql, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new JsonObject
                    {
                        ["id"] = reader.GetInt32(0),
                        ["title"] = reader.GetString(1),
                        ["type"] = reader.GetString(2),
                        ["release_year"] = reader.GetInt32(3),
                        ["creator_id"] = reader.GetInt32(4)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading media list: {ex.Message}");
            }
            return list;
        }

    }
}