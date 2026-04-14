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
using MumbleManager.Hubs;
using MumbleManager.Models;
using MumbleManager.Services;
using Microsoft.AspNetCore.SignalR;

namespace MumbleManager.Endpoints;

public static class TemplateEndpoints
{
    public static void MapTemplateEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/templates").WithTags("Templates").RequireAuthorization();

        g.MapGet("/", async (ClaimsPrincipal principal, AppDbContext db) =>
        {
            var userId    = principal.UserId();
            var templates = await db.ChannelTemplates.Where(t => t.UserId == userId).ToListAsync();
            return templates.Select(t => new
            {
                t.Id, t.Name, t.Description, t.CreatedUtc, t.ModifiedUtc,
                RootChildren = t.RootChildren,
            });
        });

        g.MapPost("/", async (
            [FromBody] TemplateDto dto,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var template = new ChannelTemplate
            {
                Id           = Guid.NewGuid().ToString(),
                UserId       = principal.UserId(),
                Name         = dto.Name,
                Description  = dto.Description ?? string.Empty,
                CreatedUtc   = DateTime.UtcNow,
                ModifiedUtc  = DateTime.UtcNow,
                RootChildren = dto.RootChildren ?? new(),
            };
            db.ChannelTemplates.Add(template);
            await db.SaveChangesAsync();
            return Results.Created($"/api/templates/{template.Id}", new { template.Id });
        });

        g.MapPut("/{id}", async (
            string id,
            [FromBody] TemplateDto dto,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userId   = principal.UserId();
            var existing = await db.ChannelTemplates.FirstOrDefaultAsync(
                t => t.Id == id && t.UserId == userId);
            if (existing is null) return Results.NotFound();
            existing.Name         = dto.Name;
            existing.Description  = dto.Description ?? string.Empty;
            existing.ModifiedUtc  = DateTime.UtcNow;
            existing.RootChildren = dto.RootChildren ?? new();
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        g.MapDelete("/{id}", async (
            string id,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userId = principal.UserId();
            var t = await db.ChannelTemplates.FirstOrDefaultAsync(
                t => t.Id == id && t.UserId == userId);
            if (t is null) return Results.NotFound();
            db.ChannelTemplates.Remove(t);
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        g.MapPost("/apply", async (
            [FromBody] ApplyTemplateRequest req,
            ClaimsPrincipal principal,
            AppDbContext db,
            SessionRegistry sessions,
            IHubContext<StatusHub> hub) =>
        {
            var userId  = principal.UserId();
            var session = sessions.Get(userId, req.HostId);
            if (session is null) return Results.Problem("Not connected.", statusCode: 409);

            var template = await db.ChannelTemplates.FirstOrDefaultAsync(
                t => t.Id == req.TemplateId && t.UserId == userId);
            if (template is null) return Results.NotFound();

            var progress = new Progress<string>(async msg =>
            {
                try { await hub.Clients.Group($"{userId}:{req.HostId}").SendAsync("status", msg); }
                catch { }
            });

            await session.Ice.ApplyTemplateAsync(req.ServerId, template, progress);
            return Results.Ok();
        });
    }
}

public record TemplateDto(string Name, string? Description, List<ChannelNode>? RootChildren);
public record ApplyTemplateRequest(string HostId, int ServerId, string TemplateId);
