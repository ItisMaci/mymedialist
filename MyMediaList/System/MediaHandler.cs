using MyMediaList.Handlers;
using MyMediaList.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json.Nodes;

namespace MyMediaList.System
{
    public sealed class MediaHandler : Handler, IHandler
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // [override] Handler                                                                                               //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles a request if possible.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        public override void Handle(HttpRestEventArgs e)
        {
            if (!e.Path.StartsWith("/media", StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                string[] segments = e.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length == 1)
                {
                    if (e.Method == HttpMethod.Get)
                    {
                        JsonArray list = Media.GetList(e.Session);
                        e.Respond(HttpStatusCode.OK, new JsonObject { ["success"] = true, ["data"] = list });
                    }
                    else if (e.Method == HttpMethod.Post)
                    {
                        if (e.Session == null)
                        {
                            e.Respond(HttpStatusCode.Unauthorized, new JsonObject { ["success"] = false, ["reason"] = "Authentication required." });
                            return;
                        }

                        Media media = new()
                        {
                            Title = e.Content?["title"]?.GetValue<string>() ?? string.Empty,
                            Description = e.Content?["description"]?.GetValue<string>() ?? string.Empty,
                            Type = e.Content?["type"]?.GetValue<string>() ?? "Movie",
                            ReleaseYear = e.Content?["release_year"]?.GetValue<int>() ?? 0,
                            AgeRestriction = e.Content?["age_restriction"]?.GetValue<int>() ?? 0
                        };

                        int userId = User.GetId(e.Session.UserName);
                        media.SetCreator(userId);

                        media.Save();

                        e.Respond(HttpStatusCode.Created, new JsonObject { ["success"] = true, ["id"] = media.Id, ["message"] = "Media entry created." });
                    }
                    else
                    {
                        e.Respond(HttpStatusCode.MethodNotAllowed, new JsonObject { ["success"] = false, ["reason"] = "Method not allowed." });
                    }
                }

                else if (segments.Length == 2 && int.TryParse(segments[1], out int id))
                {
                    if (e.Method == HttpMethod.Get)
                    {
                        Media media = Media.Get(id, e.Session);
                        e.Respond(HttpStatusCode.OK, new JsonObject
                        {
                            ["success"] = true,
                            ["id"] = media.Id,
                            ["title"] = media.Title,
                            ["description"] = media.Description,
                            ["type"] = media.Type,
                            ["release_year"] = media.ReleaseYear,
                            ["age_restriction"] = media.AgeRestriction,
                            ["creator_id"] = media.CreatorId
                        });
                    }
                    else if (e.Method == HttpMethod.Put)
                    {
                        if (e.Session == null)
                        {
                            e.Respond(HttpStatusCode.Unauthorized, new JsonObject { ["success"] = false, ["reason"] = "Authentication required." });
                            return;
                        }

                        Media media = Media.Get(id, e.Session);

                        int currentUserId = User.GetId(e.Session.UserName);
                        if (media.CreatorId != currentUserId)
                        {
                            e.Respond(HttpStatusCode.Forbidden, new JsonObject { ["success"] = false, ["reason"] = "You can only edit your own entries." });
                            return;
                        }

                        media.BeginEdit(e.Session);
                        if (e.Content?.ContainsKey("title") == true) media.Title = e.Content["title"]!.GetValue<string>();
                        if (e.Content?.ContainsKey("description") == true) media.Description = e.Content["description"]!.GetValue<string>();
                        if (e.Content?.ContainsKey("type") == true) media.Type = e.Content["type"]!.GetValue<string>();
                        if (e.Content?.ContainsKey("release_year") == true) media.ReleaseYear = e.Content["release_year"]!.GetValue<int>();
                        if (e.Content?.ContainsKey("age_restriction") == true) media.AgeRestriction = e.Content["age_restriction"]!.GetValue<int>();

                        media.Save();

                        e.Respond(HttpStatusCode.OK, new JsonObject { ["success"] = true, ["message"] = "Media updated." });
                    }
                    else if (e.Method == HttpMethod.Delete)
                    {
                        if (e.Session == null)
                        {
                            e.Respond(HttpStatusCode.Unauthorized, new JsonObject { ["success"] = false, ["reason"] = "Authentication required." });
                            return;
                        }

                        Media media = Media.Get(id, e.Session);

                        int currentUserId = User.GetId(e.Session.UserName);
                        if (media.CreatorId != currentUserId)
                        {
                            e.Respond(HttpStatusCode.Forbidden, new JsonObject { ["success"] = false, ["reason"] = "You can only delete your own entries." });
                            return;
                        }

                        media.Delete();
                        e.Respond(HttpStatusCode.OK, new JsonObject { ["success"] = true, ["message"] = "Media deleted." });
                    }
                    else
                    {
                        e.Respond(HttpStatusCode.MethodNotAllowed, new JsonObject { ["success"] = false, ["reason"] = "Method not allowed." });
                    }
                }
                else
                {
                    e.Respond(HttpStatusCode.NotFound, new JsonObject { ["success"] = false, ["reason"] = "Endpoint not found." });
                }

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"[{nameof(MediaHandler)}] Handled {e.Method} {e.Path}.");
            }
            catch (InvalidOperationException ex)
            {
                e.Respond(HttpStatusCode.NotFound, new JsonObject { ["success"] = false, ["reason"] = ex.Message });
            }
            catch (Exception ex)
            {
                e.Respond(HttpStatusCode.InternalServerError, new JsonObject { ["success"] = false, ["reason"] = ex.Message });
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{nameof(MediaHandler)}] Error: {ex.Message}");
            }

            e.Responded = true;
        }
    }
}
