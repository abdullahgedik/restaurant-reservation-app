using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RestaurantReservationSystem.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int RestaurantId { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual IdentityUser User { get; set; }
        public virtual Restaurant Restaurant { get; set; }
    }
}