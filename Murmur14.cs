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

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MumbleManager.Data;
using MumbleManager.Endpoints;
using MumbleManager.Hubs;
using MumbleManager.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret must be set.");

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")
                  ?? "Data Source=/data/mumblemanager.db");
    opt.ConfigureWarnings(w =>
        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

builder.Services.AddSingleton<SessionRegistry>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EmailService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = "MumbleManager",
            ValidAudience            = "MumbleManager",
            IssuerSigningKey         =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        };
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                var path  = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(opt =>
    opt.AddPolicy("AdminOnly", p => p.RequireRole("Admin")));

builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "MumbleManager API", Version = "v1" }));

builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p =>
        p.WithOrigins(
            builder.Configuration["AllowedOrigins"]?.Split(',')
            ?? new[] { "http://localhost:5173" })
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials()));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ── Build ─────────────────────────────────────────────────────────────────────

var app = builder.Build();

await DbSeeder.SeedAsync(app.Services);

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// ── Routes ────────────────────────────────────────────────────────────────────

app.MapHub<StatusHub>("/hubs/status");

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapHostEndpoints();
app.MapConnectionEndpoints();
app.MapServerEndpoints();
app.MapChannelEndpoints();
app.MapTemplateEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
