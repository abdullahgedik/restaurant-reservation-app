using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace RestaurantReservationSystem.Models
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }

        public int RestaurantId { get; set; }

        [Required(ErrorMessage = "Lütfen bir masa seçin.")]
        [Display(Name = "Masa")]
        public int TableId { get; set; }

        [Required(ErrorMessage = "Rezervasyon tarihi gereklidir.")]
        [Display(Name = "Rezervasyon Tarihi")]
        [DataType(DataType.DateTime)]
        [FutureDate(ErrorMessage = "Rezervasyon tarihi gelecekte olmalıdır.")]
        public DateTime ReservationDate { get; set; }

        [Required(ErrorMessage = "Kişi sayısı gereklidir.")]
        [Range(1, 20, ErrorMessage = "Kişi sayısı 1-20 arasında olmalıdır.")]
        [Display(Name = "Kişi Sayısı")]
        public int GuestCount { get; set; }

        public ReservationStatus Status { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }

        [ForeignKey("RestaurantId")]
        public virtual Restaurant Restaurant { get; set; }

        [ForeignKey("TableId")]
        public virtual Table Table { get; set; }
    }

    // Custom validation attribute for future date
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value is DateTime date)
            {
                return date > DateTime.Now;
            }
            return false;
        }
    }
}