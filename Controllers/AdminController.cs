using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using lol.Models;
using System.Linq.Dynamic.Core;
using lol.Data;
using lol.Services;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace lol.Controllers
{
    [Authorize(Roles = "Администратор")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger,
            ApplicationDbContext context,
            NotificationService notificationService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(string searchString, string sortOrder, string groupFilter, string roleFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["GroupFilter"] = groupFilter;
            ViewData["RoleFilter"] = roleFilter;

            var users = _userManager.Users.AsQueryable();

            // Поиск
            if (!String.IsNullOrEmpty(searchString))
            {
                users = users.Where(u =>
                    u.Email.Contains(searchString) ||
                    u.FirstName.Contains(searchString) ||
                    u.LastName.Contains(searchString) ||
                    u.Group.Contains(searchString) ||
                    u.PhoneNumber.Contains(searchString));
            }

            // Фильтр по группе
            if (!String.IsNullOrEmpty(groupFilter))
            {
                users = users.Where(u => u.Group == groupFilter);
            }

            // Сортировка
            if (!String.IsNullOrEmpty(sortOrder))
            {
                users = users.OrderBy(sortOrder);
            }
            else
            {
                users = users.OrderBy(u => u.Email);
            }

            // Получаем роли для каждого пользователя
            var userList = new List<UserViewModel>();
            foreach (var user in await users.ToListAsync())
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Group = user.Group,
                    PhoneNumber = user.PhoneNumber,
                    Roles = roles.ToList()
                });
            }

            // Фильтр по ролям
            if (!String.IsNullOrEmpty(roleFilter))
            {
                userList = userList.Where(u => u.Roles.Contains(roleFilter)).ToList();
            }

            // Получаем список всех групп и ролей для фильтров
            ViewBag.Groups = await _userManager.Users.Select(u => u.Group).Distinct().ToListAsync();
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            return View(userList);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Group = user.Group,
                PhoneNumber = user.PhoneNumber,
                AllRoles = roles.Select(r => new RoleViewModel
                {
                    Name = r.Name,
                    IsSelected = userRoles.Contains(r.Name)
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return NotFound();
                }

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Group = model.Group;
                user.PhoneNumber = model.PhoneNumber;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    // Обновляем роли
                    var userRoles = await _userManager.GetRolesAsync(user);
                    var selectedRoles = model.AllRoles.Where(r => r.IsSelected).Select(r => r.Name);

                    await _userManager.RemoveFromRolesAsync(user, userRoles);
                    await _userManager.AddToRolesAsync(user, selectedRoles);

                    // Отправляем уведомление пользователю
                    string notifyMessage = $"Данные вышего аккаунта были изменены администратором.";
                    await _notificationService.CreateAsync(user.Id, notifyMessage);

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            // Если что-то пошло не так, перезагружаем роли
            model.AllRoles = (await _roleManager.Roles.ToListAsync())
                .Select(r => new RoleViewModel
                {
                    Name = r.Name,
                    IsSelected = model.AllRoles?.Any(ur => ur.Name == r.Name && ur.IsSelected) ?? false
                }).ToList();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Проверяем, не пытаемся ли мы удалить текущего пользователя
            if (user.Id == _userManager.GetUserId(User))
            {
                return BadRequest("Вы не можете удалить свой собственный аккаунт");
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return RedirectToAction(nameof(Index));
        }

        // Вспомогательный метод для получения статусов с DisplayName
        private static List<(string Value, string Display)> GetProjectStatusList()
        {
            return Enum.GetValues(typeof(ProjectStatus))
                .Cast<ProjectStatus>()
                .Select(e => (
                    e.ToString(),
                    e.GetType()
                     .GetMember(e.ToString())[0]
                     .GetCustomAttribute<DisplayAttribute>()?.GetName() ?? e.ToString()
                )).ToList();
        }

        // Управление проектами
        public async Task<IActionResult> Projects(string searchString, string statusFilter)
        {
            var projects = _context.Projects.Include(p => p.ExecutorTeams).AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                projects = projects.Where(p => p.IdeaName.Contains(searchString) || p.Customer.Contains(searchString));
            }
            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (Enum.TryParse<ProjectStatus>(statusFilter, out var statusEnum))
                {
                    projects = projects.Where(p => p.Status == statusEnum);
                }
            }
            var projectList = await projects.ToListAsync();
            // Загружаем оценки экспертов для каждого проекта
            var expertReviewsDict = new Dictionary<int, List<ExpertReview>>();
            foreach (var project in projectList)
            {
                var reviews = await _context.ExpertReviews
                    .Where(r => r.ProjectId == project.Id)
                    .Include(r => r.Expert)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
                expertReviewsDict[project.Id] = reviews;
            }
            ViewBag.ExpertReviews = expertReviewsDict;
            ViewBag.Statuses = GetProjectStatusList();
            ViewData["StatusFilter"] = statusFilter;
            ViewData["searchString"] = searchString;
            return View(projectList);
        }

        // Управление командами
        public async Task<IActionResult> Teams(string searchString, string statusFilter)
        {
            var teams = _context.Teams.Include(t => t.Leader).Include(t => t.Members).AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                teams = teams.Where(t => t.Name.Contains(searchString) || t.Leader.FirstName.Contains(searchString) || t.Leader.LastName.Contains(searchString));
            }
            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (Enum.TryParse<TeamStatus>(statusFilter, out var statusEnum))
                {
                    teams = teams.Where(t => t.Status == statusEnum);
                }
            }
            ViewBag.Statuses = Enum.GetNames(typeof(TeamStatus));
            ViewData["StatusFilter"] = statusFilter;
            ViewData["searchString"] = searchString;
            return View(await teams.ToListAsync());
        }

        // Управление заявками на проекты
        public async Task<IActionResult> ProjectApplications(string searchString, string statusFilter)
        {
            var applications = _context.ProjectApplications
                .Include(a => a.Project)
                .Include(a => a.Team)
                .AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                applications = applications.Where(a => a.Project.IdeaName.Contains(searchString) || a.Team.Name.Contains(searchString));
            }
            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (Enum.TryParse<ProjectApplicationStatus>(statusFilter, out var statusEnum))
                {
                    applications = applications.Where(a => a.Status == statusEnum);
                }
            }
            ViewBag.Statuses = Enum.GetNames(typeof(ProjectApplicationStatus));
            ViewData["StatusFilter"] = statusFilter;
            ViewData["searchString"] = searchString;
            return View(await applications.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProjectStatus(int projectId, ProjectStatus newStatus, string? editComment)
        {
            if (newStatus == ProjectStatus.Published)
            {
                TempData["Error"] = "Статус 'Опубликована' может быть установлен только автоматически после одобрения тремя экспертами.";
                return RedirectToAction(nameof(Projects));
            }
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }
            project.Status = newStatus;
            project.UpdatedAt = DateTime.Now;
            var user = await _userManager.FindByEmailAsync(project.Customer);
            if (newStatus == ProjectStatus.Editing)
            {
                project.EditComment = editComment;
                // Уведомление с комментарием
                string notifyEdit = $"Ваша идея отправлена на доработку администратором. Комментарий: {editComment}";
                await _notificationService.CreateAsync(user.Id, notifyEdit);
            }
            else
            {
                project.EditComment = null;
            }
            await _context.SaveChangesAsync();
            // Стандартное уведомление о смене статуса
            string notifyMessage = $"Статус проекта \"{project.IdeaName}\" изменен на \"{newStatus}\".";
            await _notificationService.CreateAsync(user.Id, notifyMessage);
            return RedirectToAction(nameof(Projects));
        }

        // Управление биржами проектов
        public async Task<IActionResult> ProjectExchanges(string search, bool? isActive)
        {
            var exchanges = _context.ProjectExchanges.Include(e => e.Projects).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                exchanges = exchanges.Where(e => e.Name.Contains(search));
            if (isActive.HasValue)
                exchanges = exchanges.Where(e => e.IsActive == isActive.Value);
            exchanges = exchanges.OrderByDescending(e => e.IsActive).ThenBy(e => e.StartDate);
            ViewBag.Search = search;
            ViewBag.IsActive = isActive;
            return View(await exchanges.ToListAsync());
        }
    }
} 