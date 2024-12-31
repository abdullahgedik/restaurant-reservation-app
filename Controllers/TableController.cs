using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantReservationSystem.Data;
using RestaurantReservationSystem.Models;

namespace RestaurantReservationSystem.Controllers
{
    [Authorize]
    public class TableController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public TableController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Create(int restaurantId)
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == restaurantId);

            if (restaurant == null)
            {
                return NotFound();
            }

            if (restaurant.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            ViewBag.RestaurantId = restaurantId;
            ViewBag.RestaurantName = restaurant.Name;
            return View(new Table { RestaurantId = restaurantId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RestaurantId,TableNumber,Capacity")] Table table)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var restaurant = await _context.Restaurants
                        .FirstOrDefaultAsync(r => r.Id == table.RestaurantId);

                    if (restaurant == null || restaurant.UserId != _userManager.GetUserId(User))
                    {
                        return Forbid();
                    }

                    var newTable = new Table
                    {
                        RestaurantId = table.RestaurantId,
                        TableNumber = table.TableNumber,
                        Capacity = table.Capacity,
                        IsAvailable = true
                    };

                    _context.Tables.Add(newTable);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Details", "Restaurant", new { id = table.RestaurantId });
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Masa eklenirken bir hata oluştu: " + ex.Message);
            }

            ViewBag.RestaurantId = table.RestaurantId;
            var currentRestaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == table.RestaurantId);
            ViewBag.RestaurantName = currentRestaurant?.Name;
            return View(table);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var table = await _context.Tables
                .Include(t => t.Restaurant)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (table == null)
            {
                return NotFound();
            }

            // Restoran sahibi kontrolü
            if (table.Restaurant.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            ViewBag.RestaurantName = table.Restaurant.Name;
            return View(table);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RestaurantId,TableNumber,Capacity,IsAvailable")] Table table)
        {
            if (id != table.Id)
            {
                return NotFound();
            }

            try
            {
                var restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.Id == table.RestaurantId);

                if (restaurant == null || restaurant.UserId != _userManager.GetUserId(User))
                {
                    return Forbid();
                }

                var existingTable = await _context.Tables
                    .Include(t => t.Restaurant)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (existingTable == null)
                {
                    return NotFound();
                }

                // Değerleri güncelle
                existingTable.TableNumber = table.TableNumber;
                existingTable.Capacity = table.Capacity;
                existingTable.IsAvailable = table.IsAvailable;

                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Restaurant", new { id = table.RestaurantId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Masa güncellenirken bir hata oluştu: " + ex.Message);
            }

            ViewBag.RestaurantName = (await _context.Restaurants.FindAsync(table.RestaurantId))?.Name;
            return View(table);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var table = await _context.Tables
                .Include(t => t.Restaurant)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (table == null)
            {
                return NotFound();
            }

            if (table.Restaurant.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            ViewBag.RestaurantName = table.Restaurant.Name;
            return View(table);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var table = await _context.Tables
                .Include(t => t.Restaurant)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (table == null)
            {
                return NotFound();
            }

            if (table.Restaurant.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            try
            {
                _context.Tables.Remove(table);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Restaurant", new { id = table.RestaurantId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Masa silinirken bir hata oluştu: " + ex.Message);
                return View(table);
            }
        }
    }
}