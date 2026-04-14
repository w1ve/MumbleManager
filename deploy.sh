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

using Microsoft.AspNetCore.Diagnostics;

namespace MumbleManager.Services;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _log;
    private readonly IServiceProvider _services;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> log,
        IServiceProvider services)
    {
        _log      = log;
        _services = services;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx,
        Exception ex,
        CancellationToken ct)
    {
        _log.LogError(ex, "Unhandled exception on {Method} {Path}",
            ctx.Request.Method, ctx.Request.Path);

        // Send email alert — fire and forget, never throws
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope   = _services.CreateScope();
                var emailService  = scope.ServiceProvider.GetRequiredService<EmailService>();
                await emailService.SendFatalErrorAsync(
                    $"{ex.GetType().Name}: {ex.Message}\n\nPath: {ctx.Request.Method} {ctx.Request.Path}",
                    ex.StackTrace);
            }
            catch { /* never propagate */ }
        }, CancellationToken.None);

        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await ctx.Response.WriteAsJsonAsync(new
        {
            status  = 500,
            message = "An unexpected error occurred.",
        }, ct);

        return true;
    }
}
