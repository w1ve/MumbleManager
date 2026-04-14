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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MumbleManager.Data;
using MumbleManager.Models;
using MumbleManager.Services;

namespace MumbleManager.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/auth").WithTags("Auth");

        // POST /api/auth/login
        g.MapPost("/login", async (
            [FromBody] LoginRequest req,
            AppDbContext db,
            AuthService auth) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u =>
                u.Username == req.UsernameOrEmail || u.Email == req.UsernameOrEmail);

            if (user is null || !auth.VerifyPassword(req.Password, user.PasswordHash))
                return Results.Unauthorized();

            var token = auth.GenerateToken(user);
            return Results.Ok(new
            {
                token,
                user = new { user.Id, user.Username, user.Email, Role = user.Role.ToString() }
            });
        });

        // POST /api/auth/register — public self-registration (creates User role only)
        g.MapPost("/register", async (
            [FromBody] RegisterRequest req,
            AppDbContext db,
            AuthService auth,
            EmailService email) =>
        {
            if (string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.Email)    ||
                string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { message = "All fields are required." });

            if (req.Password.Length < 6)
                return Results.BadRequest(new { message = "Password must be at least 6 characters." });

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
                Role         = UserRole.User,   // self-registered users are never admin
                CreatedUtc   = DateTime.UtcNow,
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Fire-and-forget emails (don't block the response)
            _ = email.SendAccountCreatedAsync(user.Email, user.Username);

            return Results.Created($"/api/auth/register", new
            {
                message = "Account created. You can now log in."
            });
        });

        // GET /api/auth/me
        g.MapGet("/me", (ClaimsPrincipal principal) =>
            Results.Ok(new
            {
                id       = principal.UserId(),
                username = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
                email    = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
                role     = principal.IsAdmin() ? "Admin" : "User",
            })
        ).RequireAuthorization();

        // POST /api/auth/change-password
        g.MapPost("/change-password", async (
            [FromBody] ChangePasswordRequest req,
            ClaimsPrincipal principal,
            AppDbContext db,
            AuthService auth,
            EmailService email) =>
        {
            var user = await db.Users.FindAsync(principal.UserId());
            if (user is null) return Results.Unauthorized();

            if (!auth.VerifyPassword(req.CurrentPassword, user.PasswordHash))
                return Results.BadRequest(new { message = "Current password is incorrect." });

            user.PasswordHash = auth.HashPassword(req.NewPassword);
            await db.SaveChangesAsync();

            _ = email.SendPasswordChangedAsync(user.Email, user.Username);

            return Results.Ok();
        }).RequireAuthorization();
    }
}

public record LoginRequest(string UsernameOrEmail, string Password);
public record RegisterRequest(string Username, string Email, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
