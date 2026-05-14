using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.model;

namespace server.Hubs
{
    [Authorize]
    public class MessagingHub : Hub
    {
        private readonly AppDBContext _context;

        public MessagingHub(AppDBContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            if (TryGetCurrentUserId(out var userId))
                await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));

            await base.OnConnectedAsync();
        }

        public async Task JoinConversation(Guid conversationId)
        {
            if (!TryGetCurrentUserId(out var userId))
                throw new HubException("User ID not found in token");

            var isParticipant = await _context.Set<ConversationParticipant>()
                .AnyAsync(participant =>
                    participant.ConversationId == conversationId &&
                    participant.UserId == userId &&
                    participant.IsActive);

            if (!isParticipant)
                throw new HubException("You are not part of this conversation");

            await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
        }

        public async Task LeaveConversation(Guid conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
        }

        public static string GetUserGroupName(Guid userId) => $"user:{userId}";

        public static string GetConversationGroupName(Guid conversationId) => $"conversation:{conversationId}";

        private bool TryGetCurrentUserId(out Guid userId)
        {
            userId = Guid.Empty;

            var userIdClaim =
                Context.User?.FindFirst("userId")?.Value ??
                Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                Context.User?.FindFirst("nameid")?.Value;

            return Guid.TryParse(userIdClaim, out userId);
        }
    }
}
