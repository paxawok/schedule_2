namespace schedule_2.Models
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CourseId { get; set; }
        public List<int> StudentIds { get; set; }
    }
}
