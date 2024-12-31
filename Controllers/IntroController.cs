using Microsoft.AspNetCore.Mvc;

namespace RestaurantReservationSystem.Controllers
{
    public class IntroController : Controller
    {
        public IActionResult Splash()
        {
            // Splash sayfasını 3 saniye göster, sonra Onboarding'e yönlendir
            return View();
        }

        public IActionResult Onboarding()
        {
            // İlk defa gelen kullanıcılar için onboarding sayfası
            return View();
        }
    }
}