namespace schedule_2.Models
{
    public class Schedule
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}