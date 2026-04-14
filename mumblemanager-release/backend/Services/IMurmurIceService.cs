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

public interface IMurmurIceService : IDisposable
{
    bool             IsConnected { get; }
    MurmurVersionInfo Version    { get; }

    void Connect(int localTunnelPort, string iceSecret = "");

    Task<List<VirtualServerConfig>>       GetAllServersAsync();
    Task                                  WriteConfigAsync(int serverId, VirtualServerConfig cfg);
    Task<VirtualServerConfig?>            GetServerByIdAsync(int id);
    Task<HashSet<int>>                    GetUsedPortsAsync();
    Task<int>                             CreateNewServerAsync(int port);
    Task                                  StartServerAsync(int serverId);
    Task                                  StopServerAsync(int serverId);
    Task                                  DeleteServerAsync(int serverId);

    Task<Dictionary<int, ChannelInfo>>    GetChannelsAsync(int serverId);
    Task<int>                             AddChannelAsync(int serverId, string name, int parentId);
    Task                                  RemoveChannelAsync(int serverId, int channelId);
    Task                                  SetChannelNameAsync(int serverId, int channelId, string name);
    Task                                  DeleteAllChannelsAsync(int serverId);
    Task                                  ApplyTemplateAsync(int serverId, ChannelTemplate template,
                                              IProgress<string>? progress = null);
}

public class ChannelInfo
{
    public int    Id          { get; set; }
    public int    ParentId    { get; set; }
    public string Name        { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int    Position    { get; set; }
    public bool   Temporary   { get; set; }
}
