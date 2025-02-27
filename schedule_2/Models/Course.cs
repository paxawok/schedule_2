//namespace schedule_2.Models
//{
//    public class Course
//    {
//        public int Id { get; set; }
//        public string Name { get; set; }
//        public string Description { get; set; }
//    }
//}


namespace schedule_2.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        // Зв'язок Many-to-Many з викладачами
        public ICollection<TeacherCourse> TeacherCourses { get; set; } = new List<TeacherCourse>();

        // Зв'язок Many-to-Many з групами
        public ICollection<GroupCourse> GroupCourses { get; set; } = new List<GroupCourse>();
    }
}