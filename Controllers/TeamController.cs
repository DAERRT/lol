using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using lol.Models;
using lol.Data;
using lol.Services;

namespace lol.Controllers
{
    [Authorize]
    public class TeamController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TeamController> _logger;
        private readonly NotificationService _notificationService;

        public TeamController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<TeamController> logger,
            NotificationService notificationService) : base(context)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        // Список всех команд
        public async Task<IActionResult> Index(string search, TeamStatus? status)
        {
            var teamsQuery = _context.Teams
                .Include(t => t.Creator)
                .Include(t => t.Leader)
                .Include(t => t.Members)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                teamsQuery = teamsQuery.Where(t => t.Name.Contains(search) || t.Leader.UserName.Contains(search));

            if (status.HasValue)
                teamsQuery = teamsQuery.Where(t => t.Status == status);

            var teams = await teamsQuery.ToListAsync();

            var user = await _userManager.GetUserAsync(User);
            var requests = new Dictionary<int, bool>();
            if (user != null)
            {
                var userRequests = await _context.TeamRequests
                    .Where(r => r.UserId == user.Id && r.Status == RequestStatus.Pending)
                    .ToListAsync();
                foreach (var team in teams)
                {
                    requests[team.Id] = userRequests.Any(r => r.TeamId == team.Id);
                }
            }
            ViewBag.HasPendingRequest = requests;
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.StatusList = System.Enum.GetValues(typeof(TeamStatus)).Cast<TeamStatus>().ToList();
            return View(teams);
        }

        // Создание команды
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Team team)
        {
            // Удаляем ошибки для серверных полей
            ModelState.Remove(nameof(team.CreatorId));
            ModelState.Remove(nameof(team.Creator));
            ModelState.Remove(nameof(team.LeaderId));
            ModelState.Remove(nameof(team.Leader));

            _logger.LogInformation("Начало создания команды");
            _logger.LogInformation($"ModelState.IsValid: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogError($"Ошибка валидации: {error.ErrorMessage}");
                }
                return View(team);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogError("Пользователь не найден");
                    return NotFound("Пользователь не найден");
                }

                _logger.LogInformation($"Создание команды пользователем {user.UserName}");

                team.CreatorId = user.Id;
                team.LeaderId = user.Id;
                team.DateCreated = DateTime.Now;
                team.DateUpdated = DateTime.Now;
                team.Members = new List<ApplicationUser> { user };

                _logger.LogInformation($"Данные команды: Name={team.Name}, Desc={team.Desc}, IsPrivate={team.IsPrivate}, Status={team.Status}");

                _context.Teams.Add(team);
                await _context.SaveChangesAsync();
                
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании команды");
                ModelState.AddModelError("", "Произошла ошибка при создании команды. Пожалуйста, попробуйте снова.");
                return View(team);
            }
        }

        // Детали команды
        public async Task<IActionResult> Details(int id)
        {
            var team = await _context.Teams
                .Include(t => t.Creator)
                .Include(t => t.Leader)
                .Include(t => t.Members)
                .Include(t => t.ExecutorProjects)
                .Include(t => t.TeamRequests)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.IsMember = team.Members.Contains(currentUser);
            ViewBag.IsCreator = team.CreatorId == currentUser.Id;
            ViewBag.HasPendingRequest = await _context.TeamRequests
                .AnyAsync(r => r.TeamId == id && r.UserId == currentUser.Id && r.Status == RequestStatus.Pending);

            return View(team);
        }

        // Подача заявки на вступление
        [HttpGet]
        public async Task<IActionResult> RequestJoin(int id)
        {
            var team = await _context.Teams.FindAsync(id);
            if (team == null)
                return NotFound();
            return View(team);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestJoin(int id, string message)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound();
            }

            if (team.IsPrivate)
            {
                return Forbid(); // Приватная команда — нельзя подать заявку
            }

            var user = await _userManager.GetUserAsync(User);

            // Проверяем, не является ли пользователь уже участником
            if (team.Members.Contains(user))
            {
                return BadRequest("Вы уже являетесь участником этой команды");
            }

            // Проверяем, нет ли уже активной заявки
            var existingRequest = await _context.TeamRequests
                .FirstOrDefaultAsync(r => r.TeamId == id && r.UserId == user.Id && r.Status == RequestStatus.Pending);

            if (existingRequest != null)
            {
                return BadRequest("У вас уже есть активная заявка на вступление");
            }

            var UserCompetencies = await _context.UserCompetencies
                .Where(uc => uc.UserId == user.Id)
                .Include(uc => uc.Competency)
                    .ThenInclude(c => c.Category)
                .Select(uc => new
                {
                    Name = uc.Competency.Name,
                    CategoryColor = uc.Competency.Category.Color
                })
                .ToListAsync();

            var UserCertificates = await _context.Certificates
                .Where(uc => uc.UserId == user.Id)
                .Select(uc => new
                {
                    Title = uc.Title,
                    FilePath = uc.FilePath,
                    UploadDate = uc.UploadDate.ToString("dd.MM.yyyy")
                })
                .ToListAsync();

            var request = new TeamRequest
            {
                UserId = user.Id,
                TeamId = id,
                CompetenciesAtRequestJson = Newtonsoft.Json.JsonConvert.SerializeObject(UserCompetencies),
                CertificatesAtRequestJson = Newtonsoft.Json.JsonConvert.SerializeObject(UserCertificates),
                Message = message,
                Status = RequestStatus.Pending,
                DateCreated = DateTime.Now
            };

            _context.TeamRequests.Add(request);
            await _context.SaveChangesAsync();

            // Отправляем уведомление создателю команды
            string notifyMessage = $"Новая заявка на вступление в вашу команду \"{team.Name}\" от пользователя {user.FirstName} {user.LastName}.";
            await _notificationService.CreateAsync(team.CreatorId, notifyMessage);

            return RedirectToAction(nameof(Details), new { id });
        }

        // Управление заявками (для создателя команды)
        public async Task<IActionResult> ManageRequests(int id)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (team.CreatorId != currentUser.Id)
            {
                return Forbid();
            }

            var requests = await _context.TeamRequests
                .Include(r => r.User)
                .Where(r => r.TeamId == id && r.Status == RequestStatus.Pending)
                .ToListAsync();

            ViewBag.TeamId = id;
            return View(requests);
        }

        
        [HttpPost]
        public async Task<IActionResult> RemoveMember(int teamId, string userId)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);
            var user = await _userManager.FindByIdAsync(userId);
            if (team.Members.Any(m => m.Id == userId))
            {
                team.Members.Remove(user);
                await _context.SaveChangesAsync();
                // Отправляем уведомление пользователю
                string message = $"Вы были исключены из команды \"{team.Name}\".";
                await _notificationService.CreateAsync(userId, message);
            }
            return RedirectToAction(nameof(Details), new { id = teamId });
        }

        // Обработка заявки (одобрение/отклонение)
        [HttpPost]
        public async Task<IActionResult> ProcessRequest(int requestId, bool approve)
        {
            var request = await _context.TeamRequests
                .Include(r => r.Team)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (request.Team.CreatorId != currentUser.Id)
            {
                return Forbid();
            }

            request.Status = approve ? RequestStatus.Approved : RequestStatus.Rejected;
            request.DateProcessed = DateTime.Now;

            if (approve)
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                request.Team.Members.Add(user);
            }

            await _context.SaveChangesAsync();

            // Отправляем уведомление пользователю
            string message = approve
                ? $"Ваша заявка на вступление в команду \"{request.Team.Name}\" одобрена!"
                : $"Ваша заявка на вступление в команду \"{request.Team.Name}\" отклонена.";

            await _notificationService.CreateAsync(request.UserId, message);

            return RedirectToAction(nameof(ManageRequests), new { id = request.TeamId });
        }

        // Выход из команды
        [HttpPost]
        public async Task<IActionResult> Leave(int id)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            // Проверяем, не является ли пользователь создателем
            if (team.CreatorId == user.Id)
            {
                return BadRequest("Создатель не может покинуть команду");
            }

            // Если пользователь является лидером, нужно назначить нового лидера
            if (team.LeaderId == user.Id)
            {
                var newLeader = team.Members.FirstOrDefault(m => m.Id != user.Id);
                if (newLeader != null)
                {
                    team.LeaderId = newLeader.Id;
                }
            }

            team.Members.Remove(user);
            await _context.SaveChangesAsync();

            // Отправляем уведомление создателю команды
            string notifyMessage = $"Пользователь {user.FirstName} {user.LastName} покинул вашу команду \"{team.Name}\".";
            await _notificationService.CreateAsync(team.CreatorId, notifyMessage);

            return RedirectToAction(nameof(Index));
        }

        // GET: Редактирование команды
        public async Task<IActionResult> Edit(int id)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (team == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (team.CreatorId != currentUser.Id && !await _userManager.IsInRoleAsync(currentUser, "Администратор"))
                return Forbid();

            ViewBag.Members = team.Members;
            return View(team);
        }

        // POST: Редактирование команды
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Team model, string leaderId)
        {
            // Удаляем ошибки для серверных полей
            ModelState.Remove(nameof(model.CreatorId));
            ModelState.Remove(nameof(model.Creator));
            ModelState.Remove(nameof(model.LeaderId));
            ModelState.Remove(nameof(model.Leader));

            var team = await _context.Teams.Include(t => t.Members).FirstOrDefaultAsync(t => t.Id == id);
            if (team == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (team.CreatorId != currentUser.Id && !await _userManager.IsInRoleAsync(currentUser, "Администратор"))
                return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.Members = team.Members;
                return View(model);
            }

            string oldLeaderId = team.LeaderId;
            team.Name = model.Name;
            team.Desc = model.Desc;
            team.IsPrivate = model.IsPrivate;
            team.Status = model.Status;
            team.DateUpdated = DateTime.Now;
            if (!string.IsNullOrEmpty(leaderId) && team.Members.Any(m => m.Id == leaderId))
                team.LeaderId = leaderId;

            await _context.SaveChangesAsync();

            // Назначаем роль "Тимлид" новому лидеру
            var newLeader = await _userManager.FindByIdAsync(team.LeaderId);
            if (newLeader != null && !await _userManager.IsInRoleAsync(newLeader, "Тимлид"))
            {
                await _userManager.AddToRoleAsync(newLeader, "Тимлид");
            }
            // (Опционально) Убираем роль у старого лидера, если он больше не лидер ни одной команды
            if (!string.IsNullOrEmpty(oldLeaderId) && oldLeaderId != team.LeaderId)
            {
                var oldLeader = await _userManager.FindByIdAsync(oldLeaderId);
                if (oldLeader != null)
                {
                    bool isStillLeader = await _context.Teams.AnyAsync(t => t.LeaderId == oldLeaderId);
                    if (!isStillLeader && await _userManager.IsInRoleAsync(oldLeader, "Тимлид"))
                    {
                        await _userManager.RemoveFromRoleAsync(oldLeader, "Тимлид");
                    }
                }
            }

            return RedirectToAction("Details", new { id = team.Id });
        }

        // GET: Удаление команды
        public async Task<IActionResult> Delete(int id)
        {
            var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == id);
            if (team == null) return NotFound();
            var currentUser = await _userManager.GetUserAsync(User);
            if (team.CreatorId != currentUser.Id && !await _userManager.IsInRoleAsync(currentUser, "Администратор"))
                return Forbid();
            return View(team);
        }

        // POST: Удаление команды
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (team == null) return NotFound();
            var currentUser = await _userManager.GetUserAsync(User);
            if (team.CreatorId != currentUser.Id && !await _userManager.IsInRoleAsync(currentUser, "Администратор"))
                return Forbid();

            // Отправляем уведомления всем участникам
            string notifyMessage = $"Команда \"{team.Name}\" была удалена.";
            foreach (var member in team.Members)
            {
                await _notificationService.CreateAsync(member.Id, notifyMessage);
            }

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
            // Если у команды был тимлид, удаляем у него роль "Тимлид", если он не является тимлидом в других командах
            if (team.LeaderId != null)
            {
                var leader = await _userManager.FindByIdAsync(team.LeaderId);
                if (leader != null)
                {
                    var otherTeams = await _context.Teams.Where(t => t.LeaderId == leader.Id && t.Id != team.Id).AnyAsync();
                    if (!otherTeams)
                    {
                        await _userManager.RemoveFromRoleAsync(leader, "Тимлид");
                    }
                }
            }
            return RedirectToAction("Index");
        }

        // AJAX: Таблица команд
        public async Task<IActionResult> TeamTablePartial(string search, TeamStatus? status)
        {
            var teamsQuery = _context.Teams
                .Include(t => t.Creator)
                .Include(t => t.Leader)
                .Include(t => t.Members)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                teamsQuery = teamsQuery.Where(t => t.Name.Contains(search) || t.Leader.UserName.Contains(search));
            if (status.HasValue)
                teamsQuery = teamsQuery.Where(t => t.Status == status);
            var teams = await teamsQuery.ToListAsync();
            var user = await _userManager.GetUserAsync(User);
            var requests = new Dictionary<int, bool>();
            if (user != null)
            {
                var userRequests = await _context.TeamRequests
                    .Where(r => r.UserId == user.Id && r.Status == RequestStatus.Pending)
                    .ToListAsync();
                foreach (var team in teams)
                {
                    requests[team.Id] = userRequests.Any(r => r.TeamId == team.Id);
                }
            }
            ViewBag.HasPendingRequest = requests;
            return PartialView("~/Views/Team/TeamTablePartial.cshtml", teams);
        }
    }
}
