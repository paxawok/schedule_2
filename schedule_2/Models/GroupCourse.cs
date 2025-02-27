using System.Text.RegularExpressions;

namespace schedule_2.Models
{
    // ������� ������� Many-to-Many �� Group � Course
    public class GroupCourse
    {
        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
    }
}