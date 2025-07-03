using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using lol.Data;
using lol.Models;
using lol.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace lol.Controllers
{
    [Authorize]
    public class CompanyCardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public CompanyCardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: CompanyCard/Index
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var companyCard = await _context.CompanyCards
                .FirstOrDefaultAsync(cc => cc.UserId == userId);

            return RedirectToAction("Details",new {id = companyCard.Id});
        }

        // GET: CompanyCard/Create
        [Authorize(Roles = "Новый пользователь")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: CompanyCard/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Новый пользователь")]
        public async Task<IActionResult> Create(CompanyCard model, IFormFile document)
        {
            // Remove validation for fields not filled by user
            ModelState.Remove(nameof(model.UserId));
            ModelState.Remove(nameof(model.User));
            ModelState.Remove(nameof(model.ModeratorComment));
            ModelState.Remove(nameof(model.DocumentPath));

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                var existingCard = await _context.CompanyCards
                    .FirstOrDefaultAsync(cc => cc.UserId == userId);

                if (existingCard != null)
                {
                    ModelState.AddModelError("", "У вас уже есть карточка компании. Вы не можете создать новую, пока текущая не будет удалена или отклонена.");
                    return View(model);
                }

                model.UserId = userId;
                model.Status = CompanyCardStatus.Pending;
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;
                model.ModeratorComment = string.Empty; // Ensure it's not null

                if (document != null && document.Length > 0)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/company_docs", $"{userId}_{document.FileName}");
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/company_docs"));
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await document.CopyToAsync(stream);
                    }
                    model.DocumentPath = $"/uploads/company_docs/{userId}_{document.FileName}";
                }
                else
                {
                    model.DocumentPath = string.Empty; // Ensure it's not null if no document is uploaded
                }

                _context.Add(model);
                await _context.SaveChangesAsync();

                // Notify administrators
                var admins = await _userManager.GetUsersInRoleAsync("Администратор");
                foreach (var admin in admins)
                {
                    await _notificationService.CreateAsync(admin.Id, $"Новая заявка на роль 'Заказчик' от пользователя {User.Identity.Name}.");
                }

                return RedirectToAction("Index","Profile");
            }
            else
            {
                // Log validation errors for debugging
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                ViewBag.ValidationErrors = errors;
                return View(model);
            }
        }

        // GET: CompanyCard/Details
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var companyCard = await _context.CompanyCards
                .FirstOrDefaultAsync(cc => cc.Id == id && cc.UserId == userId);

            if (companyCard == null)
            {
                return NotFound();
            }

            return View(companyCard);
        }
    }
}
