namespace schedule_2.Models
{
    public class CourseGroup
    {
        public int CourseId { get; set; }
        public int GroupId { get; set; }
        public virtual Course Course { get; set; } = null!;
        public virtual Group Group { get; set; } = null!;
    }
}