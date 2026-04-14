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

using Microsoft.EntityFrameworkCore;
using MumbleManager.Models;

namespace MumbleManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser>            Users            { get; set; }
    public DbSet<SshHostEntry>       Hosts            { get; set; }
    public DbSet<VirtualServerCache> VirtualServers   { get; set; }
    public DbSet<ChannelTemplate>    ChannelTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder b)
    {
        // AppUser
        b.Entity<AppUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasMany(x => x.Hosts)
             .WithOne()
             .HasForeignKey(h => h.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Templates)
             .WithOne()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // SshHostEntry
        b.Entity<SshHostEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasMany(x => x.CachedServers)
             .WithOne()
             .HasForeignKey(s => s.HostId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // VirtualServerCache
        b.Entity<VirtualServerCache>(e =>
        {
            e.HasKey(x => x.RowId);
            e.Property(x => x.RowId).ValueGeneratedOnAdd();
        });

        // ChannelTemplate — store RootChildren as JSON blob
        b.Entity<ChannelTemplate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.RootChildrenJson)
             .HasColumnName("RootChildren");
        });
    }
}
