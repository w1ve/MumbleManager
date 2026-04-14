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

public static class HostEndpoints
{
    public static void MapHostEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/hosts").WithTags("Hosts").RequireAuthorization();

        g.MapGet("/", async (ClaimsPrincipal principal, AppDbContext db, SessionRegistry sessions, HttpContext http) =>
        {
            var userId    = principal.UserId();
            var clientKey = http.Request.ClientKey();
            var hosts     = await db.Hosts
                .Where(h => h.UserId == userId)
                .Include(h => h.CachedServers)
                .ToListAsync();
            return hosts.Select(h => new
            {
                h.Id, h.DisplayName, h.Host, h.SshPort, h.Username, h.IceSecret,
                HasPassword = !string.IsNullOrEmpty(h.Password),
                IsConnected = sessions.IsConnected(userId, h.Id, clientKey),
                CachedServers = h.CachedServers.Select(s => s.ToConfig()).ToList(),
            });
        });

        g.MapPost("/", async (
            [FromBody] SshHostEntry host,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            host.Id           = Guid.NewGuid().ToString();
            host.UserId       = principal.UserId();
            host.CachedServers = new();
            db.Hosts.Add(host);
            await db.SaveChangesAsync();
            return Results.Created($"/api/hosts/{host.Id}", new { host.Id });
        });

        g.MapPut("/{id}", async (
            string id,
            [FromBody] SshHostEntry updated,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var existing = await db.Hosts.FirstOrDefaultAsync(
                h => h.Id == id && h.UserId == principal.UserId());
            if (existing is null) return Results.NotFound();

            existing.DisplayName = updated.DisplayName;
            existing.Host        = updated.Host;
            existing.SshPort     = updated.SshPort;
            existing.Username    = updated.Username;
            existing.IceSecret   = updated.IceSecret;
            if (!string.IsNullOrWhiteSpace(updated.Password))
                existing.Password = updated.Password;

            await db.SaveChangesAsync();
            return Results.Ok();
        });

        g.MapPost("/{id}/restart-mumble", async (
            string id,
            ClaimsPrincipal principal,
            SessionRegistry sessions,
            HttpContext http) =>
        {
            var userId    = principal.UserId();
            var clientKey = http.Request.ClientKey();
            var session   = sessions.Get(userId, id, clientKey) ?? sessions.Get(userId, id);
            if (session is null) return Results.Problem("Not connected.", statusCode: 409);
            var output = await session.Tunnel.RunCommandAsync("sudo service mumble-server restart");
            return Results.Ok(new { output });
        });

        g.MapDelete("/{id}", async (
            string id,
            ClaimsPrincipal principal,
            AppDbContext db,
            SessionRegistry sessions,
            HttpContext http) =>
        {
            var userId    = principal.UserId();
            var clientKey = http.Request.ClientKey();
            sessions.Remove(userId, id, clientKey);
            var host = await db.Hosts.FirstOrDefaultAsync(
                h => h.Id == id && h.UserId == userId);
            if (host is null) return Results.NotFound();
            db.Hosts.Remove(host);
            await db.SaveChangesAsync();
            return Results.Ok();
        });
    }
}
