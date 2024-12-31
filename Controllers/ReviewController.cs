using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantReservationSystem.Data;
using RestaurantReservationSystem.Models;

namespace RestaurantReservationSystem.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReviewController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Review/Create/5 (restaurantId)
        public async Task<IActionResult> Create(int restaurantId)
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == restaurantId);

            if (restaurant == null)
            {
                return NotFound();
            }

            // Kullanıcının bu restoranda rezervasyonu olup olmadığını kontrol et
            var userId = _userManager.GetUserId(User);
            var hasReservation = await _context.Reservations
                .AnyAsync(r => r.RestaurantId == restaurantId &&
                              r.UserId == userId &&
                              r.Status != ReservationStatus.Cancelled);

            if (!hasReservation)
            {
                TempData["Error"] = "Sadece rezervasyon yaptığınız restoranları değerlendirebilirsiniz.";
                return RedirectToAction("Details", "Restaurant", new { id = restaurantId });
            }

            // Kullanıcının daha önce değerlendirme yapıp yapmadığını kontrol et
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.RestaurantId == restaurantId && r.UserId == userId);

            if (existingReview != null)
            {
                TempData["Error"] = "Bu restoran için zaten bir değerlendirme yapmışsınız.";
                return RedirectToAction("Details", "Restaurant", new { id = restaurantId });
            }

            ViewBag.RestaurantName = restaurant.Name;
            return View(new Review { RestaurantId = restaurantId });
        }

        // POST: Review/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RestaurantId,Rating,Comment")] Review review)
        {
            // ModelState'i temizle çünkü UserId'yi manuel olarak ekleyeceğiz
            ModelState.Clear();

            try
            {
                // Kullanıcı ID'sini al ve ata
                var userId = _userManager.GetUserId(User);
                review.UserId = userId;
                review.ReviewDate = DateTime.Now;

                // Rezervasyon kontrolü
                var hasReservation = await _context.Reservations
                    .AnyAsync(r => r.RestaurantId == review.RestaurantId &&
                                  r.UserId == userId &&
                                  r.Status != ReservationStatus.Cancelled);

                if (!hasReservation)
                {
                    TempData["Error"] = "Sadece rezervasyon yaptığınız restoranları değerlendirebilirsiniz.";
                    return RedirectToAction("Details", "Restaurant", new { id = review.RestaurantId });
                }

                // Önceki değerlendirme kontrolü
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.RestaurantId == review.RestaurantId && r.UserId == userId);

                if (existingReview != null)
                {
                    TempData["Error"] = "Bu restoran için zaten bir değerlendirme yapmışsınız.";
                    return RedirectToAction("Details", "Restaurant", new { id = review.RestaurantId });
                }

                // Manuel validasyon
                if (review.Rating < 1 || review.Rating > 5)
                {
                    ModelState.AddModelError("Rating", "Puan 1-5 arasında olmalıdır.");
                }

                if (string.IsNullOrEmpty(review.Comment))
                {
                    ModelState.AddModelError("Comment", "Lütfen bir yorum yazın.");
                }

                if (ModelState.IsValid)
                {
                    _context.Reviews.Add(review);
                    await _context.SaveChangesAsync();

                    // Restoran puanını güncelle
                    var restaurant = await _context.Restaurants
                        .Include(r => r.Reviews)
                        .FirstOrDefaultAsync(r => r.Id == review.RestaurantId);

                    if (restaurant != null)
                    {
                        restaurant.Rating = (decimal)restaurant.Reviews.Average(r => r.Rating);
                        await _context.SaveChangesAsync();
                    }

                    TempData["Success"] = "Değerlendirmeniz başarıyla kaydedildi.";
                    return RedirectToAction("Details", "Restaurant", new { id = review.RestaurantId });
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Değerlendirme kaydedilirken bir hata oluştu: " + ex.Message);
            }

            // Hata durumunda view verilerini yeniden yükle
            var currentRestaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == review.RestaurantId);
            ViewBag.RestaurantName = currentRestaurant?.Name;
            return View(review);
        }

        // GET: Review/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Restaurant)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
            {
                return NotFound();
            }

            // Sadece kendi yorumunu düzenleyebilir
            if (review.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            ViewBag.RestaurantName = review.Restaurant.Name;
            return View(review);
        }

        // POST: Review/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RestaurantId,Rating,Comment")] Review review)
        {
            if (id != review.Id)
            {
                return NotFound();
            }

            // Mevcut yorumu getir
            var existingReview = await _context.Reviews
                .Include(r => r.Restaurant)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (existingReview == null)
            {
                return NotFound();
            }

            // Yetki kontrolü
            if (existingReview.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Sadece değiştirilebilir alanları güncelle
                    existingReview.Rating = review.Rating;
                    existingReview.Comment = review.Comment;
                    existingReview.ReviewDate = DateTime.Now;

                    await _context.SaveChangesAsync();

                    // Restoran puanını güncelle
                    var restaurant = await _context.Restaurants
                        .Include(r => r.Reviews)
                        .FirstOrDefaultAsync(r => r.Id == existingReview.RestaurantId);

                    if (restaurant != null)
                    {
                        restaurant.Rating = (decimal)restaurant.Reviews.Average(r => r.Rating);
                        await _context.SaveChangesAsync();
                    }

                    TempData["Success"] = "Değerlendirmeniz başarıyla güncellendi.";
                    return RedirectToAction("Details", "Restaurant", new { id = existingReview.RestaurantId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReviewExists(review.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.RestaurantName = existingReview.Restaurant.Name;
            return View(review);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Restaurant)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
            {
                return NotFound();
            }

            // Sadece kendi yorumunu silebilir
            if (review.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            return View(review);
        }

        // POST: Review/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _context.Reviews
                .Include(r => r.Restaurant)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
            {
                return NotFound();
            }

            // Yetki kontrolü
            if (review.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            var restaurantId = review.RestaurantId;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            // Restoran puanını güncelle
            var restaurant = await _context.Restaurants
                .Include(r => r.Reviews)
                .FirstOrDefaultAsync(r => r.Id == restaurantId);

            if (restaurant != null && restaurant.Reviews.Any())
            {
                restaurant.Rating = (decimal)restaurant.Reviews.Average(r => r.Rating);
            }
            else
            {
                restaurant.Rating = 0;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Değerlendirmeniz başarıyla silindi.";
            return RedirectToAction("Details", "Restaurant", new { id = restaurantId });
        }

        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(e => e.Id == id);
        }
    }
}