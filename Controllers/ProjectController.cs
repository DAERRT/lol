using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using lol.Models;
using lol.Data;
using System.Collections.Generic;
using System.Linq;
using lol.Services;

namespace lol.Controllers
{
    [Authorize]
    public class ProjectController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public ProjectController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService) : base(context)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: Список проектов
        public async Task<IActionResult> Index(string search, ProjectStatus? status)
        {
            var user = await _userManager.GetUserAsync(User);
            var isCustomer = await _userManager.IsInRoleAsync(user, "Заказчик");

            var projectsQuery = _context.Projects
                .Include(p => p.ExecutorTeams)
                .Include(p => p.ProjectExchanges)
                .AsQueryable();

            if (isCustomer)
                projectsQuery = projectsQuery.Where(p => p.Customer == user.Email);

            if (!string.IsNullOrWhiteSpace(search))
                projectsQuery = projectsQuery.Where(p => p.IdeaName.Contains(search) || p.Customer.Contains(search));

            if (status.HasValue)
                projectsQuery = projectsQuery.Where(p => p.Status == status);

            var projects = await projectsQuery.ToListAsync();
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.StatusList = System.Enum.GetValues(typeof(ProjectStatus)).Cast<ProjectStatus>().ToList();
            return View(projects);
        }

        // GET: Детали проекта
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.ExecutorTeams)
                .Include(p => p.ProjectExchanges)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            // Проверяем, есть ли проект хотя бы в одной активной бирже
            bool isInActiveExchange = project.ProjectExchanges.Any(e => e.IsActive);
            ViewBag.IsInActiveExchange = isInActiveExchange;

            // Для тимлида: определяем, есть ли уже заявка от его команды
            var user = await _userManager.GetUserAsync(User);
            bool isTeamLead = await _userManager.IsInRoleAsync(user, "Тимлид");
            if (isTeamLead)
            {
                var team = await _context.Teams.FirstOrDefaultAsync(t => t.LeaderId == user.Id);
                ViewBag.Team = team;
                bool alreadyApplied = false;
                if (team != null)
                {
                    // Проверяем: если команда уже исполнитель — заявка невозможна, иначе — можно
                    bool isExecutor = project.ExecutorTeams.Any(et => et.Id == team.Id);
                    bool hasPendingApplication = await _context.ProjectApplications.AnyAsync(a => a.ProjectId == project.Id && a.TeamId == team.Id && a.Status == ProjectApplicationStatus.Pending);
                    alreadyApplied = isExecutor || hasPendingApplication;
                }
                ViewBag.AlreadyApplied = alreadyApplied;
            }

            return View(project);
        }

        // GET: Создание проекта
        [Authorize(Roles = "Заказчик")]
        public IActionResult Create()
        {
            ViewData["StackOptions"] = GetStackOptions();
            return View();
        }

        // POST: Создание проекта
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Заказчик")]
        public async Task<IActionResult> Create(Project project)
        {
            // Получаем email текущего пользователя
            var userEmail = User.Identity.Name; // или через UserManager, если email не совпадает с UserName

            // Заполняем обязательные поля, которые не вводятся пользователем
            project.Customer = userEmail;
            project.Initiator = userEmail;
            project.CreatedAt = DateTime.Now;
            project.UpdatedAt = DateTime.Now;
            project.Status = ProjectStatus.New;

            // Удаляем ошибки валидации для этих полей, если они есть
            ModelState.Remove(nameof(Project.Customer));
            ModelState.Remove(nameof(Project.Initiator));

            if (ModelState.IsValid)
            {
                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["StackOptions"] = GetStackOptions();
            return View(project);
        }

        // GET: Редактирование проекта
        [Authorize(Roles = "Заказчик, Администратор")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (project.Customer != user.Email && !await _userManager.IsInRoleAsync(user, "Администратор"))
            {
                return Forbid();
            }

            if (project.Status == ProjectStatus.Published && !await _userManager.IsInRoleAsync(user, "Администратор"))
            {
                return Forbid();
            }

            return View(project);
        }

        // POST: Редактирование проекта
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Заказчик, Администратор")]
        public async Task<IActionResult> Edit(int id, Project project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
                    if (existingProject == null)
                    {
                        return NotFound();
                    }

                    var user = await _userManager.GetUserAsync(User);
                    if (existingProject.Customer != user.Email && !await _userManager.IsInRoleAsync(user, "Администратор"))
                    {
                        return Forbid();
                    }

                    if (existingProject.Status == ProjectStatus.Published && !await _userManager.IsInRoleAsync(user, "Администратор"))
                    {
                        return Forbid();
                    }

                    existingProject.IdeaName = project.IdeaName;
                    existingProject.Problem = project.Problem;
                    existingProject.Solution = project.Solution;
                    existingProject.ExpectedResult = project.ExpectedResult;
                    existingProject.NecessaryResources = project.NecessaryResources;
                    existingProject.Stack = project.Stack;
                    existingProject.UpdatedAt = DateTime.Now;

                    // После любого редактирования проекта (кем угодно):
                    existingProject.Status = ProjectStatus.New;
                    existingProject.EditComment = null;
                    var expertReviews = _context.ExpertReviews.Where(r => r.ProjectId == existingProject.Id);
                    _context.ExpertReviews.RemoveRange(expertReviews);
                    var admins = await _userManager.GetUsersInRoleAsync("Администратор");
                    foreach (var admin in admins)
                    {
                        await _notificationService.CreateAsync(admin.Id, $"Проект '{existingProject.IdeaName}' был отредактирован и требует повторной проверки.");
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(project);
        }

        // GET: Удаление проекта
        [Authorize(Roles = "Заказчик, Администратор")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (project.Customer != user.Email && !await _userManager.IsInRoleAsync(user, "Администратор"))
            {
                return Forbid();
            }

            if (project.Status == ProjectStatus.Published && !await _userManager.IsInRoleAsync(user, "Администратор"))
            {
                return Forbid();
            }

            return View(project);
        }

        // POST: Удаление проекта
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Заказчик, Администратор")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (project.Customer != user.Email && !await _userManager.IsInRoleAsync(user, "Администратор"))
            {
                return Forbid();
            }

            if (project.Status == ProjectStatus.Published && !await _userManager.IsInRoleAsync(user, "Администратор"))
            {
                return Forbid();
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Заказчик, Администратор")]
        public async Task<IActionResult> RemoveExecutor(int projectId, int teamId)
        {
            var project = await _context.Projects
                .Include(p => p.ExecutorTeams)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            var user = await _userManager.GetUserAsync(User);
            if (project == null || project.Customer != user.Email && !await _userManager.IsInRoleAsync(user, "Администратор"))
                return Forbid();

            var team = await _context.Teams.FindAsync(teamId);
            if (team != null && project.ExecutorTeams.Contains(team))
            {
                project.ExecutorTeams.Remove(team);
                team.Status = TeamStatus.LookingForProject;
                // Удаляем заявку этой команды на этот проект
                var application = await _context.ProjectApplications.FirstOrDefaultAsync(a => a.ProjectId == projectId && a.TeamId == teamId);
                if (application != null)
                {
                    _context.ProjectApplications.Remove(application);
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", new { id = projectId });
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }

        private List<string> GetStackOptions()
        {
            return new List<string> {
                "C#", "Java", "Python", "JavaScript", "TypeScript", "PHP", "Go", "Kotlin", "Swift", "Ruby", "C++",
                ".NET", "ASP.NET", "Django", "Flask", "Spring", "React", "Angular", "Vue.js", "Node.js", "Express", "Laravel"
            };
        }

        // AJAX: Таблица проектов
        public async Task<IActionResult> ProjectTablePartial(string search, ProjectStatus? status)
        {
            var user = await _userManager.GetUserAsync(User);
            var isCustomer = await _userManager.IsInRoleAsync(user, "Заказчик");
            var projectsQuery = _context.Projects
                .Include(p => p.ExecutorTeams)
                .Include(p => p.ProjectExchanges)
                .AsQueryable();
            if (isCustomer)
                projectsQuery = projectsQuery.Where(p => p.Customer == user.Email);
            if (!string.IsNullOrWhiteSpace(search))
                projectsQuery = projectsQuery.Where(p => p.IdeaName.Contains(search) || p.Customer.Contains(search));
            if (status.HasValue)
                projectsQuery = projectsQuery.Where(p => p.Status == status);
            var projects = await projectsQuery.ToListAsync();
            return PartialView("~/Views/Project/ProjectTablePartial.cshtml", projects);
        }
    }
} 