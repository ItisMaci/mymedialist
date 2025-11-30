using MyMediaList.Handlers;
using MyMediaList.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MyMediaList.System
{
    public sealed class SessionHandler : Handler, IHandler
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // [override] Handler                                                                                               //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Handles a request if possible.</summary>
        /// <param name="e">Event arguments.</param>
        public override void Handle(HttpRestEventArgs e)
        {
            if (e.Path.StartsWith("/login"))
            {
                if ((e.Path == "/login") && (e.Method == HttpMethod.Post))
                {
                    try
                    {
                        Session? session = Session.Create(e.Content["username"]?.GetValue<string>() ?? string.Empty, e.Content["password"]?.GetValue<string>() ?? string.Empty);

                        if (session is null)
                        {
                            e.Respond(HttpStatusCode.Unauthorized, new JsonObject() { ["success"] = false, ["reason"] = "Invalid username or password." });
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[{nameof(VersionHandler)} Invalid login attempt. {e.Method.ToString()} {e.Path}.");
                        }
                        else
                        {
                            e.Respond(HttpStatusCode.OK, new JsonObject() { ["success"] = true, ["token"] = session.Token });
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine($"[{nameof(VersionHandler)} Handled {e.Method.ToString()} {e.Path}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        e.Respond(HttpStatusCode.InternalServerError, new JsonObject() { ["success"] = false, ["reason"] = ex.Message });
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[{nameof(VersionHandler)} Exception creating session. {e.Method.ToString()} {e.Path}: {ex.Message}");
                    }
                }
                else
                {
                    e.Respond(HttpStatusCode.BadRequest, new JsonObject() { ["success"] = false, ["reason"] = "Invalid session endpoint." });

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{nameof(VersionHandler)} Invalid session endpoint.");
                }

                e.Responded = true;
            }
        }
    }

}
