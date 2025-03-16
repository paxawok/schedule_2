namespace schedule_2.Models
{
    public class EventGroup
    {
        public int EventId { get; set; }
        public int GroupId { get; set; }
        public virtual Event Event { get; set; } = null!;
        public virtual Group Group { get; set; } = null!;
    }
}