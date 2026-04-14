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
using MumbleManager.Services;

namespace MumbleManager.Endpoints;

public static class ChannelEndpoints
{
    public static void MapChannelEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/hosts/{hostId}/servers/{serverId:int}/channels")
                   .WithTags("Channels")
                   .RequireAuthorization();

        g.MapGet("/", async (
            string hostId, int serverId,
            ClaimsPrincipal principal,
            SessionRegistry sessions) =>
        {
            var session = sessions.Get(principal.UserId(), hostId);
            if (session is null) return Results.Problem("Not connected.", statusCode: 409);
            var channels = await session.Ice.GetChannelsAsync(serverId);
            return Results.Ok(channels.Values.OrderBy(c => c.ParentId).ThenBy(c => c.Position));
        });

        g.MapPost("/", async (
            string hostId, int serverId,
            [FromBody] AddChannelRequest req,
            ClaimsPrincipal principal,
            SessionRegistry sessions) =>
        {
            var session = sessions.Get(principal.UserId(), hostId);
            if (session is null) return Results.Problem("Not connected.", statusCode: 409);
            int newId = await session.Ice.AddChannelAsync(serverId, req.Name, req.ParentId);
            return Results.Ok(new { channelId = newId });
        });

        g.MapDelete("/{channelId:int}", async (
            string hostId, int serverId, int channelId,
            ClaimsPrincipal principal,
            SessionRegistry sessions) =>
        {
            if (channelId == 0) return Results.BadRequest("Cannot remove Root channel.");
            var session = sessions.Get(principal.UserId(), hostId);
            if (session is null) return Results.Problem("Not connected.", statusCode: 409);
            await session.Ice.RemoveChannelAsync(serverId, channelId);
            return Results.Ok();
        });

        g.MapPatch("/{channelId:int}", async (
            string hostId, int serverId, int channelId,
            [FromBody] RenameChannelRequest req,
            ClaimsPrincipal principal,
            SessionRegistry sessions) =>
        {
            var session = sessions.Get(principal.UserId(), hostId);
            if (session is null) return Results.Problem("Not connected.", statusCode: 409);
            await session.Ice.SetChannelNameAsync(serverId, channelId, req.Name);
            return Results.Ok();
        });

        g.MapDelete("/all", async (
            string hostId, int serverId,
            ClaimsPrincipal principal,
            SessionRegistry sessions) =>
        {
            var session = sessions.Get(principal.UserId(), hostId);
            if (session is null) return Results.Problem("Not connected.", statusCode: 409);
            await session.Ice.DeleteAllChannelsAsync(serverId);
            return Results.Ok();
        });
    }
}

public record AddChannelRequest(string Name, int ParentId);
public record RenameChannelRequest(string Name);
