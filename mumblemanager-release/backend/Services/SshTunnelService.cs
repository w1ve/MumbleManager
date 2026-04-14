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

using Renci.SshNet;
using MumbleManager.Models;
using System.Net;
using System.Net.Sockets;

namespace MumbleManager.Services;

/// <summary>
/// Manages one SSH connection + local-port-forward tunnel to Murmur's Ice port (6502).
/// Ported from WinForms — fully cross-platform (SSH.NET runs on Linux).
/// </summary>
public class SshTunnelService : IDisposable
{
    private SshClient?          _ssh;
    private ForwardedPortLocal? _tunnel;
    private bool                _disposed;

    public int  LocalPort   { get; private set; }
    public bool IsConnected => _ssh?.IsConnected == true;

    public async Task ConnectAsync(SshHostEntry host, CancellationToken ct = default)
    {
        if (_ssh is not null) Dispose();

        LocalPort = GetFreePort();

        var info = new Renci.SshNet.ConnectionInfo(
            host.Host,
            host.SshPort,
            host.Username,
            new PasswordAuthenticationMethod(host.Username, host.Password))
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        _ssh = new SshClient(info);

        try
        {
            await Task.Run(() => _ssh.Connect(), ct);
        }
        catch (Exception ex)
        {
            try { _ssh.Dispose(); } catch { }
            _ssh = null;
            throw new InvalidOperationException(
                $"SSH connection to {host.Host}:{host.SshPort} failed: " +
                (ex.InnerException?.Message ?? ex.Message), ex);
        }

        _tunnel = new ForwardedPortLocal("127.0.0.1", (uint)LocalPort, "127.0.0.1", 6502);
        _ssh.AddForwardedPort(_tunnel);
        _tunnel.Start();
    }

    public async Task<MurmurVersionInfo> DetectVersionAsync()
    {
        if (_ssh is null || !_ssh.IsConnected) return MurmurVersionInfo.Unknown;

        var candidates = new[] { ("mumble-server", "1.5"), ("murmurd", "1.4") };

        foreach (var (binary, _) in candidates)
        {
            try
            {
                var output = await RunCommandAsync($"{binary} --version 2>&1 || {binary} -v 2>&1");
                if (string.IsNullOrWhiteSpace(output)) continue;

                var version = ParseVersionFromOutput(output);
                if (version is null) continue;

                var family = binary == "mumble-server"
                    ? MurmurVersionFamily.V15
                    : MurmurVersionFamily.V14;

                return new MurmurVersionInfo(family, version, binary);
            }
            catch { }
        }

        try
        {
            var ps = await RunCommandAsync(
                "ps aux 2>/dev/null | grep -E 'mumble-server|murmurd' | grep -v grep | head -1");
            if (ps.Contains("mumble-server"))
                return new MurmurVersionInfo(MurmurVersionFamily.V15, "1.5.x", "mumble-server");
            if (ps.Contains("murmurd"))
                return new MurmurVersionInfo(MurmurVersionFamily.V14, "1.4.x", "murmurd");
        }
        catch { }

        return MurmurVersionInfo.Unknown;
    }

    private static string? ParseVersionFromOutput(string output)
    {
        var match = System.Text.RegularExpressions.Regex.Match(output, @"\b(\d+\.\d+\.\d+)\b");
        return match.Success ? match.Groups[1].Value : null;
    }

    public async Task<bool> IsUfwActiveAsync()
    {
        if (_ssh is null || !_ssh.IsConnected)
            throw new InvalidOperationException("SSH not connected.");
        var result = await RunCommandAsync("sudo ufw status | head -1");
        return result.Contains("active", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> OpenPortInUfwAsync(int port)
    {
        if (_ssh is null || !_ssh.IsConnected)
            throw new InvalidOperationException("SSH not connected.");
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(await RunCommandAsync($"sudo ufw allow {port}/tcp"));
        sb.AppendLine(await RunCommandAsync($"sudo ufw allow {port}/udp"));
        sb.AppendLine(await RunCommandAsync("sudo ufw reload"));
        return sb.ToString();
    }

    public Task<string> RunCommandAsync(string command)
    {
        return Task.Run(() =>
        {
            using var cmd = _ssh!.CreateCommand(command);
            cmd.Execute();
            return (cmd.Result + cmd.Error).Trim();
        });
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _tunnel?.Stop(); }  catch { }
        try { _ssh?.Disconnect(); } catch { }
        _tunnel?.Dispose();
        _ssh?.Dispose();
    }
}
