using System.ComponentModel.DataAnnotations;

namespace schedule_2.Models
{
    public class Subgroup
    {
        public int Id { get; set; }
        public required string Name { get; set; } // Назва підгрупи (наприклад, "А1-1", "А1-2")
        public int GroupId { get; set; } // Зовнішній ключ до Group
        public virtual Group Group { get; set; } = null!; // Навігаційна властивість до Group

        // Зв’язок M:M з Course (через нову проміжну таблицю SubgroupCourse)
        public virtual ICollection<SubgroupCourse> SubgroupCourses { get; set; } = new List<SubgroupCourse>();

        // Зв’язок M:M з Event (через нову проміжну таблицю SubgroupEvent)
        public virtual ICollection<SubgroupEvent> SubgroupEvents { get; set; } = new List<SubgroupEvent>();
    }
}