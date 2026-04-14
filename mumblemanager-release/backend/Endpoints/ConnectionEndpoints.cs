// =============================================================================
// MumbleManager
// Author:  Gerald Hull, W1VE
// Date:    April 14, 2026
// License: MIT License
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// =============================================================================

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MumbleManager.Data;
using MumbleManager.Hubs;
using MumbleManager.Models;
using MumbleManager.Services;

namespace MumbleManager.Endpoints;

public static class ConnectionEndpoints
{
    public static void MapConnectionEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/hosts/{hostId}/connection")
                   .WithTags("Connection")
                   .RequireAuthorization();

        g.MapPost("/", async (
            string hostId,
            ClaimsPrincipal principal,
            AppDbContext db,
            SessionRegistry sessions,
            IHubContext<StatusHub> hub,
            HttpContext http) =>
        {
            var userId    = principal.UserId();
            var clientKey = http.Request.ClientKey();

            if (sessions.IsConnected(userId, hostId, clientKey))
                return Results.Conflict(new { message = "Already connected." });

            var host = await db.Hosts.FirstOrDefaultAsync(
                h => h.Id == hostId && h.UserId == userId);
            if (host is null) return Results.NotFound();

            var session  = new HostSession(host);
            var group    = $"{userId}:{hostId}:{clientKey}";
            var progress = new Progress<string>(async msg =>
            {
                try { await hub.Clients.Group(group).SendAsync("status", msg); }
                catch { }
            });

            try
            {
                await session.ConnectAsync(progress);
                sessions.Add(userId, hostId, clientKey, session);

                var servers = await session.Ice.GetAllServersAsync();
                var cached  = servers.Select(s => VirtualServerCache.FromConfig(hostId, s)).ToList();

                var old = db.VirtualServers.Where(v => v.HostId == hostId);
                db.VirtualServers.RemoveRange(old);
                db.VirtualServers.AddRange(cached);
                await db.SaveChangesAsync();

                await hub.Clients.Group(group)
                    .SendAsync("connected", new { hostId, serverCount = servers.Count });

                return Results.Ok(new
                {
                    version     = session.Version.ToString(),
                    serverCount = servers.Count,
                    servers,
                });
            }
            catch (Exception ex)
            {
                try { session.Dispose(); } catch { }
                var msg = ex.InnerException?.Message ?? ex.Message;
                await hub.Clients.Group(group).SendAsync("error", msg);
                return Results.Problem(msg, statusCode: 502);
            }
        });

        g.MapDelete("/", async (
            string hostId,
            ClaimsPrincipal principal,
            SessionRegistry sessions,
            IHubContext<StatusHub> hub,
            HttpContext http) =>
        {
            var userId    = principal.UserId();
            var clientKey = http.Request.ClientKey();
            var group     = $"{userId}:{hostId}:{clientKey}";
            bool removed  = sessions.Remove(userId, hostId, clientKey);
            // Only notify this specific browser session — not other devices
            await hub.Clients.Group(group).SendAsync("disconnected", hostId);
            return removed ? Results.Ok() : Results.NotFound();
        });

        g.MapGet("/", (string hostId, ClaimsPrincipal principal, SessionRegistry sessions, HttpContext http) =>
        {
            var userId    = principal.UserId();
            var clientKey = http.Request.ClientKey();
            var session   = sessions.Get(userId, hostId, clientKey);
            if (session is null)
                return Results.Ok(new { connected = false, version = (string?)null });
            return Results.Ok(new { connected = true, version = session.Version.ToString() });
        });
    }
}
