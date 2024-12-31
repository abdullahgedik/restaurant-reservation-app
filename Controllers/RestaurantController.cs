using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantReservationSystem.Data;
using RestaurantReservationSystem.Models;

namespace RestaurantReservationSystem.Controllers
{
    public class RestaurantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RestaurantController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Restaurant/Search
        public async Task<IActionResult> Search(string searchTerm, string cuisine, int? minRating)
        {
            var query = _context.Restaurants
                .Include(r => r.Tables)
                .Include(r => r.Reviews)
                .Include(r => r.User)
                .AsQueryable();

            // İsme göre arama
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r => r.Name.Contains(searchTerm) ||
                                        r.Description.Contains(searchTerm) ||
                                        r.Address.Contains(searchTerm));
            }

            // Mutfak türüne göre filtreleme
            if (!string.IsNullOrEmpty(cuisine))
            {
                query = query.Where(r => r.Cuisine == cuisine);
            }

            // Minimum puana göre filtreleme
            if (minRating.HasValue)
            {
                query = query.Where(r => r.Rating >= minRating.Value);
            }

            // Sonuçları puana göre sırala
            var restaurants = await query.OrderByDescending(r => r.Rating)
                                        .ToListAsync();

            // Mevcut mutfak türlerini al
            var cuisines = await _context.Restaurants
                                        .Select(r => r.Cuisine)
                                        .Distinct()
                                        .OrderBy(c => c)
                                        .ToListAsync();

            ViewBag.Cuisines = cuisines;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedCuisine = cuisine;
            ViewBag.MinRating = minRating;

            return View("Index", restaurants);
        }

        public async Task<IActionResult> Index()
        {
            var restaurants = await _context.Restaurants
                .Include(r => r.Tables)
                .Include(r => r.User)
                .OrderByDescending(r => r.Rating)
                .ToListAsync();
            return View(restaurants);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var restaurant = await _context.Restaurants
                .Include(r => r.Tables)
                .Include(r => r.Reviews)
                    .ThenInclude(r => r.User)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (restaurant == null)
            {
                return NotFound();
            }

            // Masaları sırala
            if (restaurant.Tables != null)
            {
                restaurant.Tables = restaurant.Tables.OrderBy(t => t.TableNumber).ToList();
            }

            // Müsait masaları kontrol et
            ViewBag.HasAvailableTables = restaurant.Tables?.Any(t => t.IsAvailable) ?? false;

            return View(restaurant);
        }

        [Authorize]
        public IActionResult Create()
        {
            try
            {
                return View(new Restaurant());
            }
            catch (Exception ex)
            {
                // Hata loglaması yapabilirsiniz
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Name,Address,Description,Cuisine,ContactNumber")] Restaurant restaurant)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var currentUserId = _userManager.GetUserId(User);
                    if (currentUserId == null)
                    {
                        ModelState.AddModelError(string.Empty, "Kullanıcı girişi gereklidir.");
                        return View(restaurant);
                    }

                    restaurant.UserId = currentUserId;
                    restaurant.Rating = 0;
                    restaurant.Tables = new List<Table>();
                    restaurant.Reservations = new List<Reservation>();
                    restaurant.Reviews = new List<Review>();

                    _context.Add(restaurant);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }

                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine(error.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Restoran eklenirken bir hata oluştu: " + ex.Message);
            }

            return View(restaurant);
        }

        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var restaurant = await _context.Restaurants
                .Include(r => r.Tables)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurant == null)
            {
                return NotFound();
            }

            if (restaurant.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            return View(restaurant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Address,Description,Cuisine,ContactNumber,UserId,Rating")] Restaurant restaurant)
        {
            if (id != restaurant.Id)
            {
                return NotFound();
            }

            if (restaurant.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingRestaurant = await _context.Restaurants
                        .Include(r => r.Tables)
                        .Include(r => r.Reviews)
                        .Include(r => r.Reservations)
                        .FirstOrDefaultAsync(r => r.Id == id);

                    if (existingRestaurant == null)
                    {
                        return NotFound();
                    }

                    existingRestaurant.Name = restaurant.Name;
                    existingRestaurant.Address = restaurant.Address;
                    existingRestaurant.Description = restaurant.Description;
                    existingRestaurant.Cuisine = restaurant.Cuisine;
                    existingRestaurant.ContactNumber = restaurant.ContactNumber;

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Details), new { id = restaurant.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RestaurantExists(restaurant.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(restaurant);
        }

        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var restaurant = await _context.Restaurants
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (restaurant == null)
            {
                return NotFound();
            }

            if (restaurant.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            return View(restaurant);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var restaurant = await _context.Restaurants
                .Include(r => r.Tables)
                .Include(r => r.Reviews)
                .Include(r => r.Reservations)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurant == null)
            {
                return NotFound();
            }

            if (restaurant.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            _context.Tables.RemoveRange(restaurant.Tables);
            _context.Reviews.RemoveRange(restaurant.Reviews);
            _context.Reservations.RemoveRange(restaurant.Reservations);

            _context.Restaurants.Remove(restaurant);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RestaurantExists(int id)
        {
            return _context.Restaurants.Any(e => e.Id == id);
        }
    }
}