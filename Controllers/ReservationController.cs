using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantReservationSystem.Data;
using RestaurantReservationSystem.Models;

namespace RestaurantReservationSystem.Controllers
{
    [Authorize]
    public class ReservationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReservationController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Reservation
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var reservations = await _context.Reservations
                .Include(r => r.Restaurant)
                .Include(r => r.Table)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            return View(reservations);
        }

        // GET: Reservation/Create
        public async Task<IActionResult> Create(int restaurantId)
        {
            try
            {
                var restaurant = await _context.Restaurants
                    .Include(r => r.Tables.Where(t => t.IsAvailable))
                    .FirstOrDefaultAsync(r => r.Id == restaurantId);

                if (restaurant == null)
                {
                    return NotFound();
                }

                ViewBag.RestaurantName = restaurant.Name;
                ViewBag.AvailableTables = restaurant.Tables.ToList();

                var reservation = new Reservation
                {
                    RestaurantId = restaurantId,
                    ReservationDate = DateTime.Now.AddHours(1),
                    UserId = _userManager.GetUserId(User)
                };

                return View(reservation);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index", "Restaurant");
            }
        }

        // POST: Reservation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RestaurantId,TableId,ReservationDate,GuestCount")] Reservation reservation)
        {
            try
            {
                // Model state'i temizle
                ModelState.Clear();

                // Kullanıcı ID'sini set et
                reservation.UserId = _userManager.GetUserId(User);
                reservation.Status = ReservationStatus.Pending;

                // Validasyonları kontrol et
                if (reservation.ReservationDate <= DateTime.Now)
                {
                    ModelState.AddModelError("ReservationDate", "Rezervasyon tarihi gelecekte olmalıdır.");
                }

                if (reservation.GuestCount <= 0 || reservation.GuestCount > 20)
                {
                    ModelState.AddModelError("GuestCount", "Kişi sayısı 1-20 arasında olmalıdır.");
                }

                // Masa kontrolü
                var table = await _context.Tables
                    .FirstOrDefaultAsync(t => t.Id == reservation.TableId);

                if (table == null)
                {
                    ModelState.AddModelError("TableId", "Lütfen bir masa seçin.");
                }
                else if (!table.IsAvailable)
                {
                    ModelState.AddModelError("TableId", "Seçilen masa müsait değil.");
                }
                else if (table.Capacity < reservation.GuestCount)
                {
                    ModelState.AddModelError("GuestCount", $"Seçilen masa en fazla {table.Capacity} kişiliktir.");
                }

                if (ModelState.IsValid && table != null)
                {
                    // Masayı rezerve et
                    table.IsAvailable = false;

                    // Rezervasyonu kaydet
                    _context.Reservations.Add(reservation);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Rezervasyonunuz başarıyla oluşturuldu.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Rezervasyon oluşturulurken bir hata oluştu: " + ex.Message);
            }

            // Hata durumunda view verilerini yeniden yükle
            var restaurant = await _context.Restaurants
                .Include(r => r.Tables.Where(t => t.IsAvailable))
                .FirstOrDefaultAsync(r => r.Id == reservation.RestaurantId);

            ViewBag.RestaurantName = restaurant?.Name;
            ViewBag.AvailableTables = restaurant?.Tables.ToList() ?? new List<Table>();

            return View(reservation);
        }

        // POST: Reservation/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            if (reservation.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            if (reservation.ReservationDate < DateTime.Now)
            {
                TempData["Error"] = "Geçmiş rezervasyonlar iptal edilemez.";
                return RedirectToAction(nameof(Index));
            }

            reservation.Status = ReservationStatus.Cancelled;
            reservation.Table.IsAvailable = true;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Rezervasyonunuz iptal edildi.";

            return RedirectToAction(nameof(Index));
        }
    }
}