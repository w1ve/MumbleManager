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

using Ice;
using MumbleManager.Models;

namespace MumbleManager.Services
{
    /// <summary>
    /// IMurmurIceService implementation for Murmur 1.4.x.
    /// Uses the Murmur namespace generated from Murmur.ice (the 1.4.x slice file).
    ///
    /// SETUP: Run slice2cs on the 1.4.x slice before building:
    ///   Invoke-WebRequest https://raw.githubusercontent.com/mumble-voip/mumble/v1.4.287/src/murmur/Murmur.ice -OutFile Murmur14.ice
    ///   $slice2cs = "$env:USERPROFILE\.nuget\packages\zeroc.ice.net\3.7.11\tools\windows-x64\slice2cs.exe"
    ///   $iceSlice = "$env:USERPROFILE\.nuget\packages\zeroc.ice.net\3.7.11\slice"
    ///   & $slice2cs Murmur14.ice -I"$iceSlice" --output-dir Generated\V14
    ///
    /// The generated file will use namespace Murmur — add Generated\V14\*.cs to the project.
    /// </summary>
    public class MurmurLegacyIceService : IMurmurIceService
    {
        private Communicator?     _ice;
        private Murmur.MetaPrx?   _meta;

        public bool             IsConnected => _meta is not null;
        public MurmurVersionInfo Version     { get; private set; } = MurmurVersionInfo.Unknown;

        // ── Config key constants (identical between 1.4 and 1.5) ─────────────
        private const string K_Slots           = "users";
        private const string K_ServerName      = "registername";
        private const string K_Password        = "password";
        private const string K_DefaultChannel  = "defaultchannel";
        private const string K_WelcomeMessage  = "welcometext";
        private const string K_AllowHtml       = "allowhtml";
        private const string K_AllowPing       = "allowping";
        private const string K_UserBandwidth   = "userbandwidth";
        private const string K_UserTimeout     = "usertimeout";
        private const string K_MaxMsgLen       = "textmessagelength";
        private const string K_RememberChannel = "rememberchannel";

        // ── Connect ──────────────────────────────────────────────────────────

        public void Connect(int localTunnelPort, string iceSecret = "")
        {
            Dispose();

            var props = Util.createProperties();
            props.setProperty("Ice.Default.EncodingVersion", "1.0");
            props.setProperty("Ice.MessageSizeMax", "65536");
            if (!string.IsNullOrEmpty(iceSecret))
                props.setProperty("Ice.ImplicitContext", "Shared");

            _ice = Util.initialize(new InitializationData { properties = props });

            if (!string.IsNullOrEmpty(iceSecret))
                _ice.getImplicitContext().put("secret", iceSecret);

            var proxy = _ice.stringToProxy($"Meta:tcp -h 127.0.0.1 -p {localTunnelPort}");
            _meta = Murmur.MetaPrxHelper.checkedCast(proxy)
                ?? throw new InvalidOperationException(
                    "Could not reach Murmur 1.4.x Ice Meta interface.");
        }

        public void SetVersion(MurmurVersionInfo v) => Version = v;

        // ── Tunnel proxy helper ───────────────────────────────────────────────

        private Murmur.ServerPrx Tunnel(Murmur.ServerPrx srv) =>
            Murmur.ServerPrxHelper.uncheckedCast(
                srv.ice_endpoints(_meta!.ice_getEndpoints()));

        // ── Server enumeration ───────────────────────────────────────────────

        public async Task<List<VirtualServerConfig>> GetAllServersAsync()
        {
            EnsureConnected();
            var servers = await Task.Run(() => _meta!.getAllServers());
            var result  = new List<VirtualServerConfig>();
            foreach (var s in servers)
                result.Add(await ReadConfigAsync(Tunnel(s)));
            return result;
        }

        // ── Config read/write ────────────────────────────────────────────────

        private async Task<VirtualServerConfig> ReadConfigAsync(Murmur.ServerPrx srv)
        {
            return await Task.Run(() =>
            {
                string GetConf(string key, string def = "") =>
                    TryGetConf(srv, key, out var v) ? v : def;

                var cfg = new VirtualServerConfig
                {
                    ServerId         = srv.id(),
                    IsRunning        = srv.isRunning(),
                    ServerName       = GetConf(K_ServerName),
                    ServerPassword   = GetConf(K_Password),
                    DefaultChannel   = GetConf(K_DefaultChannel, "0"),
                    WelcomeMessage   = GetConf(K_WelcomeMessage),
                    AllowHtml        = ParseBool(GetConf(K_AllowHtml,        "true")),
                    AllowPing        = ParseBool(GetConf(K_AllowPing,        "true")),
                    UserBandwidth    = ParseInt (GetConf(K_UserBandwidth,    "72000"), 72000),
                    UserTimeout      = ParseInt (GetConf(K_UserTimeout,      "30"),    30),
                    MaxMessageLength = ParseInt (GetConf(K_MaxMsgLen,        "5000"),  5000),
                    RememberChannel  = ParseBool(GetConf(K_RememberChannel,  "false")),
                    Slots            = ParseInt (GetConf(K_Slots,            "10"),    10),
                };
                try { cfg.Port = srv.getConf("port") is { Length: > 0 } p ? int.Parse(p) : 64738; }
                catch { cfg.Port = 64738; }
                return cfg;
            });
        }

        public async Task WriteConfigAsync(int serverId, VirtualServerConfig cfg)
        {
            var srv = await GetSrvAsync(serverId);
            await Task.Run(() =>
            {
                srv.setConf(K_ServerName,      cfg.ServerName);
                srv.setConf(K_Password,        cfg.ServerPassword);
                srv.setConf(K_DefaultChannel,  cfg.DefaultChannel);
                srv.setConf(K_WelcomeMessage,  cfg.WelcomeMessage);
                srv.setConf(K_AllowHtml,       cfg.AllowHtml       ? "true" : "false");
                srv.setConf(K_AllowPing,       cfg.AllowPing       ? "true" : "false");
                srv.setConf(K_UserBandwidth,   cfg.UserBandwidth.ToString());
                srv.setConf(K_UserTimeout,     cfg.UserTimeout.ToString());
                srv.setConf(K_MaxMsgLen,       cfg.MaxMessageLength.ToString());
                srv.setConf(K_RememberChannel, cfg.RememberChannel ? "true" : "false");
                srv.setConf(K_Slots,           cfg.Slots.ToString());
                srv.setConf("port",            cfg.Port.ToString());
            });
        }

        public async Task<VirtualServerConfig?> GetServerByIdAsync(int id)
        {
            EnsureConnected();
            try { return await ReadConfigAsync(await GetSrvAsync(id)); }
            catch { return null; }
        }

        // 1.4.x has no MumbleServer.ServerPrx — always returns null
        public Task<MumbleServer.ServerPrx?> GetServerPrxByIdAsync(int id) =>
            Task.FromResult<MumbleServer.ServerPrx?>(null);

        // ── Port / creation ──────────────────────────────────────────────────

        public async Task<HashSet<int>> GetUsedPortsAsync()
        {
            EnsureConnected();
            return await Task.Run(() =>
            {
                var ports = new HashSet<int>();
                foreach (var s in _meta!.getAllServers())
                    try { if (int.TryParse(Tunnel(s).getConf("port"), out var p)) ports.Add(p); }
                    catch { }
                return ports;
            });
        }

        public async Task<int> CreateNewServerAsync(int port)
        {
            EnsureConnected();
            return await Task.Run(() =>
            {
                var srv = Tunnel(_meta!.newServer());
                srv.setConf("port", port.ToString());
                return srv.id();
            });
        }

        public async Task StartServerAsync(int serverId)
        {
            var srv = await GetSrvAsync(serverId);
            await Task.Run(() => srv.start());
        }

        public async Task StopServerAsync(int serverId)
        {
            var srv = await GetSrvAsync(serverId);
            await Task.Run(() => srv.stop());
        }

        public async Task DeleteServerAsync(int serverId)
        {
            EnsureConnected();
            await Task.Run(() =>
            {
                var all   = _meta!.getAllServers();
                var match = all.FirstOrDefault(s => Tunnel(s).id() == serverId);
                if (match is null) return;
                var srv = Tunnel(match);
                try { if (srv.isRunning()) srv.stop(); } catch { }
                match.delete();
            });
        }

        // ── Channel management ────────────────────────────────────────────────

        public async Task<Dictionary<int, ChannelInfo>> GetChannelsAsync(int serverId)
        {
            var srv = await GetSrvAsync(serverId);
            return await Task.Run(() =>
                srv.getChannels().ToDictionary(
                    kv => kv.Key,
                    kv => new ChannelInfo
                    {
                        Id          = kv.Value.id,
                        ParentId    = kv.Value.parent,
                        Name        = kv.Value.name,
                        Description = kv.Value.description,     
                        Position    = kv.Value.position,
                        Temporary   = kv.Value.temporary,
                    }));
        }

        public async Task<int> AddChannelAsync(int serverId, string name, int parentId)
        {
            var srv = await GetSrvAsync(serverId);
            return await Task.Run(() => srv.addChannel(name, parentId));
        }

        public async Task RemoveChannelAsync(int serverId, int channelId)
        {
            if (channelId == 0) throw new ArgumentException("Cannot remove Root channel.");
            var srv = await GetSrvAsync(serverId);
            await Task.Run(() => srv.removeChannel(channelId));
        }

        public async Task SetChannelNameAsync(int serverId, int channelId, string name)
        {
            var srv = await GetSrvAsync(serverId);
            await Task.Run(() =>
            {
                var channels = srv.getChannels();
                if (!channels.TryGetValue(channelId, out var ch)) return;
                ch.name = name;
                srv.setChannelState(ch);
            });
        }

        public async Task DeleteAllChannelsAsync(int serverId)
        {
            var srv = await GetSrvAsync(serverId);
            await Task.Run(() =>
            {
                var rootChildren = srv.getChannels().Values
                    .Where(c => c.parent == 0 && c.id != 0)
                    .Select(c => c.id).ToList();
                foreach (var id in rootChildren)
                    try { srv.removeChannel(id); } catch { }
            });
        }

        public async Task ApplyTemplateAsync(int serverId, Models.ChannelTemplate template,
            IProgress<string>? progress = null)
        {
            progress?.Report("Clearing existing channels…");
            await DeleteAllChannelsAsync(serverId);
            var srv = await GetSrvAsync(serverId);
            progress?.Report("Creating channels from template…");
            await Task.Run(() => CreateNodesRecursive(srv, template.RootChildren, 0, progress));
            progress?.Report("Template applied.");
        }

        private static void CreateNodesRecursive(Murmur.ServerPrx srv,
            List<Models.ChannelNode> nodes, int parentId, IProgress<string>? progress)
        {
            int pos = 0;
            foreach (var node in nodes)
            {
                progress?.Report($"Creating: {node.Name}");
                int newId = srv.addChannel(node.Name, parentId);
                if (!string.IsNullOrEmpty(node.Description) || node.Position != 0 || node.Temporary)
                {
                    var channels = srv.getChannels();
                    if (channels.TryGetValue(newId, out var ch))
                    {
                        ch.description    = node.Description;  
                        ch.position = pos;
                        ch.temporary = node.Temporary;
                        srv.setChannelState(ch);
                    }
                }
                pos++;
                if (node.Children.Count > 0)
                    CreateNodesRecursive(srv, node.Children, newId, progress);
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private async Task<Murmur.ServerPrx> GetSrvAsync(int id)
        {
            EnsureConnected();
            return await Task.Run(() =>
            {
                var all   = _meta!.getAllServers();
                var match = all.FirstOrDefault(s => Tunnel(s).id() == id);
                return match is not null ? Tunnel(match)
                    : throw new InvalidOperationException($"Server id {id} not found.");
            });
        }

        private void EnsureConnected()
        {
            if (_meta is null) throw new InvalidOperationException("Not connected to Murmur Ice.");
        }

        private static bool TryGetConf(Murmur.ServerPrx srv, string key, out string value)
        {
            try { value = srv.getConf(key); return true; }
            catch { value = string.Empty; return false; }
        }

        private static int  ParseInt (string s, int  def) => int.TryParse(s, out var v) ? v : def;
        private static bool ParseBool(string s) =>
            s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1";

        public void Dispose()
        {
            try { _ice?.destroy(); } catch { }
            _ice  = null;
            _meta = null;
        }
    }
}
