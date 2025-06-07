using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using lol.Services;
using System.Linq;

namespace lol.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;
        private readonly IServiceProvider _serviceProvider;
        public ChatHub(ChatService chatService, IServiceProvider serviceProvider)
        {
            _chatService = chatService;
            _serviceProvider = serviceProvider;
        }
        // Отправка сообщения в чат
        public async Task SendMessageToChat(int chatId, string userId, string message)
        {
            await Clients.Group($"chat_{chatId}").SendAsync("ReceiveMessage", chatId, userId, message);
        }

        // Присоединение к чату (группе)
        public async Task JoinChat(int chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}"); 
        }

        // Покинуть чат (группу)
        public async Task LeaveChat(int chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{chatId}");
        }

        // Уведомление о прочтении сообщения
        public async Task NotifyMessageRead(int chatId, int messageId, string userId)
        {
            await Clients.Group($"chat_{chatId}").SendAsync("MessageRead", chatId, messageId, userId);
        }

        // Уведомление о непрочитанных сообщениях (badge)
        public async Task NotifyUnreadCountChanged(int chatId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = (lol.Data.ApplicationDbContext)scope.ServiceProvider.GetService(typeof(lol.Data.ApplicationDbContext));
                var chatUsers = context.ChatUsers.Where(cu => cu.ChatId == chatId).Select(cu => cu.UserId).ToList();
                foreach (var userId in chatUsers)
                {
                    var count = await _chatService.GetUnreadCountForUser(chatId, userId);
                    await Clients.User(userId).SendAsync("UpdateUnreadCount", chatId, count);
                }
            }
        }
    }
} 