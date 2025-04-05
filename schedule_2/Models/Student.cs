using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace schedule_2.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Ім'я")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Прізвище")]
        public string LastName { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            ErrorMessage = "Будь ласка, введіть коректну електронну адресу")]
        public string Email { get; set; }

        // Зв'язок з ідентифікацією
        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        // Зв'язок з групою
        public int GroupId { get; set; }
        public Group Group { get; set; }

        // Навігаційні властивості для отримання повного імені
        public string FullName => $"{LastName} {FirstName}";
    }
}