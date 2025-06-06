using lol.Data;
using lol.Models;
using Microsoft.EntityFrameworkCore;

namespace lol.Services
{
    public class ChatService
    {
        private readonly ApplicationDbContext _context;
        public ChatService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Получить чаты пользователя
        public async Task<List<Chat>> GetUserChatsAsync(string userId)
        {
            return await _context.Chats
                .Include(c => c.ChatUsers)
                    .ThenInclude(cu => cu.User)
                .Include(c => c.Messages)
                .Where(c => c.ChatUsers.Any(u => u.UserId == userId))
                .ToListAsync();
        }

        // Получить сообщения чата
        public async Task<List<Message>> GetChatMessagesAsync(int chatId)
        {
            return await _context.Messages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.SentAt)
                .Include(m => m.User)
                .Include(m => m.Attachments)
                .ToListAsync();
        }

        // Создать личный чат (или вернуть существующий)
        public async Task<Chat> GetOrCreatePrivateChatAsync(string userId1, string userId2)
        {
            var chat = await _context.Chats
                .Include(c => c.ChatUsers)
                .FirstOrDefaultAsync(c => !c.IsGroup && !c.IsTeamChat &&
                    c.ChatUsers.Any(u => u.UserId == userId1) &&
                    c.ChatUsers.Any(u => u.UserId == userId2) &&
                    c.ChatUsers.Count == 2);
            if (chat != null) return chat;

            chat = new Chat { IsGroup = false, IsTeamChat = false };
            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();
            _context.ChatUsers.Add(new ChatUser { ChatId = chat.Id, UserId = userId1 });
            _context.ChatUsers.Add(new ChatUser { ChatId = chat.Id, UserId = userId2 });
            await _context.SaveChangesAsync();
            return chat;
        }

        // Создать групповой чат
        public async Task<Chat> CreateGroupChatAsync(string name, List<string> userIds, string creatorId)
        {
            var chat = new Chat { Name = name, IsGroup = true, IsTeamChat = false };
            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();
            
            // Добавляем создателя чата
            _context.ChatUsers.Add(new ChatUser { ChatId = chat.Id, UserId = creatorId });
            
            // Добавляем остальных участников
            foreach (var userId in userIds)
            {
                if (userId != creatorId) // Не добавляем создателя повторно
                {
                    _context.ChatUsers.Add(new ChatUser { ChatId = chat.Id, UserId = userId });
                }
            }
            await _context.SaveChangesAsync();
            return chat;
        }

        // Добавить пользователя в чат
        public async Task AddUserToChatAsync(int chatId, string userId)
        {
            if (!await _context.ChatUsers.AnyAsync(cu => cu.ChatId == chatId && cu.UserId == userId))
            {
                _context.ChatUsers.Add(new ChatUser { ChatId = chatId, UserId = userId });
                await _context.SaveChangesAsync();
            }
        }

        // Отправить сообщение
        public async Task<Message> SendMessageAsync(int chatId, string userId, string text, int? replyToMessageId = null)
        {
            var message = new Message
            {
                ChatId = chatId,
                UserId = userId,
                Text = text,
                SentAt = DateTime.UtcNow,
                ReplyToMessageId = replyToMessageId
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<ChatInfo> GetChatInfoAsync(int chatId)
        {
            var chat = await _context.Chats
                .Include(c => c.ChatUsers)
                    .ThenInclude(cu => cu.User)
                .FirstOrDefaultAsync(c => c.Id == chatId);
            if (chat == null)
                return null;
            return new ChatInfo
            {
                Id = chat.Id,
                Name = chat.Name,
                IsGroup = chat.IsGroup,
                IsTeamChat = chat.IsTeamChat,
                Participants = chat.ChatUsers.Select(cu => new ParticipantInfo
                {
                    Id = cu.User.Id,
                    FirstName = cu.User.FirstName,
                    LastName = cu.User.LastName
                }).ToList()
            };
        }

        public async Task<List<TeamInfo>> GetUserTeamsAsync(string userId)
        {
            return await _context.Teams
                .Where(t => t.Members.Any(m => m.Id == userId))
                .Select(t => new TeamInfo { Id = t.Id, Name = t.Name })
                .ToListAsync();
        }

        public async Task<Chat> CreateTeamChatAsync(int teamId, string creatorId)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null)
                throw new Exception("Команда не найдена");

            var chat = new Chat { Name = team.Name, IsGroup = true, IsTeamChat = true };
            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            foreach (var member in team.Members)
            {
                _context.ChatUsers.Add(new ChatUser { ChatId = chat.Id, UserId = member.Id });
            }
            await _context.SaveChangesAsync();
            return chat;
        }

        public async Task<bool> DeleteChatAsync(int chatId)
        {
            var chat = await _context.Chats
                .Include(c => c.ChatUsers)
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null || chat.IsTeamChat)
                return false; // Нельзя удалить командный чат или не найден

            _context.ChatUsers.RemoveRange(chat.ChatUsers);
            _context.Messages.RemoveRange(chat.Messages);
            _context.Chats.Remove(chat);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AddMessageWithAttachments(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }

        public async Task<List<MessageAttachment>> GetChatAttachmentsAsync(int chatId)
        {
            return await _context.MessageAttachments
                .Include(a => a.Message)
                .Where(a => a.Message.ChatId == chatId)
                .ToListAsync();
        }
    }
} 