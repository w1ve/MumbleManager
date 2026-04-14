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

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MumbleManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelTemplates",
                columns: table => new
                {
                    Id          = table.Column<string>(type: "TEXT", nullable: false),
                    Name        = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc  = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RootChildren = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                },
                constraints: table => table.PrimaryKey("PK_ChannelTemplates", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Hosts",
                columns: table => new
                {
                    Id          = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    Host        = table.Column<string>(type: "TEXT", nullable: false),
                    SshPort     = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 22),
                    Username    = table.Column<string>(type: "TEXT", nullable: false),
                    Password    = table.Column<string>(type: "TEXT", nullable: false),
                    IceSecret   = table.Column<string>(type: "TEXT", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_Hosts", x => x.Id));

            migrationBuilder.CreateTable(
                name: "VirtualServers",
                columns: table => new
                {
                    RowId           = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    HostId          = table.Column<string>(type: "TEXT", nullable: false),
                    ServerId        = table.Column<int>(type: "INTEGER", nullable: false),
                    Port            = table.Column<int>(type: "INTEGER", nullable: false),
                    ServerName      = table.Column<string>(type: "TEXT", nullable: false),
                    ServerPassword  = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultChannel  = table.Column<string>(type: "TEXT", nullable: false),
                    WelcomeMessage  = table.Column<string>(type: "TEXT", nullable: false),
                    AllowHtml       = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    AllowPing       = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    UserBandwidth   = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 72000),
                    UserTimeout     = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 30),
                    MaxMessageLength = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5000),
                    RememberChannel = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Slots           = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 10),
                    IsRunning       = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualServers", x => x.RowId);
                    table.ForeignKey(
                        name: "FK_VirtualServers_Hosts_HostId",
                        column: x => x.HostId,
                        principalTable: "Hosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VirtualServers_HostId",
                table: "VirtualServers",
                column: "HostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "VirtualServers");
            migrationBuilder.DropTable(name: "Hosts");
            migrationBuilder.DropTable(name: "ChannelTemplates");
        }
    }
}
