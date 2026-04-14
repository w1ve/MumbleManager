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

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MumbleManager.Services;

public class EmailService
{
    private readonly IConfiguration    _config;
    private readonly ILogger<EmailService> _log;

    // Config keys
    private string SmtpHost     => _config["Email:SmtpHost"]     ?? "smtp.gmail.com";
    private int    SmtpPort     => int.Parse(_config["Email:SmtpPort"] ?? "587");
    private string FromAddress  => _config["Email:From"]         ?? "YOUR_FROM_EMAIL@example.com";
    private string FromName     => _config["Email:FromName"]     ?? "MumbleManager";
    private string AppPassword  => _config["Email:AppPassword"]  ?? "";
    private string AdminAddress => _config["Email:AdminAddress"] ?? "YOUR_FROM_EMAIL@example.com";

    public EmailService(IConfiguration config, ILogger<EmailService> log)
    {
        _config = config;
        _log    = log;
    }

    // ── Public send methods ───────────────────────────────────────────────────

    public async Task SendAccountCreatedAsync(string toEmail, string toName)
    {
        await SendAsync(
            to:      toEmail,
            toName:  toName,
            subject: "Welcome to MumbleManager",
            body:    $@"
<p>Hi {toName},</p>
<p>Your MumbleManager account has been created successfully.</p>
<p>You can log in at any time using your username and password.</p>
<p>If you did not request this account, please contact the administrator.</p>
<br/><p style='color:#666;font-size:12px'>— MumbleManager</p>");

        await SendAsync(
            to:      AdminAddress,
            toName:  "MumbleManager Admin",
            subject: $"New account created: {toName} ({toEmail})",
            body:    $@"
<p>A new user account has been created on MumbleManager.</p>
<ul>
  <li><b>Username:</b> {toName}</li>
  <li><b>Email:</b> {toEmail}</li>
  <li><b>Time:</b> {DateTime.UtcNow:u} UTC</li>
</ul>");
    }

    public async Task SendPasswordChangedAsync(string toEmail, string toName)
    {
        await SendAsync(
            to:      toEmail,
            toName:  toName,
            subject: "MumbleManager — Password Changed",
            body:    $@"
<p>Hi {toName},</p>
<p>Your MumbleManager password was changed successfully.</p>
<p>If you did not make this change, please contact the administrator immediately.</p>
<br/><p style='color:#666;font-size:12px'>— MumbleManager</p>");
    }

    public async Task SendAccountDeletedAsync(string toEmail, string toName)
    {
        await SendAsync(
            to:      toEmail,
            toName:  toName,
            subject: "MumbleManager — Account Deleted",
            body:    $@"
<p>Hi {toName},</p>
<p>Your MumbleManager account has been deleted by an administrator.</p>
<p>All associated hosts and templates have been removed.</p>
<p>If you believe this was in error, please contact the administrator.</p>
<br/><p style='color:#666;font-size:12px'>— MumbleManager</p>");
    }

    public async Task SendFatalErrorAsync(string errorMessage, string? stackTrace = null)
    {
        await SendAsync(
            to:      AdminAddress,
            toName:  "MumbleManager Admin",
            subject: "MumbleManager — Fatal Error",
            body:    $@"
<p><b>A fatal error occurred in MumbleManager:</b></p>
<pre style='background:#111;color:#f66;padding:12px;border-radius:4px;font-size:12px'>{System.Net.WebUtility.HtmlEncode(errorMessage)}</pre>
{(stackTrace is not null ? $"<pre style='background:#111;color:#aaa;padding:12px;border-radius:4px;font-size:11px'>{System.Net.WebUtility.HtmlEncode(stackTrace)}</pre>" : "")}
<p style='color:#666;font-size:12px'>{DateTime.UtcNow:u} UTC</p>");
    }

    // ── Core sender ───────────────────────────────────────────────────────────

    private async Task SendAsync(string to, string toName, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(AppPassword))
        {
            _log.LogWarning("Email not configured — skipping send to {To}", to);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(FromName, FromAddress));
            message.To.Add(new MailboxAddress(toName, to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = WrapHtml(subject, body) };

            using var client = new SmtpClient();
            await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(FromAddress, AppPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _log.LogInformation("Email sent: {Subject} → {To}", subject, to);
        }
        catch (Exception ex)
        {
            // Log but never throw — email failures should not break app flow
            _log.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
        }
    }

    private static string WrapHtml(string title, string body) => $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'/>
  <style>
    body {{ font-family: 'Helvetica Neue', Arial, sans-serif; background: #0e1014; color: #d2d7dc; margin: 0; padding: 0; }}
    .wrap {{ max-width: 520px; margin: 40px auto; background: #1a1d23; border: 1px solid #303642; border-radius: 6px; overflow: hidden; }}
    .header {{ background: #121418; padding: 20px 28px; border-bottom: 1px solid #303642; }}
    .header h1 {{ margin: 0; font-size: 18px; color: #00bc8c; letter-spacing: .04em; }}
    .header p  {{ margin: 4px 0 0; font-size: 11px; color: #6e7887; text-transform: uppercase; letter-spacing: .06em; }}
    .body {{ padding: 24px 28px; font-size: 14px; line-height: 1.7; color: #d2d7dc; }}
    .body a {{ color: #00bc8c; }}
    pre {{ font-size: 12px; overflow-x: auto; }}
    ul {{ padding-left: 20px; }}
    li {{ margin-bottom: 4px; }}
  </style>
</head>
<body>
  <div class='wrap'>
    <div class='header'>
      <h1>MumbleManager</h1>
      <p>Murmur Server Administration</p>
    </div>
    <div class='body'>{body}</div>
  </div>
</body>
</html>";
}
