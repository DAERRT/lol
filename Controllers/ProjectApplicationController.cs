using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using lol.Data;
using lol.Models;
using lol.Services;

namespace lol.Controllers
{
    [Authorize]
    public class ProjectApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public ProjectApplicationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // Просмотр заявок заказчиком на свои проекты
        [Authorize(Roles = "Заказчик")]
        public async Task<IActionResult> Moderation()
        {
            var user = await _userManager.GetUserAsync(User);
            var applications = await _context.ProjectApplications
                .Include(a => a.Project)
                .Include(a => a.Team)
                .Where(a => a.Project.Customer == user.Email)
                .ToListAsync();
            return View(applications);
        }

        // GET: Подача заявки (форма)
        [Authorize(Roles = "Тимлид, Администратор")]
        public async Task<IActionResult> Create(int projectId)
        {
            var user = await _userManager.GetUserAsync(User);
            // Получаем все команды, где пользователь — тимлид
            var teams = await _context.Teams
                .Where(t => t.LeaderId == user.Id)
                .ToListAsync();

            // Исключаем те команды, которые уже подали заявку на этот проект
            var alreadyAppliedTeamIds = await _context.ProjectApplications
                .Where(a => a.ProjectId == projectId)
                .Select(a => a.TeamId)
                .ToListAsync();

            var availableTeams = teams.Where(t => !alreadyAppliedTeamIds.Contains(t.Id)).ToList();

            if (!availableTeams.Any())
            {
                // Все команды уже подали заявку
                return RedirectToAction("AlreadyApplied");
            }

            ViewBag.Teams = new SelectList(availableTeams, "Id", "Name");
            ViewBag.ProjectId = projectId;
            return View();
        }

        // POST: Подача заявки
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Тимлид, Администратор")]
        public async Task<IActionResult> Create(int projectId, int teamId, string? message)
        {
            var user = await _userManager.GetUserAsync(User);
            // Проверяем, что пользователь действительно тимлид этой команды
            var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == teamId && t.LeaderId == user.Id);
            if (team == null)
            {
                return Forbid();
            }
            bool alreadyApplied = await _context.ProjectApplications.AnyAsync(a => a.ProjectId == projectId && a.TeamId == team.Id);
            if (alreadyApplied)
            {
                return RedirectToAction("AlreadyApplied");
            }
            var application = new ProjectApplication
            {
                ProjectId = projectId,
                TeamId = team.Id,
                Message = message,
                Status = ProjectApplicationStatus.Pending,
                CreatedAt = DateTime.Now
            };
            _context.ProjectApplications.Add(application);
            await _context.SaveChangesAsync();
            // Отправляем уведомление создателю проекта
            var project = await _context.Projects.FindAsync(projectId);
            if (project != null)
            {
                var creator = await _userManager.FindByEmailAsync(project.Customer);
                if (creator != null)
                {
                    string notifyMessage = $"Пользователь {user.FirstName} {user.LastName} подал(а) заявку на участие в проекте \"{project.IdeaName}\" от команды \"{team.Name}\".";
                    await _notificationService.CreateAsync(creator.Id, notifyMessage);
                }
            }
            return RedirectToAction("MyApplications");
        }

        // GET: Мои заявки (тимлид)
        [Authorize(Roles = "Тимлид, Администратор")]
        public async Task<IActionResult> MyApplications()
        {
            var user = await _userManager.GetUserAsync(User);
            var team = await _context.Teams.FirstOrDefaultAsync(t => t.LeaderId == user.Id);
            if (team == null)
            {
                return Forbid();
            }
            var applications = await _context.ProjectApplications
                .Include(a => a.Project)
                .Where(a => a.TeamId == team.Id)
                .ToListAsync();
            return View(applications);
        }

        // GET: Уже подана заявка (тимлид)
        [Authorize(Roles = "Тимлид, Администратор")]
        public IActionResult AlreadyApplied()
        {
            return View();
        }

        // POST: Модерация заявки (одобрить/отклонить)
        [HttpPost]
        [Authorize(Roles = "Заказчик")]
        public async Task<IActionResult> Moderate(int id, string action)
        {
            var application = await _context.ProjectApplications
                .Include(a => a.Project)
                    .ThenInclude(p => p.ExecutorTeams)
                .Include(a => a.Team)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (application == null)
                return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (application.Project.Customer != user.Email)
                return Forbid();
            if (action == "approve")
            {
                application.Status = ProjectApplicationStatus.Approved;
                // Добавляем команду в исполнители проекта
                var project = application.Project;
                var team = application.Team;
                if (project != null && team != null && !project.ExecutorTeams.Contains(team))
                {
                    project.ExecutorTeams.Add(team);
                }
                // Меняем статус команды на 'Работает над проектом'
                if (team != null)
                {
                    team.Status = TeamStatus.WorkingOnProject;
                }
                // Отправляем уведомление всем участникам команды
                string notifyMessage = $"Ваша заявка на участие в проекте \"{project.IdeaName}\" от команды \"{team.Name}\" одобрена!";
                await _notificationService.CreateAsync(team.CreatorId, notifyMessage);
                await _notificationService.CreateAsync(team.LeaderId, notifyMessage);
            }
            else if (action == "reject")
            {
                application.Status = ProjectApplicationStatus.Rejected;
                // Отправляем уведомление всем участникам команды
                string notifyMessage = $"Ваша заявка на участие в проекте \"{application.Project.IdeaName}\" от команды \"{application.Team.Name}\" отклонена.";
                await _notificationService.CreateAsync(application.Team.CreatorId, notifyMessage);
                await _notificationService.CreateAsync(application.Team.LeaderId, notifyMessage);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Moderation");
        }
    }
} 