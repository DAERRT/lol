using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using lol.Data;
using lol.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace lol.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch analytics data
            var userCount = await _context.Users.CountAsync();
            var projectCount = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.Approved || p.Status == ProjectStatus.Published);
            var teamCount = await _context.Teams.CountAsync();
            var activityCount = await _context.ProjectApplications
                .CountAsync(pa => pa.CreatedAt >= DateTime.UtcNow.AddDays(-7));

            // Data for user growth chart (last 30 days) - placeholder as CreatedAt is not available
            var userGrowthData = Enumerable.Range(0, 30)
                .Select(d => userCount / 30) // Distribute total users evenly as placeholder
                .ToList();

            // Data for project activity chart (last 6 months)
            var projectActivityData = Enumerable.Range(0, 6)
                .Select(m => new
                {
                    Month = DateTime.UtcNow.AddMonths(-5 + m).Month,
                    Year = DateTime.UtcNow.AddMonths(-5 + m).Year,
                    Count = _context.Projects.Count(p => p.CreatedAt.Month == DateTime.UtcNow.AddMonths(-5 + m).Month 
                                                        && p.CreatedAt.Year == DateTime.UtcNow.AddMonths(-5 + m).Year)
                })
                .Select(x => x.Count)
                .ToList();

            // Pass data to view
            ViewBag.UserCount = userCount;
            ViewBag.ProjectCount = projectCount;
            ViewBag.TeamCount = teamCount;
            ViewBag.ActivityCount = activityCount;
            ViewBag.UserGrowthData = userGrowthData;
            ViewBag.ProjectActivityData = projectActivityData;

            return View();
        }
    }
}
