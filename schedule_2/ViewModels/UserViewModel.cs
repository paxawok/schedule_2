using System.Collections.Generic;

namespace schedule_2.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public bool IsTeacher { get; set; }
        public int? TeacherId { get; set; }
    }
}