using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace RestaurantReservationSystem.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int RestaurantId { get; set; }

        [Required(ErrorMessage = "Lütfen bir puan verin.")]
        [Range(1, 5, ErrorMessage = "Puan 1-5 arasında olmalıdır.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Lütfen bir yorum yazın.")]
        [StringLength(500, ErrorMessage = "Yorum en fazla 500 karakter olabilir.")]
        public string Comment { get; set; }

        public DateTime ReviewDate { get; set; }

        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }

        [ForeignKey("RestaurantId")]
        public virtual Restaurant Restaurant { get; set; }
    }
}