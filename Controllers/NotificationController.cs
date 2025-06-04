using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using lol.Services;
using Microsoft.AspNetCore.Identity;
using lol.Models;

namespace lol.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly NotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(NotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        // Получить все уведомления текущего пользователя
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var notifications = await _notificationService.GetUserNotificationsAsync(user.Id);
            return View(notifications);
        }

        // Получить уведомления в формате JSON (для AJAX)
        [HttpGet]
        public async Task<IActionResult> GetUserNotifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var notifications = await _notificationService.GetUserNotificationsAsync(user.Id);
            return Json(notifications);
        }

        // Пометить уведомление как прочитанное
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // Очистить все уведомления пользователя
        [HttpPost]
        public async Task<IActionResult> ClearAll()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            await _notificationService.DeleteAllUserNotificationsAsync(user.Id);
            return RedirectToAction(nameof(Index));
        }

        // Удалить одно уведомление
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            await _notificationService.DeleteNotificationAsync(id, user.Id);
            return RedirectToAction(nameof(Index));
        }
    }
}
