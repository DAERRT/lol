using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using lol.Models;
using lol.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using lol.Hubs;
using lol.Data;
using Microsoft.Extensions.Logging;

namespace lol.Controllers
{
    [Authorize]
    public class ChatController : BaseController
    {
        private readonly ChatService _chatService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly NotificationService _notificationService;
        private readonly ILogger<ChatController> _logger;

        private readonly ApplicationDbContext _context;

        public ChatController(ChatService chatService, UserManager<ApplicationUser> userManager, IHubContext<ChatHub> hubContext, NotificationService notificationService, ApplicationDbContext context, ILogger<ChatController> logger) : base(context)
        {
            _chatService = chatService;
            _userManager = userManager;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _context = context;
            _logger = logger;
        }

        // Список чатов пользователя
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var chats = await _chatService.GetUserChatsAsync(user.Id);
            ViewBag.UserId = user.Id;
            ViewBag.ActiveChatId = null;
            return View(chats);
        }

        // История сообщений чата
        public async Task<IActionResult> Messages(int chatId)
        {
            var user = await _userManager.GetUserAsync(User);
            var messages = await _chatService.GetChatMessagesAsync(chatId);
            var chats = await _chatService.GetUserChatsAsync(user.Id);
            var chat = chats.FirstOrDefault(c => c.Id == chatId);
            ViewBag.ChatId = chatId;
            ViewBag.UserId = user.Id;
            ViewBag.Chats = chats;
            ViewBag.ActiveChatId = chatId;
            if (chat != null && !chat.IsGroup && !chat.IsTeamChat)
            {
                var interlocutor = chat.ChatUsers?.FirstOrDefault(u => u.UserId != user.Id)?.User;
                ViewBag.Interlocutor = interlocutor;
            }
            else
            {
                ViewBag.Interlocutor = null;
                ViewBag.GroupAvatarPath = chat?.AvatarPath ?? "/images/avatars/group.png";
            }
            return View(messages);
        }

        // Создать личный чат с пользователем
        [HttpPost]
        public async Task<IActionResult> CreatePrivate(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    string errorMessage = $"[{DateTime.Now}] Error: userId is null or empty when attempting to create private chat\n";
                    try
                    {
                        string logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                        if (!Directory.Exists(logDir))
                        {
                            Directory.CreateDirectory(logDir);
                            Console.WriteLine($"Created log directory at: {logDir}");
                        }
                        string logPath = Path.Combine(logDir, "app.log");
                        System.IO.File.AppendAllText(logPath, errorMessage);
                        Console.WriteLine($"Successfully wrote to log file at: {logPath}");
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogError(logEx, "Error writing to log file at {Path}", Path.Combine(Directory.GetCurrentDirectory(), "logs", "app.log"));
                        Console.WriteLine($"Error writing to log file: {logEx.Message}. Attempted path: {Path.Combine(Directory.GetCurrentDirectory(), "logs", "app.log")}");
                    }
                    return BadRequest($"Ошибка: ID пользователя не указан");
                }
                
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    string errorMessage = $"[{DateTime.Now}] Error: Current user could not be retrieved\n";
                    try
                    {
                        string logDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs");
                        Directory.CreateDirectory(logDir);
                        string logPath = Path.Combine(logDir, "app.log");
                        System.IO.File.AppendAllText(logPath, errorMessage);
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogError(logEx, "Error writing to log file");
                        Console.WriteLine($"Error writing to log file: {logEx.Message}");
                    }
                    return BadRequest($"Ошибка: Не удалось получить данные текущего пользователя");
                }
                
                _logger.LogInformation($"Creating private chat for current user {currentUser.Id} with user {userId}");
                try
                {
                    string logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                        Console.WriteLine($"Created log directory at: {logDir}");
                    }
                    string logPath = Path.Combine(logDir, "app.log");
                    string logMessage = $"[{DateTime.Now}] Creating private chat for current user {currentUser.Id} with user {userId}\n";
                    System.IO.File.AppendAllText(logPath, logMessage);
                    Console.WriteLine($"Successfully wrote to log file at: {logPath}");
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Error writing to log file at {Path}", Path.Combine(Directory.GetCurrentDirectory(), "logs", "app.log"));
                    Console.WriteLine($"Error writing to log file: {logEx.Message}. Attempted path: {Path.Combine(Directory.GetCurrentDirectory(), "logs", "app.log")}");
                }
                
                var chat = await _chatService.GetOrCreatePrivateChatAsync(currentUser.Id, userId);
                _logger.LogInformation("Chat created with ID: {ChatId} for users {CurrentUserId} and {UserId}", chat.Id, currentUser.Id, userId);
                _logger.LogInformation("Redirecting to Messages with chatId: {ChatId}", chat.Id);
                try
                {
                    string logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                        Console.WriteLine($"Created log directory at: {logDir}");
                    }
                    string logPath = Path.Combine(logDir, "app.log");
                    string logMessage = $"[{DateTime.Now}] Chat created with ID: {chat.Id} for users {currentUser.Id} and {userId}\n";
                    System.IO.File.AppendAllText(logPath, logMessage);
                    Console.WriteLine($"Successfully wrote to log file at: {logPath}");
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Error writing to log file at {Path}", Path.Combine(Directory.GetCurrentDirectory(), "logs", "app.log"));
                    Console.WriteLine($"Error writing to log file: {logEx.Message}. Attempted path: {Path.Combine(Directory.GetCurrentDirectory(), "logs", "app.log")}");
                }
                if (chat.Id <= 0)
                {
                    string errorMessage = $"[{DateTime.Now}] Warning: Invalid chat ID {chat.Id} after creation for users {currentUser.Id} and {userId}\n";
                    try
                    {
                        string logDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs");
                        Directory.CreateDirectory(logDir);
                        string logPath = Path.Combine(logDir, "app.log");
                        System.IO.File.AppendAllText(logPath, errorMessage);
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogError(logEx, "Error writing to log file");
                        Console.WriteLine($"Error writing to log file: {logEx.Message}");
                    }
                    return BadRequest($"Ошибка: Созданный чат имеет некорректный ID");
                }
                return RedirectToAction("Messages", new { chatId = chat.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating private chat for user {UserId}", userId);
                Console.WriteLine($"Error creating private chat: {ex.Message}");
                try
                {
                    string logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                        Console.WriteLine($"Created log directory at: {logDir}");
                    }
                    string logPath = Path.Combine(logDir, "app.log");
                    string errorMessage = $"[{DateTime.Now}] Error creating private chat for user {userId}: {ex.Message}\n";
                    System.IO.File.AppendAllText(logPath, errorMessage);
                    Console.WriteLine($"Successfully wrote error to log file at: {logPath}");
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Error writing to log file at {Path}", Path.Combine(Directory.GetCurrentDirectory(), "logs", "app.log"));
                    Console.WriteLine($"Error writing to log file: {logEx.Message}. Attempted path: {Path.Combine(Directory.GetCurrentDirectory(), "logs", "app.log")}");
                }
                return BadRequest($"Ошибка при создании чата: {ex.Message}");
            }
        }

        // Создать групповой чат
        [HttpPost]
        public async Task<IActionResult> CreateGroup(string name, List<string> userIds)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var chat = await _chatService.CreateGroupChatAsync(name, userIds, currentUser.Id);
            return RedirectToAction("Messages", new { chatId = chat.Id });
        }

        // Отправить сообщение
        [HttpPost]
        public async Task<IActionResult> SendMessage(int chatId, string text, int? replyToMessageId)
        {
            var user = await _userManager.GetUserAsync(User);
            await _chatService.SendMessageAsync(chatId, user.Id, text, replyToMessageId);
            // Обновляем badge у всех участников чата
            await _hubContext.Clients.All.SendAsync("NotifyUnreadCountChanged", chatId);
            return Ok();
        }

        // Отправить сообщение с файлами (AJAX)
        [HttpPost]
        public async Task<IActionResult> SendMessageWithFiles(int chatId, string text, int? replyToMessageId, List<IFormFile> files)
        {
            var user = await _userManager.GetUserAsync(User);
            var savedFiles = new List<object>();
            var attachments = new List<MessageAttachment>();
            // Проверка размера и сохранение файлов
            foreach (var file in files ?? new List<IFormFile>())
            {
                if (file.Length > 200 * 1024 * 1024)
                    return BadRequest($"Файл '{file.FileName}' превышает 200 МБ.");
                var uploadsDir = Path.Combine("wwwroot", "uploads", chatId.ToString());
                Directory.CreateDirectory(uploadsDir);
                var fileName = Path.GetRandomFileName() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                var relPath = $"/uploads/{chatId}/{fileName}";
                attachments.Add(new MessageAttachment
                {
                    FileName = file.FileName,
                    FilePath = relPath,
                    FileSize = file.Length,
                    ContentType = file.ContentType
                });
                savedFiles.Add(new { Name = file.FileName, Path = relPath, Size = file.Length });
            }
            // Сохраняем сообщение и вложения
            var message = new Message
            {
                ChatId = chatId,
                UserId = user.Id,
                Text = string.IsNullOrWhiteSpace(text) ? "" : text,
                SentAt = DateTime.UtcNow,
                ReplyToMessageId = replyToMessageId,
                Attachments = attachments
            };
            await _chatService.AddMessageWithAttachments(message);
            return Json(new { success = true, files = savedFiles });
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

        [HttpGet]
        public async Task<IActionResult> GetChatAttachments(int chatId)
        {
            var attachments = await _chatService.GetChatAttachmentsAsync(chatId);
            var result = attachments.Select(a => new
            {
                a.Id,
                a.FileName,
                a.FilePath,
                a.FileSize,
                a.ContentType,
                a.MessageId,
                SentAt = a.Message.SentAt
            });
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> UploadGroupAvatar(int chatId, IFormFile avatar)
        {
            if (avatar == null || avatar.Length == 0)
                return Json(new { success = false, error = "Файл не выбран" });

            var chat = await _chatService.GetChatInfoByIdAsync(chatId);
            if (chat == null)
                return Json(new { success = false, error = "Чат не найден" });

            // Проверка типа файла (только изображения)
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(avatar.ContentType))
                return Json(new { success = false, error = "Разрешены только изображения" });

            // Сохраняем файл
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/avatars");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
            var ext = Path.GetExtension(avatar.FileName);
            var fileName = $"group_{chatId}_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
            var filePath = Path.Combine(uploads, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }

            // Обновляем путь в БД
            await _chatService.UpdateChatAvatarAsync(chatId, $"/images/avatars/{fileName}");

            return Json(new { success = true, avatarPath = $"/images/avatars/{fileName}" });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int messageId)
        {
            var user = await _userManager.GetUserAsync(User);
            await _chatService.MarkMessageAsRead(messageId, user.Id);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount(int chatId)
        {
            var user = await _userManager.GetUserAsync(User);
            var count = await _chatService.GetUnreadCountForUser(chatId, user.Id);
            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> IsMessageReadByAll(int messageId)
        {
            var isRead = await _chatService.IsMessageReadByAll(messageId);
            return Json(new { isRead });
        }

        [HttpPost]
        public async Task<IActionResult> MarkMultipleAsRead([FromBody] int[] messageIds)
        {
            var user = await _userManager.GetUserAsync(User);
            await _chatService.MarkMessagesAsRead(messageIds, user.Id);
            // Обновляем badge у всех участников чата (берём chatId первого сообщения)
            if (messageIds.Length > 0)
            {
                var firstMsg = await _chatService.GetChatMessagesAsync(messageIds[0]);
                if (firstMsg != null && firstMsg.Count > 0)
                {
                    int chatId = firstMsg[0].ChatId;
                    await _hubContext.Clients.All.SendAsync("NotifyUnreadCountChanged", chatId);
                }
            }
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetMessagesReadStatus(string messageIds)
        {
            var ids = messageIds.Split(',').Select(int.Parse).ToArray();
            var statuses = await _chatService.GetMessagesReadStatus(ids);
            return Json(statuses);
        }

        [HttpPost]
        public async Task<IActionResult> RenameGroup(int chatId, string newName)
        {
            await _chatService.RenameGroupChatAsync(chatId, newName);
            return Json(new { success = true });
        }
    }
}
