//namespace schedule_2.Models
//{
//    public class Student
//    {
//        public int Id { get; set; }
//        public string FirstName { get; set; }
//        public string LastName { get; set; }
//        public string Email { get; set; }
//        public int GroupId { get; set; }
//    }
//}



namespace schedule_2.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Foreign Key
        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;
    }
}