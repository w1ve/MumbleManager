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

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/users")
                   .WithTags("Users")
                   .RequireAuthorization("AdminOnly");

        // GET /api/users
        g.MapGet("/", async (AppDbContext db) =>
        {
            var users = await db.Users.ToListAsync();
            return users.Select(u => new
            {
                u.Id, u.Username, u.Email,
                Role = u.Role.ToString(),
                u.CreatedUtc,
            });
        });

        // POST /api/users — admin creates a user
        g.MapPost("/", async (
            [FromBody] CreateUserRequest req,
            AppDbContext db,
            AuthService auth,
            EmailService email) =>
        {
            if (await db.Users.AnyAsync(u => u.Username == req.Username))
                return Results.Conflict(new { message = "Username already taken." });
            if (await db.Users.AnyAsync(u => u.Email == req.Email))
                return Results.Conflict(new { message = "Email already registered." });

            var user = new AppUser
            {
                Id           = Guid.NewGuid().ToString(),
                Username     = req.Username,
                Email        = req.Email,
                PasswordHash = auth.HashPassword(req.Password),
                Role         = req.Role == "Admin" ? UserRole.Admin : UserRole.User,
                CreatedUtc   = DateTime.UtcNow,
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            _ = email.SendAccountCreatedAsync(user.Email, user.Username);

            return Results.Created($"/api/users/{user.Id}", new { user.Id });
        });

        // PUT /api/users/{id}
        g.MapPut("/{id}", async (
            string id,
            [FromBody] EditUserRequest req,
            AppDbContext db,
            AuthService auth) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            if (!string.IsNullOrWhiteSpace(req.Username)) user.Username = req.Username;
            if (!string.IsNullOrWhiteSpace(req.Email))    user.Email    = req.Email;
            if (!string.IsNullOrWhiteSpace(req.Password)) user.PasswordHash = auth.HashPassword(req.Password);
            if (req.Role is not null)
                user.Role = req.Role == "Admin" ? UserRole.Admin : UserRole.User;

            await db.SaveChangesAsync();
            return Results.Ok();
        });

        // DELETE /api/users/{id} — by user ID
        g.MapDelete("/{id}", async (
            string id,
            ClaimsPrincipal principal,
            AppDbContext db,
            SessionRegistry sessions,
            EmailService email) =>
        {
            if (id == principal.UserId())
                return Results.BadRequest(new { message = "Cannot delete your own account." });

            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            if (user.Role == UserRole.Admin)
                return Results.BadRequest(new { message = "Cannot delete an admin account." });

            foreach (var key in sessions.ConnectedIds.Where(k => k.StartsWith($"{id}:")))
                sessions.Remove(key);

            var savedEmail    = user.Email;
            var savedUsername = user.Username;

            db.Users.Remove(user);
            await db.SaveChangesAsync();

            _ = email.SendAccountDeletedAsync(savedEmail, savedUsername);

            return Results.Ok();
        });

        // DELETE /api/users/by-email/{email} — admin deletes by email address
        g.MapDelete("/by-email/{emailAddress}", async (
            string emailAddress,
            ClaimsPrincipal principal,
            AppDbContext db,
            SessionRegistry sessions,
            EmailService email) =>
        {
            var decoded = Uri.UnescapeDataString(emailAddress);
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == decoded);
            if (user is null) return Results.NotFound(new { message = $"No user with email {decoded}." });

            if (user.Id == principal.UserId())
                return Results.BadRequest(new { message = "Cannot delete your own account." });

            if (user.Role == UserRole.Admin)
                return Results.BadRequest(new { message = "Cannot delete an admin account." });

            foreach (var key in sessions.ConnectedIds.Where(k => k.StartsWith($"{user.Id}:")))
                sessions.Remove(key);

            var savedEmail    = user.Email;
            var savedUsername = user.Username;

            db.Users.Remove(user);
            await db.SaveChangesAsync();

            _ = email.SendAccountDeletedAsync(savedEmail, savedUsername);

            return Results.Ok(new { message = $"User {savedUsername} ({savedEmail}) deleted." });
        });

        // POST /api/users/{id}/reset-password — admin resets password, user gets email
        g.MapPost("/{id}/reset-password", async (
            string id,
            [FromBody] ResetPasswordRequest req,
            AppDbContext db,
            AuthService auth,
            EmailService email) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            user.PasswordHash = auth.HashPassword(req.NewPassword);
            await db.SaveChangesAsync();

            _ = email.SendPasswordChangedAsync(user.Email, user.Username);

            return Results.Ok();
        });
    }
}

public record CreateUserRequest(string Username, string Email, string Password, string Role);
public record EditUserRequest(string? Username, string? Email, string? Password, string? Role);
public record ResetPasswordRequest(string NewPassword);
