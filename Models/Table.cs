using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantReservationSystem.Models
{
    public class Table
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Restoran ID gereklidir.")]
        public int RestaurantId { get; set; }

        [Required(ErrorMessage = "Masa numarası gereklidir.")]
        [Display(Name = "Masa Numarası")]
        [StringLength(10)]
        public string TableNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kapasite gereklidir.")]
        [Range(1, 20, ErrorMessage = "Kapasite 1-20 arasında olmalıdır.")]
        public int Capacity { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Navigation properties
        [ForeignKey("RestaurantId")]
        public virtual Restaurant? Restaurant { get; set; }
        public virtual ICollection<Reservation>? Reservations { get; set; }
    }
}