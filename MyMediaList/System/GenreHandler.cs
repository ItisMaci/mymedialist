using MyMediaList.Handlers;
using MyMediaList.Server;
using System;
using System.Net;
using System.Text.Json.Nodes;

namespace MyMediaList.System
{
    public sealed class GenreHandler : Handler, IHandler
    {
        public override void Handle(HttpRestEventArgs e)
        {
            if (e.Path.StartsWith("/genres"))
            {
                if (e.Path == "/genres" && e.Method == HttpMethod.Post)
                {
                    try
                    {
                        Genre genre = new()
                        {
                            Name = e.Content?["name"]?.GetValue<string>() ?? string.Empty
                        };
                        genre.Save();

                        e.Respond(HttpStatusCode.OK, new JsonObject() { ["success"] = true, ["message"] = "Genre created." });
                    }
                    catch (Exception ex)
                    {
                        e.Respond(HttpStatusCode.InternalServerError, new JsonObject() { ["success"] = false, ["reason"] = ex.Message });
                    }
                }
                else if (e.Path.StartsWith("/genres/") && e.Method == HttpMethod.Get)
                {
                    try
                    {
                        string name = e.Path.Substring("/genres/".Length);
                        Genre genre = Genre.Get(name, e.Session);
                        e.Respond(HttpStatusCode.OK, new JsonObject() { ["success"] = true, ["name"] = genre.Name, ["id"] = genre.Id });
                    }
                    catch (Exception ex)
                    {
                        e.Respond(HttpStatusCode.InternalServerError, new JsonObject() { ["success"] = false, ["reason"] = ex.Message });
                    }
                }
                else if (e.Path.StartsWith("/genres/") && e.Method == HttpMethod.Delete)
                {
                    try
                    {
                        string name = e.Path.Substring("/genres/".Length);
                        Genre genre = Genre.Get(name, e.Session);
                        genre.Delete();
                        e.Respond(HttpStatusCode.OK, new JsonObject() { ["success"] = true, ["message"] = "Genre deleted." });
                    }
                    catch (Exception ex)
                    {
                        e.Respond(HttpStatusCode.InternalServerError, new JsonObject() { ["success"] = false, ["reason"] = ex.Message });
                    }
                }
                else
                {
                    e.Respond(HttpStatusCode.BadRequest, new JsonObject() { ["success"] = false, ["reason"] = "Invalid endpoint." });
                }
                e.Responded = true;
            }
        }
    }
}