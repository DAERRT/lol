using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using lol.Data;
using lol.Models;

namespace lol.Controllers
{
    [Authorize(Roles = "Администратор")]
    public class ProjectExchangeController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProjectExchangeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Создать
        public IActionResult Create()
        {
            ViewBag.Projects = _context.Projects.Where(p => p.Status == ProjectStatus.Published).ToList();
            return View();
        }

        // POST: Создать
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectExchange exchange, int[] selectedProjects)
        {
            if (ModelState.IsValid)
            {
                exchange.Projects = _context.Projects.Where(p => selectedProjects.Contains(p.Id)).ToList();
                _context.ProjectExchanges.Add(exchange);
                await _context.SaveChangesAsync();
                return RedirectToAction("ProjectExchanges", "Admin");
            }
            ViewBag.Projects = _context.Projects.Where(p => p.Status == ProjectStatus.Published).ToList();
            return View(exchange);
        }

        // GET: Редактировать
        public async Task<IActionResult> Edit(int id)
        {
            var exchange = await _context.ProjectExchanges.Include(e => e.Projects).FirstOrDefaultAsync(e => e.Id == id);
            if (exchange == null) return NotFound();
            ViewBag.Projects = _context.Projects.Where(p => p.Status == ProjectStatus.Published).ToList();
            return View(exchange);
        }

        // POST: Редактировать
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProjectExchange exchange, int[] selectedProjects)
        {
            var dbExchange = await _context.ProjectExchanges.Include(e => e.Projects).FirstOrDefaultAsync(e => e.Id == id);
            if (dbExchange == null) return NotFound();
            if (ModelState.IsValid)
            {
                dbExchange.Name = exchange.Name;
                dbExchange.StartDate = exchange.StartDate;
                dbExchange.EndDate = exchange.EndDate;
                dbExchange.IsActive = exchange.IsActive;
                dbExchange.Projects.Clear();
                var projects = _context.Projects.Where(p => selectedProjects.Contains(p.Id)).ToList();
                foreach (var p in projects) dbExchange.Projects.Add(p);
                await _context.SaveChangesAsync();
                return RedirectToAction("ProjectExchanges", "Admin");
            }
            ViewBag.Projects = _context.Projects.Where(p => p.Status == ProjectStatus.Published).ToList();
            return View(exchange);
        }

        // GET: Удалить
        public async Task<IActionResult> Delete(int id)
        {
            var exchange = await _context.ProjectExchanges.Include(e => e.Projects).FirstOrDefaultAsync(e => e.Id == id);
            if (exchange == null) return NotFound();
            return View(exchange);
        }

        // POST: Удалить
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var exchange = await _context.ProjectExchanges.FindAsync(id);
            if (exchange == null) return NotFound();
            _context.ProjectExchanges.Remove(exchange);
            await _context.SaveChangesAsync();
            return RedirectToAction("ProjectExchanges", "Admin");
        }
    }
} 