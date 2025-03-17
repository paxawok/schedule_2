namespace schedule_2.Models
{
    public class SubgroupCourse
    {
        public int SubgroupId { get; set; }
        public int CourseId { get; set; }
        public virtual Subgroup Subgroup { get; set; } = null!;
        public virtual Course Course { get; set; } = null!;
    }
}