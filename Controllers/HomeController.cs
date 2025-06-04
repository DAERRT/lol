using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace lol.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
} 