using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using lol.Models;
using Microsoft.EntityFrameworkCore;
using lol.Data;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace lol.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // Просмотр профиля
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Получаем команды, где пользователь участник или создатель
            var teams = await _context.Teams
                .Include(t => t.Creator)
                .Include(t => t.Leader)
                .Where(t => t.Members.Any(m => m.Id == user.Id) || t.CreatorId == user.Id)
                .ToListAsync();

            // Получаем активные заявки пользователя
            var requests = await _context.TeamRequests
                .Include(r => r.Team)
                .Where(r => r.UserId == user.Id && r.Status == RequestStatus.Pending)
                .ToListAsync();
            ViewBag.ActiveRequests = requests;

            // Получаем проекты, где пользователь — заказчик
            var projects = await _context.Projects
                .Where(p => p.Customer == user.Email)
                .ToListAsync();
            ViewBag.Projects = projects;

            // Получаем карточку компании пользователя, если она существует
            var companyCard = await _context.CompanyCards
                .FirstOrDefaultAsync(cc => cc.UserId == user.Id);
            ViewBag.CompanyCard = companyCard;

            ViewBag.Teams = teams;
            ViewBag.UserRoles = await _userManager.GetRolesAsync(user);
            return View(user);
        }

        // GET: Редактирование профиля
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: Редактирование профиля
        [HttpPost]
        public async Task<IActionResult> Edit(ApplicationUser model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Group = model.Group;
                user.PhoneNumber = model.PhoneNumber;
                await _userManager.UpdateAsync(user);
                ViewBag.Message = "Данные успешно обновлены!";
                return View(user);
            }
            return View(model);
        }

        // Удаление аккаунта
        [HttpPost]
        public async Task<IActionResult> Delete()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            await _signInManager.SignOutAsync();
            await _userManager.DeleteAsync(user);
            return RedirectToAction("Register", "Account");
        }

        // Загрузка аватарки
        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || avatar == null || avatar.Length == 0)
                return RedirectToAction("Edit");

            var uploads = Path.Combine("wwwroot", "images", "avatars");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            var fileName = $"{user.Id}_{Path.GetFileName(avatar.FileName)}";
            var filePath = Path.Combine(uploads, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }
            user.AvatarPath = $"/images/avatars/{fileName}";
            await _userManager.UpdateAsync(user);
            return RedirectToAction("Edit");
        }
    }
}
