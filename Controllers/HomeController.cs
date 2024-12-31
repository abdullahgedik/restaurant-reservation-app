using Microsoft.AspNetCore.Mvc;

namespace RestaurantReservationSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Restaurant");
        }

        [Route("Error")]
        public IActionResult Error()
        {
            return View();
        }
    }
}