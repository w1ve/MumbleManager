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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MumbleManager.Services;

namespace MumbleManager.Hubs;

[Authorize]
public class StatusHub : Hub
{
    // Group name is userId:hostId:clientKey — scoped per user per browser session
    public async Task JoinHostGroup(string hostId, string clientKey)
    {
        var userId = Context.User?.UserId()
            ?? throw new HubException("Not authenticated.");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"{userId}:{hostId}:{clientKey}");
    }

    public async Task LeaveHostGroup(string hostId, string clientKey)
    {
        var userId = Context.User?.UserId() ?? "";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{userId}:{hostId}:{clientKey}");
    }

    // Backward-compatible overloads (called without clientKey)
    public async Task JoinHostGroupLegacy(string hostId)
    {
        var userId = Context.User?.UserId()
            ?? throw new HubException("Not authenticated.");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"{userId}:{hostId}:default");
    }
}
