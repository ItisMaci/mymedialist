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
    public sealed class UserHandler : Handler, IHandler
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
            if (e.Path.StartsWith("/users"))
            {
                if ((e.Path == "/users") && (e.Method == HttpMethod.Post)) // Register a user
                {
                    try
                    {
                        User user = new()
                        {
                            UserName = e.Content?["username"]?.GetValue<string>() ?? string.Empty
                        };
                        user.SetPassword(e.Content?["password"]?.GetValue<string>() ?? string.Empty);
                        user.Save();

                        e.Respond(HttpStatusCode.OK, new JsonObject() { ["success"] = true, ["message"] = "User created." });
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(UserHandler)}] Handled {e.Method} {e.Path}.");
                    }
                    catch (Exception ex)
                    {
                        e.Respond(HttpStatusCode.InternalServerError, new JsonObject() { ["success"] = false, ["reason"] = ex.Message });
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[{nameof(UserHandler)}] Exception creating user. {e.Method} {e.Path}: {ex.Message}");
                    }
                }
                else if (e.Path.StartsWith("/users/") && e.Path.EndsWith("/profile") && (e.Method == HttpMethod.Get))
                {
                    try
                    {
                        string sub = e.Path.Substring("/users/".Length);
                        string username = sub.Substring(0, sub.Length - "/profile".Length);

                        User user = User.Get(username, e.Session);
                        JsonObject stats = user.GetStatistics();

                        e.Respond(HttpStatusCode.OK, new JsonObject()
                        {
                            ["success"] = true,
                            ["username"] = user.UserName,
                            ["stats"] = stats
                        });
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(UserHandler)}] Handled {e.Method} {e.Path}.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        e.Respond(HttpStatusCode.NotFound, new JsonObject() { ["success"] = false, ["reason"] = ex.Message });
                    }
                    catch (Exception ex)
                    {
                        e.Respond(HttpStatusCode.InternalServerError, new JsonObject() { ["success"] = false, ["reason"] = ex.Message });
                    }
                }
                else if (e.Path.StartsWith("/users/") && (e.Method == HttpMethod.Get)) // Get user info
                {
                    try
                    {
                        string username = e.Path.Substring("/users/".Length);
                        User user = User.Get(username, e.Session);

                        e.Respond(HttpStatusCode.OK, new JsonObject()
                        {
                            ["success"] = true,
                            ["username"] = user.UserName
                        });
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(UserHandler)}] Handled {e.Method} {e.Path}.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        e.Respond(HttpStatusCode.NotFound, new JsonObject() { ["success"] = false, ["reason"] = ex.Message });
                    }
                    catch (Exception ex)
                    {
                        e.Respond(HttpStatusCode.InternalServerError, new JsonObject() { ["success"] = false, ["reason"] = ex.Message });
                    }
                }
                else if (e.Path.StartsWith("/users/") && (e.Method == HttpMethod.Put)) // Update a users password
                {
                    try
                    {
                        string username = e.Path.Substring("/users/".Length);
                        User user = User.Get(username, e.Session);

                        user.BeginEdit(e.Session);
                        user.SetPassword(e.Content?["password"]?.GetValue<string>() ?? string.Empty);
                        user.Save();

                        e.Respond(HttpStatusCode.OK, new JsonObject()
                        {
                            ["success"] = true,
                            ["message"] = $"User '{username}' updated."
                        });
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(UserHandler)}] Handled {e.Method} {e.Path}.");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        e.Respond(HttpStatusCode.Forbidden, new JsonObject()
                        {
                            ["success"] = false,
                            ["reason"] = ex.Message
                        });
                    }
                    catch (Exception ex)
                    {
                        e.Respond(HttpStatusCode.InternalServerError, new JsonObject()
                        {
                            ["success"] = false,
                            ["reason"] = ex.Message
                        });
                    }
                }
                else if (e.Path.StartsWith("/users/") && (e.Method == HttpMethod.Delete)) // Delete a user
                {
                    try
                    {
                        string username = e.Path.Substring("/users/".Length);
                        User user = User.Get(username, e.Session);

                        user.BeginEdit(e.Session);
                        user.Delete();

                        e.Respond(HttpStatusCode.OK, new JsonObject()
                        {
                            ["success"] = true,
                            ["message"] = $"User '{username}' deleted."
                        });
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(UserHandler)}] Handled {e.Method} {e.Path}.");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        e.Respond(HttpStatusCode.Forbidden, new JsonObject()
                        {
                            ["success"] = false,
                            ["reason"] = ex.Message
                        });
                    }
                    catch (Exception ex)
                    {
                        e.Respond(HttpStatusCode.InternalServerError, new JsonObject()
                        {
                            ["success"] = false,
                            ["reason"] = ex.Message
                        });
                    }
                }
                else
                {
                    e.Respond(HttpStatusCode.BadRequest, new JsonObject() { ["success"] = false, ["reason"] = "Invalid user endpoint." });
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{nameof(UserHandler)}] Invalid user endpoint.");
                }

                e.Responded = true;
            }
        }
    }
}
