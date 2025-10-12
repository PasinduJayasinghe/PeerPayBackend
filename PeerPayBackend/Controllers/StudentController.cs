using Microsoft.AspNetCore.Mvc;

namespace PeerPayBackend.Controllers
{
    public class StudentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
