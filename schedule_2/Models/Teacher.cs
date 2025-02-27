//namespace schedule_2.Models
//{
//    public class Teacher
//    {
//        public int Id { get; set; }
//        public string FirstName { get; set; }
//        public string LastName { get; set; }
//        public string Email { get; set; }
//        public List<int> CourseIds { get; set; }
//    }
//}



namespace schedule_2.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Зв'язок Many-to-Many з курсами
        public ICollection<TeacherCourse> TeacherCourses { get; set; } = new List<TeacherCourse>();

        // Зв'язок One-to-Many з розкладом
        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }
}