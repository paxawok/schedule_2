using System.ComponentModel.DataAnnotations;

namespace schedule_2.Models
{
    public class Classroom
    {
        public int Id { get; set; }

        public required string Name { get; set; }
        [Range(10, 120, ErrorMessage = "Capacity should be between 10 and 120 seats")]
        public int Capacity { get; set; }
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}