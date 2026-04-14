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

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MumbleManager.Models;

namespace MumbleManager.Services;

public class AuthService
{
    private readonly IConfiguration _config;

    public AuthService(IConfiguration config) => _config = config;

    // ── Password hashing (BCrypt-style using PBKDF2) ──────────────────────────
    public string HashPassword(string password)
    {
        var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var hash = Pbkdf2(password, salt);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split(':');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Pbkdf2(password, salt);
        return CryptographicEquals(actual, expected);
    }

    private static byte[] Pbkdf2(string password, byte[] salt) =>
        System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt,
            iterations: 100_000,
            hashAlgorithm: System.Security.Cryptography.HashAlgorithmName.SHA256,
            outputLength: 32);

    private static bool CryptographicEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        int diff = 0;
        for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }

    // ── JWT ────────────────────────────────────────────────────────────────────
    public string GenerateToken(AppUser user)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name,           user.Username),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Role,           user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:   "MumbleManager",
            audience: "MumbleManager",
            claims:   claims,
            expires:  expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string JwtSecret => _config["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret not configured.");
}

// ── Extension helpers for reading claims ──────────────────────────────────────
public static class ClaimsPrincipalExtensions
{
    public static string UserId(this ClaimsPrincipal p) =>
        p.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("No user id in token.");

    public static bool IsAdmin(this ClaimsPrincipal p) =>
        p.IsInRole("Admin");
}

// ── Extension helpers for per-browser session key ─────────────────────────────
public static class HttpRequestExtensions
{
    /// <summary>
    /// Extracts the per-browser client key from the Authorization header.
    /// Each browser/device gets its own JWT token, so the token tail is unique per session.
    /// </summary>
    public static string ClientKey(this HttpRequest request)
    {
        var auth = request.Headers.Authorization.FirstOrDefault() ?? "";
        var token = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? auth["Bearer ".Length..].Trim()
            : auth;
        return SessionRegistry.ClientKey(token);
    }
}
