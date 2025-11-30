using MyMediaList.Handlers;
using MyMediaList.Server;
using System;
using System.Net;
using System.Text.Json.Nodes;

namespace MyMediaList.System
{
    public sealed class RatingHandler : Handler, IHandler
    {
        public override void Handle(HttpRestEventArgs e)
        {
            if (e.Path.StartsWith("/ratings"))
            {
                if (e.Path == "/ratings" && e.Method == HttpMethod.Post)
                {
                    try
                    {
                        Rating rating = new();
                        int uid = e.Content?["user_id"]?.GetValue<int>() ?? 0;
                        int mid = e.Content?["media_id"]?.GetValue<int>() ?? 0;
                        rating.Initialize(uid, mid);
                        rating.Score = e.Content?["score"]?.GetValue<int>() ?? 1;
                        rating.Comment = e.Content?["comment"]?.GetValue<string>() ?? string.Empty;
                        rating.Save();
                        e.Respond(HttpStatusCode.OK, new JsonObject() { ["success"] = true, ["id"] = rating.Id });
                    }
                    catch (Exception ex)
                    {
                        e.Respond(HttpStatusCode.InternalServerError, new JsonObject() { ["success"] = false, ["reason"] = ex.Message });
                    }
                }
                else if (e.Path.StartsWith("/ratings/") && e.Method == HttpMethod.Delete)
                {
                    try
                    {
                        int id = int.Parse(e.Path.Substring("/ratings/".Length));
                        Rating rating = Rating.Get(id, e.Session);
                        rating.Delete();
                        e.Respond(HttpStatusCode.OK, new JsonObject() { ["success"] = true });
                    }
                    catch (Exception ex)
                    {
                        e.Respond(HttpStatusCode.InternalServerError, new JsonObject() { ["success"] = false, ["reason"] = ex.Message });
                    }
                }
                e.Responded = true;
            }
        }
    }
}