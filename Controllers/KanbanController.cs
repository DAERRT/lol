using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using lol.Data;
using lol.Models;
using lol.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace lol.Controllers
{
    [Authorize]
    public class KanbanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public KanbanController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: Kanban/Index
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            // Получаем команды, в которых состоит пользователь
            var teams = await _context.Teams
                .Include(t => t.ExecutorProjects)
                .Where(t => t.Members.Any(m => m.Id == userId) || t.CreatorId == userId)
                .ToListAsync();

            // Получаем проекты, где пользователь является заказчиком
            var customerProjects = await _context.Projects
                .Include(p => p.ExecutorTeams)
                .Where(p => p.Customer == user.Email)
                .ToListAsync();

            // Формируем список доступных пар команда-проект
            var boardPairs = new List<(Team Team, Project Project)>();

            foreach (var team in teams)
            {
                foreach (var project in team.ExecutorProjects)
                {
                    boardPairs.Add((team, project));
                }
            }

            foreach (var project in customerProjects)
            {
                foreach (var team in project.ExecutorTeams)
                {
                    if (!boardPairs.Any(bp => bp.Team.Id == team.Id && bp.Project.Id == project.Id))
                    {
                        boardPairs.Add((team, project));
                    }
                }
            }

            ViewBag.BoardPairs = boardPairs;
            return View();
        }

        // GET: Kanban/Board
        public async Task<IActionResult> Board(int projectId, int teamId)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            // Проверяем, имеет ли пользователь доступ к этой паре команда-проект
            var project = await _context.Projects
                .Include(p => p.ExecutorTeams)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (project == null || team == null || 
                (!team.Members.Any(m => m.Id == userId) && team.CreatorId != userId && project.Customer != user.Email))
            {
                return Unauthorized();
            }

            // Получаем задачи для этой пары команда-проект
            var tasks = await _context.KanbanTasks
                .Include(kt => kt.CreatedBy)
                .Include(kt => kt.AssignedTo)
                .Where(kt => kt.ProjectId == projectId && kt.TeamId == teamId)
                .ToListAsync();

            // Получаем членов команды для возможности назначения задач
            var teamMembers = team.Members.ToList();
            if (!teamMembers.Any(m => m.Id == team.CreatorId))
            {
                teamMembers.Add(await _context.Users.FindAsync(team.CreatorId));
            }

            ViewBag.Project = project;
            ViewBag.Team = team;
            ViewBag.Tasks = tasks;
            ViewBag.TeamMembers = teamMembers;
            return View();
        }

        // POST: Kanban/CreateTask
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTask(int projectId, int teamId, KanbanTask model)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            // Проверяем доступ пользователя к паре команда-проект
            var project = await _context.Projects
                .Include(p => p.ExecutorTeams)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (project == null || team == null || 
                (!team.Members.Any(m => m.Id == userId) && team.CreatorId != userId && project.Customer != user.Email))
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                model.ProjectId = projectId;
                model.TeamId = teamId;
                model.CreatedById = userId;
                model.CreatedAt = DateTime.UtcNow;

                _context.Add(model);
                await _context.SaveChangesAsync();

                // Отправляем уведомления всем участникам доски
                var recipientIds = new List<string>();
                
                // Добавляем членов команды
                recipientIds.AddRange(team.Members.Select(m => m.Id));
                if (!recipientIds.Contains(team.CreatorId))
                {
                    recipientIds.Add(team.CreatorId);
                }

                // Добавляем заказчика проекта, если это не текущий пользователь
                var customer = await _context.Users.FirstOrDefaultAsync(u => u.Email == project.Customer);
                if (customer != null && customer.Id != userId && !recipientIds.Contains(customer.Id))
                {
                    recipientIds.Add(customer.Id);
                }

                foreach (var recipientId in recipientIds)
                {
                    if (recipientId != userId) // Не отправляем уведомление самому себе
                    {
                        await _notificationService.CreateAsync(recipientId, $"Новая задача '{model.Title}' добавлена на канбан-доску проекта '{project.IdeaName}' для команды '{team.Name}'.");
                    }
                }

                return RedirectToAction(nameof(Board), new { projectId, teamId });
            }

            // Если модель недействительна, возвращаем на страницу доски с ошибками
            ViewBag.Project = project;
            ViewBag.Team = team;
            ViewBag.Tasks = await _context.KanbanTasks
                .Include(kt => kt.CreatedBy)
                .Include(kt => kt.AssignedTo)
                .Where(kt => kt.ProjectId == projectId && kt.TeamId == teamId)
                .ToListAsync();

            var teamMembers = team.Members.ToList();
            if (!teamMembers.Any(m => m.Id == team.CreatorId))
            {
                teamMembers.Add(await _context.Users.FindAsync(team.CreatorId));
            }
            ViewBag.TeamMembers = teamMembers;

            return View("Board", model);
        }

        // POST: Kanban/UpdateTaskStatus
        [HttpPost]
        public async Task<IActionResult> UpdateTaskStatus(int taskId, KanbanTaskStatus newStatus)
        {
            var task = await _context.KanbanTasks
                .Include(kt => kt.Project)
                .Include(kt => kt.Team)
                .ThenInclude(t => t.Members)
                .FirstOrDefaultAsync(kt => kt.Id == taskId);

            if (task == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            // Проверяем доступ пользователя к паре команда-проект
            if (!task.Team.Members.Any(m => m.Id == userId) && task.Team.CreatorId != userId && task.Project.Customer != user.Email)
            {
                return Unauthorized();
            }

            task.Status = newStatus;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // GET: Kanban/GetTasks
        [HttpGet]
        public async Task<IActionResult> GetTasks(int projectId, int teamId)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            // Проверяем доступ пользователя к паре команда-проект
            var project = await _context.Projects
                .Include(p => p.ExecutorTeams)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (project == null || team == null || 
                (!team.Members.Any(m => m.Id == userId) && team.CreatorId != userId && project.Customer != user.Email))
            {
                return Unauthorized();
            }

            var tasks = await _context.KanbanTasks
                .Include(kt => kt.CreatedBy)
                .Include(kt => kt.AssignedTo)
                .Where(kt => kt.ProjectId == projectId && kt.TeamId == teamId)
                .Select(kt => new
                {
                    id = kt.Id,
                    title = kt.Title,
                    description = kt.Description,
                    status = kt.Status.ToString(),
                    priority = kt.Priority.ToString(),
                    createdAt = kt.CreatedAt,
                    deadline = kt.Deadline,
                    createdBy = kt.CreatedBy != null ? kt.CreatedBy.UserName : "Неизвестно",
                    assignedTo = kt.AssignedTo != null ? kt.AssignedTo.UserName : "Не назначено"
                })
                .ToListAsync();

            return Json(tasks);
        }
    }
}
