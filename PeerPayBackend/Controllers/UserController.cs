using Microsoft.AspNetCore.Mvc;

namespace PeerPayBackend.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
