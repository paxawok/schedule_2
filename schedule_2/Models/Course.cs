namespace schedule_2.Models
{
    public class Course
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
        public virtual ICollection<CourseTeacher> CourseTeachers { get; set; } = new List<CourseTeacher>(); // M:M з Teacher
        public virtual ICollection<CourseGroup> CourseGroups { get; set; } = new List<CourseGroup>(); // M:M з Group
        public virtual ICollection<SubgroupCourse> SubgroupCourses { get; set; } = new List<SubgroupCourse>(); // M:M з Subgroup
    }
}