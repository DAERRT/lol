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
    public class ProfileController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context) : base(context)
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

            // Получаем сертификаты пользователя
            var certificates = await _context.Certificates
                .Where(c => c.UserId == user.Id)
                .ToListAsync();
            ViewBag.Certificates = certificates;

            // Получаем компетенции пользователя
            var userWithCompetencies = await _context.Users
                .Include(u => u.Competencies)
                .ThenInclude(c => c.Category)
                .FirstOrDefaultAsync(u => u.Id == user.Id);
            ViewBag.UserCompetencies = userWithCompetencies?.Competencies?.ToList() ?? new List<Competency>();

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

        // Выбор компетенций
        [HttpGet]
        public async Task<IActionResult> Competencies()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Contains("Студент") && !userRoles.Contains("Тимлид"))
            {
                return RedirectToAction("Index");
            }

            var categories = await _context.CompetencyCategories.ToListAsync();
            var competencies = await _context.Competencies.Include(c => c.Category).ToListAsync();
            var userWithCompetencies = await _context.Users
                .Include(u => u.Competencies)
                .FirstOrDefaultAsync(u => u.Id == user.Id);
            var userCompetencyIds = userWithCompetencies?.Competencies?.Select(c => c.Id).ToList() ?? new List<int>();

            ViewBag.Categories = categories;
            ViewBag.Competencies = competencies;
            ViewBag.UserCompetencies = userCompetencyIds;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Competencies(List<int> competencyIds)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Contains("Студент") && !userRoles.Contains("Тимлид"))
            {
                return RedirectToAction("Index");
            }

            var userWithCompetencies = await _context.Users
                .Include(u => u.Competencies)
                .FirstOrDefaultAsync(u => u.Id == user.Id);
            if (userWithCompetencies != null)
            {
                userWithCompetencies.Competencies.Clear();
                if (competencyIds != null && competencyIds.Any())
                {
                    var selectedCompetencies = await _context.Competencies
                        .Where(c => competencyIds.Contains(c.Id))
                        .ToListAsync();
                    userWithCompetencies.Competencies = selectedCompetencies;
                }
                await _context.SaveChangesAsync();
            }

            ViewBag.Message = "Компетенции успешно обновлены!";
            return RedirectToAction("Index");
        }

        // Загрузка сертификатов
        [HttpGet]
        public async Task<IActionResult> Certificates()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Contains("Студент") && !userRoles.Contains("Тимлид"))
            {
                return RedirectToAction("Index");
            }

            var certificates = await _context.Certificates
                .Where(c => c.UserId == user.Id)
                .ToListAsync();
            ViewBag.Certificates = certificates;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadCertificates(List<IFormFile> certificates, List<string> titles)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Contains("Студент") && !userRoles.Contains("Тимлид"))
            {
                return RedirectToAction("Index");
            }

            if (certificates != null && certificates.Any())
            {
                var uploads = Path.Combine("wwwroot", "uploads", "certificates", user.Id);
                if (!Directory.Exists(uploads))
                    Directory.CreateDirectory(uploads);

                for (int i = 0; i < certificates.Count; i++)
                {
                    var cert = certificates[i];
                    if (cert != null && cert.Length > 0)
                    {
                        var fileName = $"{DateTime.Now.Ticks}_{Path.GetFileName(cert.FileName)}";
                        var filePath = Path.Combine(uploads, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await cert.CopyToAsync(stream);
                        }

                        var title = (titles != null && i < titles.Count) ? titles[i] : Path.GetFileNameWithoutExtension(cert.FileName);
                        var certificate = new Certificate
                        {
                            FilePath = $"/uploads/certificates/{user.Id}/{fileName}",
                            Title = title,
                            UserId = user.Id
                        };
                        _context.Certificates.Add(certificate);
                    }
                }
                await _context.SaveChangesAsync();
                ViewBag.Message = "Сертификаты успешно загружены!";
            }
            return RedirectToAction("Certificates");
        }

        // Удаление сертификата
        [HttpPost]
        public async Task<IActionResult> DeleteCertificate(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var certificate = await _context.Certificates.FindAsync(id);
            if (certificate == null || certificate.UserId != user.Id)
                return NotFound();

            _context.Certificates.Remove(certificate);
            await _context.SaveChangesAsync();
            return RedirectToAction("Certificates");
        }
    }
}
