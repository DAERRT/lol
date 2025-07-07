using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using lol.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using lol.Models;

namespace lol.Controllers
{
    public class BaseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BaseController() : this(null)
        {
        }

        public BaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Check if user has access to Kanban board
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userEmail = User.Identity.Name; // Assuming email is used as username

            if (!string.IsNullOrEmpty(userId) && _context != null)
            {
                var teams = await _context.Teams
                    .Include(t => t.ExecutorProjects)
                    .Where(t => t.Members.Any(m => m.Id == userId) || t.CreatorId == userId)
                    .ToListAsync();

                var customerProjects = await _context.Projects
                    .Include(p => p.ExecutorTeams)
                    .Where(p => p.Customer == userEmail)
                    .ToListAsync();

                var boardPairs = new List<(Team, Project)>();

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
                        if (!boardPairs.Any(bp => bp.Item1.Id == team.Id && bp.Item2.Id == project.Id))
                        {
                            boardPairs.Add((team, project));
                        }
                    }
                }

                ViewBag.HasKanbanAccess = boardPairs.Any();
            }
            else
            {
                ViewBag.HasKanbanAccess = false;
            }

            await next();
        }
    }
}
