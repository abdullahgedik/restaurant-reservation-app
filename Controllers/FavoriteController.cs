using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantReservationSystem.Data;
using RestaurantReservationSystem.Models;

namespace RestaurantReservationSystem.Controllers
{
    [Authorize]
    public class FavoriteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public FavoriteController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var favorites = await _context.Favorites
                .Include(f => f.Restaurant)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(favorites);
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int restaurantId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            var userId = _userManager.GetUserId(User);
            var existing = await _context.Favorites
                .FirstOrDefaultAsync(f => f.RestaurantId == restaurantId && f.UserId == userId);

            if (existing != null)
            {
                _context.Favorites.Remove(existing);
                await _context.SaveChangesAsync();
                return Json(new { isFavorite = false });
            }
            else
            {
                var favorite = new Favorite
                {
                    UserId = userId,
                    RestaurantId = restaurantId,
                    CreatedAt = DateTime.Now
                };

                await _context.Favorites.AddAsync(favorite);
                await _context.SaveChangesAsync();
                return Json(new { isFavorite = true });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckStatus(int restaurantId)
        {
            var userId = _userManager.GetUserId(User);
            var isFavorite = await _context.Favorites
                .AnyAsync(f => f.RestaurantId == restaurantId && f.UserId == userId);

            return Json(new { isFavorite });
        }
    }
}