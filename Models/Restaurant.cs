using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RestaurantReservationSystem.Models
{
    public class Restaurant
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Restoran adı gereklidir.")]
        [StringLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Adres gereklidir.")]
        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Mutfak türü gereklidir.")]
        [StringLength(50)]
        public string Cuisine { get; set; }

        [Required(ErrorMessage = "İletişim numarası gereklidir.")]
        [StringLength(20)]
        public string ContactNumber { get; set; }

        public decimal Rating { get; set; }

        // Foreign key için nullable yapıyoruz
        public string? UserId { get; set; }

        // Navigation property
        public virtual IdentityUser? User { get; set; }

        public virtual ICollection<Table> Tables { get; set; }
        public virtual ICollection<Reservation> Reservations { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }

        public Restaurant()
        {
            Tables = new HashSet<Table>();
            Reservations = new HashSet<Reservation>();
            Reviews = new HashSet<Review>();
            Rating = 0;
        }
    }
}