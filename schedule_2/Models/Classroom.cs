namespace schedule_2.Models
{
    public class Classroom
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int Capacity { get; set; }
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}