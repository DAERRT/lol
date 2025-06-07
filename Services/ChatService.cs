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

        public async Task UpdateChatAvatarAsync(int chatId, string avatarPath)
        {
            var chat = await _context.Chats.FirstOrDefaultAsync(c => c.Id == chatId);
            if (chat != null)
            {
                chat.AvatarPath = avatarPath;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Chat> GetChatInfoByIdAsync(int chatId)
        {
            return await _context.Chats.FirstOrDefaultAsync(c => c.Id == chatId);
        }

        // Отметить сообщение как прочитанное
        public async Task MarkMessageAsRead(int messageId, string userId)
        {
            var alreadyRead = await _context.MessageReads.AnyAsync(r => r.MessageId == messageId && r.UserId == userId);
            if (!alreadyRead)
            {
                _context.MessageReads.Add(new MessageRead { MessageId = messageId, UserId = userId, ReadAt = DateTime.UtcNow });
                await _context.SaveChangesAsync();
            }
        }

        // Получить количество непрочитанных сообщений в чате для пользователя
        public async Task<int> GetUnreadCountForUser(int chatId, string userId)
        {
            return await _context.Messages
                .Where(m => m.ChatId == chatId && m.UserId != userId)
                .CountAsync(m => !_context.MessageReads.Any(r => r.MessageId == m.Id && r.UserId == userId));
        }

        // Прочитано ли сообщение всеми кроме отправителя
        public async Task<bool> IsMessageReadByAll(int messageId)
        {
            var message = await _context.Messages.Include(m => m.Chat).FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null) return false;
            var chatUsers = await _context.ChatUsers.Where(cu => cu.ChatId == message.ChatId && cu.UserId != message.UserId).Select(cu => cu.UserId).ToListAsync();
            var readUsers = await _context.MessageReads.Where(r => r.MessageId == messageId).Select(r => r.UserId).ToListAsync();
            return chatUsers.All(u => readUsers.Contains(u));
        }

        // Отметить несколько сообщений как прочитанные
        public async Task MarkMessagesAsRead(int[] messageIds, string userId)
        {
            try
            {
                // Получаем существующие записи о прочтении
                var existingReads = await _context.MessageReads
                    .Where(r => messageIds.Contains(r.MessageId) && r.UserId == userId)
                    .Select(r => r.MessageId)
                    .ToListAsync();

                // Создаем новые записи только для непрочитанных сообщений
                var newReads = messageIds
                    .Where(id => !existingReads.Contains(id))
                    .Select(id => new MessageRead 
                    { 
                        MessageId = id, 
                        UserId = userId, 
                        ReadAt = DateTime.UtcNow 
                    });

                if (newReads.Any())
                {
                    await _context.MessageReads.AddRangeAsync(newReads);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Отмечено как прочитанное {newReads.Count()} сообщений пользователем {userId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отметке сообщений как прочитанных: {ex.Message}");
                throw;
            }
        }

        // Получить статусы прочтения для нескольких сообщений
        public async Task<List<MessageReadStatus>> GetMessagesReadStatus(int[] messageIds)
        {
            try
            {
                // Получаем все сообщения одним запросом
                var messages = await _context.Messages
                    .Include(m => m.Chat)
                    .Where(m => messageIds.Contains(m.Id))
                    .ToListAsync();

                if (!messages.Any())
                {
                    Console.WriteLine("Сообщения не найдены");
                    return new List<MessageReadStatus>();
                }

                // Получаем все чаты одним запросом
                var chatIds = messages.Select(m => m.ChatId).Distinct().ToList();
                var chatUsers = await _context.ChatUsers
                    .Where(cu => chatIds.Contains(cu.ChatId))
                    .GroupBy(cu => cu.ChatId)
                    .ToDictionaryAsync(g => g.Key, g => g.Select(cu => cu.UserId).ToList());

                // Получаем все записи о прочтении одним запросом
                var readUsers = await _context.MessageReads
                    .Where(r => messageIds.Contains(r.MessageId))
                    .GroupBy(r => r.MessageId)
                    .ToDictionaryAsync(g => g.Key, g => g.Select(r => r.UserId).ToList());

                var result = new List<MessageReadStatus>();
                foreach (var message in messages)
                {
                    var usersInChat = chatUsers.GetValueOrDefault(message.ChatId, new List<string>())
                        .Where(u => u != message.UserId)
                        .ToList();

                    var usersWhoRead = readUsers.GetValueOrDefault(message.Id, new List<string>());

                    var isRead = usersInChat.Any() && usersInChat.All(u => usersWhoRead.Contains(u));
                    
                    Console.WriteLine($"Сообщение {message.Id}: пользователей в чате {usersInChat.Count}, прочитали {usersWhoRead.Count}, статус прочтения: {isRead}");

                    result.Add(new MessageReadStatus
                    {
                        MessageId = message.Id,
                        IsRead = isRead
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении статусов прочтения: {ex.Message}");
                throw;
            }
        }
    }
} 