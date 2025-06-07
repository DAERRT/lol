using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace lol.Hubs
{
    public class ChatHub : Hub
    {
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
    }
} 