namespace schedule_2.Models
{
    // Проміжна таблиця Many-to-Many між Teacher і Course
    public class TeacherCourse
    {
        public int TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;

        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
    }
}