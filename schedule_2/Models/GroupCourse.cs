using System.Text.RegularExpressions;

namespace schedule_2.Models
{
    // Проміжна таблиця Many-to-Many між Group і Course
    public class GroupCourse
    {
        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
    }
}