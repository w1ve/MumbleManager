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

using MumbleManager.Models;

namespace MumbleManager.Services;

/// <summary>
/// Represents a live connection to one SSH host (SSH tunnel + Ice RPC).
/// Web-adapted from WinForms: no UI references, thread-safe, injectable.
/// </summary>
public class HostSession : IDisposable
{
    public SshHostEntry       Host    { get; }
    public SshTunnelService   Tunnel  { get; } = new();
    public IMurmurIceService  Ice     { get; private set; } = null!;
    public MurmurVersionInfo  Version => Ice?.Version ?? MurmurVersionInfo.Unknown;

    public HostSession(SshHostEntry host) => Host = host;

    public async Task ConnectAsync(IProgress<string>? progress = null,
                                   CancellationToken ct = default)
    {
        progress?.Report($"Opening SSH tunnel to {Host.Host}:{Host.SshPort}…");
        await Tunnel.ConnectAsync(Host, ct);

        progress?.Report("Detecting Mumble server version…");
        var version = await Tunnel.DetectVersionAsync();

        if (version.Family == MurmurVersionFamily.Unknown)
        {
            progress?.Report("Version unknown — assuming 1.5.x.");
            version = new MurmurVersionInfo(MurmurVersionFamily.V15, "unknown", "mumble-server");
        }
        else
        {
            progress?.Report($"Detected: {version}");
        }

        Ice = version.Family switch
        {
            MurmurVersionFamily.V14 => new MurmurLegacyIceService(),
            _                       => new MumbleServerIceService(),
        };

        progress?.Report($"Connecting to Murmur Ice (port {Tunnel.LocalPort})…");
        await Task.Run(() => Ice.Connect(Tunnel.LocalPort, Host.IceSecret), ct);

        if (Ice is MumbleServerIceService  ms15) ms15.SetVersion(version);
        if (Ice is MurmurLegacyIceService  ms14) ms14.SetVersion(version);

        progress?.Report($"Connected — {version}.");
    }

    public void Dispose()
    {
        try { Ice?.Dispose(); }    catch { }
        try { Tunnel.Dispose(); }  catch { }
    }
}

/// <summary>
/// Singleton service that holds all active HostSessions across HTTP requests.
/// Keyed by "userId:hostId:clientKey" where clientKey is a per-browser-session
/// identifier so multiple browsers/devices can connect to the same host
/// independently without interfering with each other.
/// </summary>
public class SessionRegistry
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, HostSession>
        _sessions = new();

    /// <summary>
    /// Derives a short stable key from the raw JWT token so each browser
    /// session (which has its own token) gets its own bucket.
    /// </summary>
    public static string ClientKey(string? rawToken)
    {
        if (string.IsNullOrEmpty(rawToken)) return "default";
        // Use last 16 chars of the token signature — unique per issued token
        return rawToken.Length > 16 ? rawToken[^16..] : rawToken;
    }

    private static string Key(string userId, string hostId, string clientKey)
        => $"{userId}:{hostId}:{clientKey}";

    public HostSession? Get(string userId, string hostId, string clientKey) =>
        _sessions.TryGetValue(Key(userId, hostId, clientKey), out var s) ? s : null;

    /// <summary>
    /// Finds the session for this specific browser (by clientKey).
    /// Falls back to any session for this userId:hostId if clientKey not found
    /// (supports API calls where clientKey routing isn't available).
    /// </summary>
    public HostSession? Get(string userId, string hostId) =>
        Get(userId, hostId, string.Empty) // try empty first
        ?? _sessions.FirstOrDefault(kv => kv.Key.StartsWith($"{userId}:{hostId}:")).Value;

    public bool IsConnected(string userId, string hostId, string clientKey) =>
        _sessions.ContainsKey(Key(userId, hostId, clientKey));

    /// <summary>Is ANY browser of this userId connected to this hostId?</summary>
    public bool IsAnyConnected(string userId, string hostId)
        => _sessions.Keys.Any(k => k.StartsWith($"{userId}:{hostId}:"));

    public IReadOnlyCollection<string> ConnectedIds => _sessions.Keys.ToList();

    public void Add(string userId, string hostId, string clientKey, HostSession session) =>
        _sessions[Key(userId, hostId, clientKey)] = session;

    public bool Remove(string userId, string hostId, string clientKey)
        => Remove(Key(userId, hostId, clientKey));

    public bool Remove(string key)
    {
        if (_sessions.TryRemove(key, out var session))
        {
            try { session.Dispose(); } catch { }
            return true;
        }
        return false;
    }

    public void DisconnectAll()
    {
        foreach (var key in _sessions.Keys.ToList())
            Remove(key);
    }
}
