using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using lol.Models;
using lol.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using lol.Hubs;

namespace lol.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ChatService _chatService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly NotificationService _notificationService;

        public ChatController(ChatService chatService, UserManager<ApplicationUser> userManager, IHubContext<ChatHub> hubContext, NotificationService notificationService)
        {
            _chatService = chatService;
            _userManager = userManager;
            _hubContext = hubContext;
            _notificationService = notificationService;
        }

        // Список чатов пользователя
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var chats = await _chatService.GetUserChatsAsync(user.Id);
            ViewBag.UserId = user.Id;
            return View(chats);
        }

        // История сообщений чата
        public async Task<IActionResult> Messages(int chatId)
        {
            var user = await _userManager.GetUserAsync(User);
            var messages = await _chatService.GetChatMessagesAsync(chatId);
            ViewBag.ChatId = chatId;
            ViewBag.UserId = user.Id;
            return View(messages);
        }

        // Создать личный чат с пользователем
        [HttpPost]
        public async Task<IActionResult> CreatePrivate(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var chat = await _chatService.GetOrCreatePrivateChatAsync(currentUser.Id, userId);
            return RedirectToAction("Messages", new { chatId = chat.Id });
        }

        // Создать групповой чат
        [HttpPost]
        public async Task<IActionResult> CreateGroup(string name, List<string> userIds)
        {
            var chat = await _chatService.CreateGroupChatAsync(name, userIds);
            return RedirectToAction("Messages", new { chatId = chat.Id });
        }

        // Отправить сообщение
        [HttpPost]
        public async Task<IActionResult> SendMessage(int chatId, string text)
        {
            var user = await _userManager.GetUserAsync(User);
            await _chatService.SendMessageAsync(chatId, user.Id, text);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new List<object>());

            var users = await _userManager.Users
                .Where(u => u.UserName.Contains(query) || u.FirstName.Contains(query) || u.LastName.Contains(query))
                .Select(u => new { u.Id, u.UserName, u.FirstName, u.LastName })
                .Take(10)
                .ToListAsync();

            return Json(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetChatInfo(int chatId)
        {
            var chat = await _chatService.GetChatInfoAsync(chatId);
            if (chat == null)
                return NotFound();
            return Json(chat);
        }

        [HttpPost]
        public async Task<IActionResult> InviteUser(int chatId, string userId)
        {
            await _chatService.AddUserToChatAsync(chatId, userId);
            var chat = await _chatService.GetChatInfoAsync(chatId);
            await _hubContext.Clients.User(userId).SendAsync("UserInvited", userId, chatId, chat.Name);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetUserTeams()
        {
            var user = await _userManager.GetUserAsync(User);
            var teams = await _chatService.GetUserTeamsAsync(user.Id);
            return Json(teams);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTeamChat(int teamId)
        {
            var user = await _userManager.GetUserAsync(User);
            var chat = await _chatService.CreateTeamChatAsync(teamId, user.Id);
            return RedirectToAction("Messages", new { chatId = chat.Id });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int chatId)
        {
            var result = await _chatService.DeleteChatAsync(chatId);
            if (!result)
                return Forbid(); // или NotFound()
            return RedirectToAction("Index");
        }
    }
} 