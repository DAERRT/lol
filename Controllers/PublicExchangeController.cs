using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using lol.Data;
using lol.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace lol.Controllers
{
    public class PublicExchangeController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PublicExchangeController> _logger;

        public PublicExchangeController(ApplicationDbContext context, ILogger<PublicExchangeController> logger) : base(context)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Список всех бирж (активные сверху)
        public async Task<IActionResult> Index(string search, bool? isActive)
        {
            var exchanges = _context.ProjectExchanges
                .Include(e => e.Projects)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                exchanges = exchanges.Where(e => e.Name.Contains(search));
            if (isActive.HasValue)
                exchanges = exchanges.Where(e => e.IsActive == isActive.Value);
            exchanges = exchanges.OrderByDescending(e => e.IsActive).ThenBy(e => e.StartDate);
            ViewBag.Search = search;
            ViewBag.IsActive = isActive;
            return View(await exchanges.ToListAsync());
        }

        // GET: Проекты внутри биржи
        public async Task<IActionResult> Details(int id, string projectSearch, ProjectStatus? projectStatus, string projectCustomer)
        {
            var exchange = await _context.ProjectExchanges
                .Include(e => e.Projects)
                .ThenInclude(p => p.ExecutorTeams)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (exchange == null) return NotFound();

            var projects = exchange.Projects.AsQueryable();
            if (!string.IsNullOrWhiteSpace(projectSearch))
                projects = projects.Where(p => p.IdeaName.Contains(projectSearch));
            if (projectStatus.HasValue)
                projects = projects.Where(p => p.Status == projectStatus);
            if (!string.IsNullOrWhiteSpace(projectCustomer))
                projects = projects.Where(p => p.Customer.Contains(projectCustomer));

            ViewBag.ProjectSearch = projectSearch;
            ViewBag.ProjectStatus = projectStatus;
            ViewBag.ProjectCustomer = projectCustomer;
            ViewBag.StatusList = System.Enum.GetValues(typeof(ProjectStatus)).Cast<ProjectStatus>().ToList();
            ViewBag.CustomerList = exchange.Projects.Select(p => p.Customer).Distinct().ToList();
            ViewBag.IsActive = exchange.IsActive;
            exchange.Projects = projects.ToList();

            // Проверяем, является ли пользователь тимлидом или создателем команды
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var userTeams = await _context.Teams
                        .Where(t => t.LeaderId == userId || t.CreatorId == userId)
                        .ToListAsync();
                    var isTeamLeader = userTeams.Any();
                    ViewBag.IsTeamLeader = isTeamLeader;
                    if (isTeamLeader)
                    {
                        var teamIds = userTeams.Select(t => t.Id).ToList();
                        var appliedProjectIds = await _context.ProjectApplications
                            .Where(pa => teamIds.Contains(pa.TeamId))
                            .Select(pa => pa.ProjectId)
                            .ToListAsync();
                        ViewBag.AppliedProjectIds = appliedProjectIds;
                    }
                    else
                    {
                        ViewBag.AppliedProjectIds = new List<int>();
                    }
                }
                else
                {
                    ViewBag.IsTeamLeader = false;
                    ViewBag.AppliedProjectIds = new List<int>();
                }
            }
            else
            {
                ViewBag.IsTeamLeader = false;
                ViewBag.AppliedProjectIds = new List<int>();
            }

            return View(exchange);
        }

        // AJAX: Таблица проектов внутри биржи
        public async Task<IActionResult> ProjectTablePartial(int id, string projectSearch, ProjectStatus? projectStatus, string projectCustomer)
        {
            var exchange = await _context.ProjectExchanges
                .Include(e => e.Projects)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (exchange == null) return NotFound();
            ViewBag.IsActive = exchange.IsActive;
            var projects = exchange.Projects.AsQueryable();
            if (!string.IsNullOrWhiteSpace(projectSearch))
                projects = projects.Where(p => p.IdeaName.Contains(projectSearch));
            if (projectStatus.HasValue)
                projects = projects.Where(p => p.Status == projectStatus);
            if (!string.IsNullOrWhiteSpace(projectCustomer))
                projects = projects.Where(p => p.Customer.Contains(projectCustomer));
            
            _logger.LogInformation($"Exchange ID: {id}, IsActive in DB: {exchange.IsActive}");

            // Проверяем, является ли пользователь тимлидом или создателем команды
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var userTeams = await _context.Teams
                        .Where(t => t.LeaderId == userId)
                        .ToListAsync();
                    var isTeamLeader = userTeams.Any();
                    ViewBag.IsTeamLeader = isTeamLeader;
                    if (isTeamLeader)
                    {
                        var teamIds = userTeams.Select(t => t.Id).ToList();
                        var appliedProjectIds = await _context.ProjectApplications
                            .Where(pa => teamIds.Contains(pa.TeamId))
                            .Select(pa => pa.ProjectId)
                            .ToListAsync();
                        ViewBag.AppliedProjectIds = appliedProjectIds;
                    }
                    else
                    {
                        ViewBag.AppliedProjectIds = new List<int>();
                    }
                }
                else
                {
                    ViewBag.IsTeamLeader = false;
                    ViewBag.AppliedProjectIds = new List<int>();
                }
            }
            else
            {
                ViewBag.IsTeamLeader = false;
                ViewBag.AppliedProjectIds = new List<int>();
            }

            return PartialView("~/Views/PublicExchange/ProjectTablePartial.cshtml", projects.ToList());
        }

        // AJAX: Таблица бирж
        public async Task<IActionResult> ExchangeTablePartial(string search, bool? isActive)
        {
            var exchanges = _context.ProjectExchanges
                .Include(e => e.Projects)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                exchanges = exchanges.Where(e => e.Name.Contains(search));
            if (isActive.HasValue)
                exchanges = exchanges.Where(e => e.IsActive == isActive.Value);
            exchanges = exchanges.OrderByDescending(e => e.IsActive).ThenBy(e => e.StartDate);
            return PartialView("~/Views/PublicExchange/ExchangeTablePartial.cshtml", await exchanges.ToListAsync());
        }
    }
}
