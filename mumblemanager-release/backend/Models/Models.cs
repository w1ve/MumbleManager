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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace MumbleManager.Models;

// ── SSH Host ────────────────────────────────────────────────────────────────

public class SshHostEntry
{
    public string Id          { get; set; } = Guid.NewGuid().ToString();
    public string UserId      { get; set; } = string.Empty;   // FK → AppUser
    public string DisplayName { get; set; } = string.Empty;
    public string Host        { get; set; } = string.Empty;
    public int    SshPort     { get; set; } = 22;
    public string Username    { get; set; } = string.Empty;
    public string Password    { get; set; } = string.Empty;
    public string IceSecret   { get; set; } = string.Empty;

    public List<VirtualServerCache> CachedServers { get; set; } = new();
}

// ── Virtual Server ──────────────────────────────────────────────────────────

/// <summary>
/// EF entity — extends VirtualServerConfig with a FK back to SshHostEntry.
/// </summary>
public class VirtualServerCache
{
    [Key]
    public int    RowId      { get; set; }          // EF PK (auto)
    public string HostId     { get; set; } = string.Empty;  // FK

    // --- Murmur fields (mirrors original VirtualServerConfig) ---
    public int    ServerId          { get; set; }
    public int    Port              { get; set; }
    public string ServerName        { get; set; } = string.Empty;
    public string ServerPassword    { get; set; } = string.Empty;
    public string DefaultChannel    { get; set; } = string.Empty;
    public string WelcomeMessage    { get; set; } = string.Empty;
    public bool   AllowHtml         { get; set; } = true;
    public bool   AllowPing         { get; set; } = true;
    public int    UserBandwidth     { get; set; } = 72000;
    public int    UserTimeout       { get; set; } = 30;
    public int    MaxMessageLength  { get; set; } = 5000;
    public bool   RememberChannel   { get; set; } = false;
    public int    Slots             { get; set; } = 10;
    public bool   IsRunning         { get; set; } = false;

    /// <summary>Convert to the service-layer DTO.</summary>
    public VirtualServerConfig ToConfig() => new()
    {
        ServerId         = ServerId,
        Port             = Port,
        ServerName       = ServerName,
        ServerPassword   = ServerPassword,
        DefaultChannel   = DefaultChannel,
        WelcomeMessage   = WelcomeMessage,
        AllowHtml        = AllowHtml,
        AllowPing        = AllowPing,
        UserBandwidth    = UserBandwidth,
        UserTimeout      = UserTimeout,
        MaxMessageLength = MaxMessageLength,
        RememberChannel  = RememberChannel,
        Slots            = Slots,
        IsRunning        = IsRunning,
    };

    public static VirtualServerCache FromConfig(string hostId, VirtualServerConfig c) => new()
    {
        HostId           = hostId,
        ServerId         = c.ServerId,
        Port             = c.Port,
        ServerName       = c.ServerName,
        ServerPassword   = c.ServerPassword,
        DefaultChannel   = c.DefaultChannel,
        WelcomeMessage   = c.WelcomeMessage,
        AllowHtml        = c.AllowHtml,
        AllowPing        = c.AllowPing,
        UserBandwidth    = c.UserBandwidth,
        UserTimeout      = c.UserTimeout,
        MaxMessageLength = c.MaxMessageLength,
        RememberChannel  = c.RememberChannel,
        Slots            = c.Slots,
        IsRunning        = c.IsRunning,
    };
}

/// <summary>Service / API DTO — same shape as original WinForms model, no EF dependency.</summary>
public class VirtualServerConfig
{
    public int    ServerId         { get; set; }
    public int    Port             { get; set; }
    public string ServerName       { get; set; } = string.Empty;
    public string ServerPassword   { get; set; } = string.Empty;
    public string DefaultChannel   { get; set; } = string.Empty;
    public string WelcomeMessage   { get; set; } = string.Empty;
    public bool   AllowHtml        { get; set; } = true;
    public bool   AllowPing        { get; set; } = true;
    public int    UserBandwidth    { get; set; } = 72000;
    public int    UserTimeout      { get; set; } = 30;
    public int    MaxMessageLength { get; set; } = 5000;
    public bool   RememberChannel  { get; set; } = false;
    public int    Slots            { get; set; } = 10;
    public bool   IsRunning        { get; set; } = false;
}

// ── Channel Template ────────────────────────────────────────────────────────

public class ChannelTemplate
{
    public string   Id              { get; set; } = Guid.NewGuid().ToString();
    public string   UserId          { get; set; } = string.Empty;   // FK → AppUser
    public string   Name            { get; set; } = string.Empty;
    public string   Description     { get; set; } = string.Empty;
    public DateTime CreatedUtc      { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedUtc     { get; set; } = DateTime.UtcNow;

    /// <summary>JSON-serialised tree — stored as a single SQLite TEXT column.</summary>
    public string RootChildrenJson { get; set; } = "[]";

    [NotMapped]
    public List<ChannelNode> RootChildren
    {
        get => JsonConvert.DeserializeObject<List<ChannelNode>>(RootChildrenJson) ?? new();
        set => RootChildrenJson = JsonConvert.SerializeObject(value);
    }
}

public class ChannelNode
{
    public string          Name        { get; set; } = string.Empty;
    public string          Description { get; set; } = string.Empty;
    public int             Position    { get; set; } = 0;
    public bool            Temporary   { get; set; } = false;
    public List<ChannelNode> Children  { get; set; } = new();
}
