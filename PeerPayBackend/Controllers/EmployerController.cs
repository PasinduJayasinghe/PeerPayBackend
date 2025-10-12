using Microsoft.AspNetCore.Mvc;

namespace PeerPayBackend.Controllers
{
    public class EmployerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
