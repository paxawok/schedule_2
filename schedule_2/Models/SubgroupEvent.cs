namespace schedule_2.Models
{
    public class SubgroupEvent
    {
        public int SubgroupId { get; set; }
        public int EventId { get; set; }
        public virtual Subgroup Subgroup { get; set; } = null!;
        public virtual Event Event { get; set; } = null!;
    }
}