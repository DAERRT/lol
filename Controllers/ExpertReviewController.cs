using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using lol.Models;
using lol.Data;
using lol.Services;

namespace lol.Controllers
{
    [Authorize(Roles = "Эксперт")]
    public class ExpertReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public ExpertReviewController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: Список проектов для экспертизы
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var projects = await _context.Projects
                .Where(p => p.Status == ProjectStatus.Approved)
                .ToListAsync();

            // Получаем проекты, которые эксперт еще не оценил
            var reviewedProjectIds = await _context.ExpertReviews
                .Where(r => r.ExpertId == user.Id)
                .Select(r => r.ProjectId)
                .ToListAsync();

            var projectsToReview = projects
                .Where(p => !reviewedProjectIds.Contains(p.Id))
                .ToList();

            return View(projectsToReview);
        }

        // GET: Форма оценки проекта
        public async Task<IActionResult> Review(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.Status == ProjectStatus.Approved);

            if (project == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var existingReview = await _context.ExpertReviews
                .FirstOrDefaultAsync(r => r.ProjectId == id && r.ExpertId == user.Id);

            // Получаем все оценки по проекту с именами экспертов
            var reviews = await _context.ExpertReviews
                .Where(r => r.ProjectId == id)
                .Include(r => r.Expert)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.AllReviews = reviews;
            ViewBag.CurrentUserId = user.Id;

            return View(project);
        }

        // POST: Сохранение оценки
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int id, bool isApproved, string? comment)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.Status == ProjectStatus.Approved);

            if (project == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var existingReview = await _context.ExpertReviews
                .FirstOrDefaultAsync(r => r.ProjectId == id && r.ExpertId == user.Id);

            if (existingReview != null)
            {
                return RedirectToAction(nameof(Index));
            }

            var review = new ExpertReview
            {
                ProjectId = id,
                ExpertId = user.Id,
                IsApproved = isApproved,
                Comment = comment
            };

            _context.ExpertReviews.Add(review);
            await _context.SaveChangesAsync();

            // Проверяем количество одобрений и отказов
            var approvedReviews = await _context.ExpertReviews
                .CountAsync(r => r.ProjectId == id && r.IsApproved);
            var rejectedReviews = await _context.ExpertReviews
                .CountAsync(r => r.ProjectId == id && !r.IsApproved);

            if (approvedReviews >= 3)
            {
                project.Status = ProjectStatus.Published;
                project.PublishedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            else if (rejectedReviews >= 3)
            {
                project.Status = ProjectStatus.Editing;
                // Собираем комментарии трех экспертов
                var rejectComments = await _context.ExpertReviews
                    .Where(r => r.ProjectId == id && !r.IsApproved)
                    .OrderBy(r => r.CreatedAt)
                    .Take(3)
                    .Include(r => r.Expert)
                    .ToListAsync();
                // Формируем текст для заказчика
                string commentsText = string.Join("\n\n", rejectComments.Select(r => $"Эксперт: {r.Expert?.Email ?? "-"}\nКомментарий: {r.Comment ?? "-"}"));
                project.EditComment = $"Проект отклонён тремя экспертами. Причины:\n\n{commentsText}";
                await _context.SaveChangesAsync();
                // Уведомление заказчику
                var customer = await _userManager.FindByEmailAsync(project.Customer);
                string notify = "Ваша идея отклонена тремя экспертами и отправлена на доработку. Причины:\n" + commentsText;
                await _notificationService.CreateAsync(customer.Id, notify);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: История оценок эксперта
        public async Task<IActionResult> History()
        {
            var user = await _userManager.GetUserAsync(User);
            var reviews = await _context.ExpertReviews
                .Include(r => r.Project)
                .Where(r => r.ExpertId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reviews);
        }
    }
} 