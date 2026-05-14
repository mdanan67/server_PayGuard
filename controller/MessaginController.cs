using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using server.Data;
using server.Hubs;
using server.model;

namespace server.controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagingController : ControllerBase
    {
        private readonly AppDBContext _context;
        private readonly IHubContext<MessagingHub> _messagingHub;

        public MessagingController(AppDBContext context, IHubContext<MessagingHub> messagingHub)
        {
            _context = context;
            _messagingHub = messagingHub;
        }

        [HttpGet("contacts")]
        public async Task<ActionResult> GetContacts()
        {
            if (!TryGetCurrentUserId(out var currentUserId, out var authError))
                return authError!;

            var currentUser = await _context.Users
                .FirstOrDefaultAsync(user => user.Id == currentUserId);

            if (currentUser == null)
                return Unauthorized(new { message = "User not found" });

            if (string.Equals(currentUser.Role, "parent", StringComparison.OrdinalIgnoreCase))
            {
                var children = await _context.FamilyMembers
                    .Where(member => member.ParentId == currentUserId)
                    .Include(member => member.Child)
                    .Select(member => new
                    {
                        id = member.Child.Id,
                        name = member.Child.FirstName + " " + member.Child.LastName,
                        email = member.Child.Email,
                        role = member.Child.Role,
                        profileImage = member.Child.Profile_image
                    })
                    .ToListAsync();

                return Ok(new { contacts = children });
            }

            var parents = await _context.FamilyMembers
                .Where(member => member.ChildId == currentUserId)
                .Include(member => member.Parent)
                .Select(member => new
                {
                    id = member.Parent.Id,
                    name = member.Parent.FirstName + " " + member.Parent.LastName,
                    email = member.Parent.Email,
                    role = member.Parent.Role,
                    profileImage = member.Parent.Profile_image
                })
                .ToListAsync();

            return Ok(new { contacts = parents });
        }

        [HttpGet("conversations")]
        public async Task<ActionResult> GetConversations()
        {
            if (!TryGetCurrentUserId(out var currentUserId, out var authError))
                return authError!;

            var conversations = await _context.Set<Conversation>()
                .Where(conversation => conversation.Participants.Any(participant =>
                    participant.UserId == currentUserId &&
                    participant.IsActive))
                .Include(conversation => conversation.Participants)
                    .ThenInclude(participant => participant.User)
                .Include(conversation => conversation.Messages)
                    .ThenInclude(message => message.SenderUser)
                .ToListAsync();

            var result = conversations
                .Select(conversation =>
                {
                    var otherParticipant = conversation.Participants
                        .FirstOrDefault(participant => participant.UserId != currentUserId);

                    var lastMessage = conversation.Messages
                        .Where(message => !message.IsDeleted)
                        .OrderByDescending(message => message.SentAt)
                        .FirstOrDefault();

                    return new
                    {
                        id = conversation.Id,
                        type = conversation.Type,
                        name = conversation.Name,
                        createdAt = conversation.CreatedAt,
                        otherUser = otherParticipant?.User == null ? null : new
                        {
                            id = otherParticipant.User.Id,
                            name = otherParticipant.User.FirstName + " " + otherParticipant.User.LastName,
                            email = otherParticipant.User.Email,
                            role = otherParticipant.User.Role,
                            profileImage = otherParticipant.User.Profile_image
                        },
                        lastMessage = lastMessage == null ? null : new
                        {
                            id = lastMessage.Id,
                            body = lastMessage.Body,
                            senderUserId = lastMessage.SenderUserId,
                            senderName = lastMessage.SenderUser == null
                                ? null
                                : lastMessage.SenderUser.FirstName + " " + lastMessage.SenderUser.LastName,
                            sentAt = lastMessage.SentAt
                        }
                    };
                })
                .OrderByDescending(conversation => conversation.lastMessage?.sentAt ?? conversation.createdAt)
                .ToList();

            return Ok(new { conversations = result });
        }

        [HttpGet("conversations/{conversationId}/messages")]
        public async Task<ActionResult> GetMessages(Guid conversationId)
        {
            if (!TryGetCurrentUserId(out var currentUserId, out var authError))
                return authError!;

            var isParticipant = await _context.Set<ConversationParticipant>()
                .AnyAsync(participant =>
                    participant.ConversationId == conversationId &&
                    participant.UserId == currentUserId &&
                    participant.IsActive);

            if (!isParticipant)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not part of this conversation" });

            var messages = await _context.Set<Message>()
                .Where(message =>
                    message.ConversationId == conversationId &&
                    !message.IsDeleted)
                .Include(message => message.SenderUser)
                .OrderBy(message => message.SentAt)
                .Select(message => new
                {
                    id = message.Id,
                    conversationId = message.ConversationId,
                    senderUserId = message.SenderUserId,
                    senderName = message.SenderUser == null
                        ? null
                        : message.SenderUser.FirstName + " " + message.SenderUser.LastName,
                    body = message.Body,
                    sentAt = message.SentAt
                })
                .ToListAsync();

            return Ok(new { messages });
        }

        [HttpPost("send")]
        public async Task<ActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            if (request == null || request.ReceiverUserId == Guid.Empty)
                return BadRequest(new { message = "Receiver user is required" });

            if (string.IsNullOrWhiteSpace(request.Body))
                return BadRequest(new { message = "Message body is required" });

            if (request.Body.Length > 2000)
                return BadRequest(new { message = "Message cannot be longer than 2000 characters" });

            if (!TryGetCurrentUserId(out var senderUserId, out var authError))
                return authError!;

            if (senderUserId == request.ReceiverUserId)
                return BadRequest(new { message = "You cannot send a message to yourself" });

            var canMessage = await CanMessageAsync(senderUserId, request.ReceiverUserId);

            if (!canMessage)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only message your linked parent or child" });

            var conversation = await GetOrCreateDirectConversationAsync(senderUserId, request.ReceiverUserId);

            var message = new Message
            {
                ConversationId = conversation.Id,
                SenderUserId = senderUserId,
                Body = request.Body.Trim(),
                SentAt = DateTime.UtcNow
            };

            await _context.Set<Message>().AddAsync(message);
            await _context.SaveChangesAsync();

            var sender = await _context.Users
                .Where(user => user.Id == senderUserId)
                .Select(user => new
                {
                    name = user.FirstName + " " + user.LastName
                })
                .FirstOrDefaultAsync();

            var messageData = new
            {
                id = message.Id,
                conversationId = message.ConversationId,
                senderUserId = message.SenderUserId,
                senderName = sender?.name,
                body = message.Body,
                sentAt = message.SentAt
            };

            await _messagingHub.Clients
                .Groups(
                    MessagingHub.GetUserGroupName(senderUserId),
                    MessagingHub.GetUserGroupName(request.ReceiverUserId))
                .SendAsync("ReceiveMessage", messageData);

            return Ok(new
            {
                message = "Message sent successfully",
                data = messageData
            });
        }

        private async Task<bool> CanMessageAsync(Guid senderUserId, Guid receiverUserId)
        {
            var sender = await _context.Users
                .Where(user => user.Id == senderUserId)
                .Select(user => new { user.Id, user.Role })
                .FirstOrDefaultAsync();

            if (sender == null)
                return false;

            if (string.Equals(sender.Role, "parent", StringComparison.OrdinalIgnoreCase))
            {
                return await _context.FamilyMembers.AnyAsync(member =>
                    member.ParentId == senderUserId &&
                    member.ChildId == receiverUserId);
            }

            return await _context.FamilyMembers.AnyAsync(member =>
                member.ChildId == senderUserId &&
                member.ParentId == receiverUserId);
        }

        private async Task<Conversation> GetOrCreateDirectConversationAsync(Guid senderUserId, Guid receiverUserId)
        {
            var conversation = await _context.Set<Conversation>()
                .Include(existingConversation => existingConversation.Participants)
                .FirstOrDefaultAsync(existingConversation =>
                    existingConversation.Type == "direct" &&
                    existingConversation.Participants.Any(participant => participant.UserId == senderUserId) &&
                    existingConversation.Participants.Any(participant => participant.UserId == receiverUserId));

            if (conversation != null)
                return conversation;

            conversation = new Conversation
            {
                Type = "direct",
                CreatedByUserId = senderUserId,
                CreatedAt = DateTime.UtcNow,
                Participants = new List<ConversationParticipant>
                {
                    new ConversationParticipant
                    {
                        UserId = senderUserId,
                        JoinedAt = DateTime.UtcNow
                    },
                    new ConversationParticipant
                    {
                        UserId = receiverUserId,
                        JoinedAt = DateTime.UtcNow
                    }
                }
            };

            await _context.Set<Conversation>().AddAsync(conversation);
            await _context.SaveChangesAsync();

            return conversation;
        }

        private bool TryGetCurrentUserId(out Guid userId, out ActionResult? error)
        {
            userId = Guid.Empty;
            error = null;

            var userIdClaim =
                User.FindFirst("userId")?.Value ??
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("nameid")?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                error = Unauthorized(new { message = "User ID not found in token" });
                return false;
            }

            if (!Guid.TryParse(userIdClaim, out userId))
            {
                error = BadRequest(new { message = "User ID in token is not a valid GUID", claimValue = userIdClaim });
                return false;
            }

            return true;
        }
    }

    public class SendMessageRequest
    {
        public Guid ReceiverUserId { get; set; }
        public string Body { get; set; } = string.Empty;
    }
}
