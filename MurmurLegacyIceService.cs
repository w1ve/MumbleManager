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
using Microsoft.EntityFrameworkCore;
using MumbleManager.Data;
using MumbleManager.Models;
using MumbleManager.Services;

namespace MumbleManager.Endpoints;

public static class ServerEndpoints
{
    public static void MapServerEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/hosts/{hostId}/servers")
                   .WithTags("Servers")
                   .RequireAuthorization();

        g.MapGet("/", async (
            string hostId,
            ClaimsPrincipal principal,
            SessionRegistry sessions,
            AppDbContext db) =>
        {
            var userId  = principal.UserId();
            var session = sessions.Get(userId, hostId);
            if (session is not null)
                return Results.Ok(await session.Ice.GetAllServersAsync());

            var cached = await db.VirtualServers
                .Where(v => v.HostId == hostId)
                .Select(v => v.ToConfig())
                .ToListAsync();
            return Results.Ok(cached);
        });

        g.MapGet("/{serverId:int}/config", async (
            string hostId, int serverId,
            ClaimsPrincipal principal,
            SessionRegistry sessions) =>
        {
            var session = sessions.Get(principal.UserId(), hostId);
            if (session is null) return Results.Problem("Not connected.", statusCode: 409);
            var cfg = await session.Ice.GetServerByIdAsync(serverId);
            return cfg is null ? Results.NotFound() : Results.Ok(cfg);
        });

        g.MapPut("/{serverId:int}/config", async (
            string hostId, int serverId,
            [FromBody] VirtualServerConfig cfg,
            ClaimsPrincipal principal,
            SessionRegistry sessions,
            AppDbContext db) =>
        {
            var userId  = principal.UserId();
            var session = sessions.Get(userId, hostId);
            if (session is null) return Results.Problem("Not connected.", statusCode: 409);

            cfg.ServerId = serverId;
            await session.Ice.WriteConfigAsync(serverId, cfg);

            var cached = await db.VirtualServers
                .FirstOrDefaultAsync(v => v.HostId == hostId && v.ServerId == serverId);
            if (cached is not null)
            {
                var updated = VirtualServerCache.FromConfig(hostId, cfg);
                updated.RowId = cached.RowId;
                db.Entry(cached).CurrentValues.SetValues(updated);
            }
            else
            {
                db.VirtualServers.Add(VirtualServerCache.FromConfig(hostId, cfg));
            }
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        g.MapPost("/", async (
            string hostId,
            [FromBody] CreateServerRequest req,
            ClaimsPrincipal principal,
            SessionRegistry sessions) =>
        {
            var session = sessions.Get(principal.UserId(), hostId);
            if (session is null) return Results.Problem("Not connected.", statusCode: 409);
            int newId = await session.Ice.CreateNewServerAsync(req.Port);
            return Results.Ok(new { serverId = newId });
        });

        g.MapGet("/used-ports", async (
            string hostId,
            ClaimsPrincipal principal,
            SessionRegistry sessions) =>
        {
            var session = sessions.Get(principal.UserId(), hostId);
            if (session is null) return Results.Problem("Not connected.", statusCode: 409);
            return Results.Ok(await session.Ice.GetUsedPortsAsync());
        });

        g.MapPost("/{serverId:int}/start", async (
            string hostId, int serverId,
            ClaimsPrincipal principal,
            SessionRegistry sessions) =>
        {
            var session = sessions.Get(principal.UserId(), hostId);
            if (session is null) return Results.Problem("Not connected.", statusCode: 409);
            await session.Ice.StartServerAsync(serverId);
            return Results.Ok();
        });

        g.MapPost("/{serverId:int}/stop", async (
            string hostId, int serverId,
            ClaimsPrincipal principal,
            SessionRegistry sessions) =>
        {
            var session = sessions.Get(principal.UserId(), hostId);
            if (session is null) return Results.Problem("Not connected.", statusCode: 409);
            await session.Ice.StopServerAsync(serverId);
            return Results.Ok();
        });

        g.MapDelete("/{serverId:int}", async (
            string hostId, int serverId,
            ClaimsPrincipal principal,
            SessionRegistry sessions,
            AppDbContext db) =>
        {
            var userId  = principal.UserId();
            var session = sessions.Get(userId, hostId);
            if (session is null) return Results.Problem("Not connected.", statusCode: 409);
            await session.Ice.DeleteServerAsync(serverId);
            var cached = await db.VirtualServers
                .FirstOrDefaultAsync(v => v.HostId == hostId && v.ServerId == serverId);
            if (cached is not null)
            {
                db.VirtualServers.Remove(cached);
                await db.SaveChangesAsync();
            }
            return Results.Ok();
        });

        g.MapPost("/{serverId:int}/ufw-open", async (
            string hostId, int serverId,
            [FromBody] UfwOpenRequest req,
            ClaimsPrincipal principal,
            SessionRegistry sessions) =>
        {
            var session = sessions.Get(principal.UserId(), hostId);
            if (session is null) return Results.Problem("Not connected.", statusCode: 409);
            bool ufwActive = await session.Tunnel.IsUfwActiveAsync();
            if (!ufwActive) return Results.Ok(new { ufwActive = false, output = (string?)null });
            var output = await session.Tunnel.OpenPortInUfwAsync(req.Port);
            return Results.Ok(new { ufwActive = true, output });
        });
    }
}

public record CreateServerRequest(int Port);
public record UfwOpenRequest(int Port);
