using System.Collections.Generic;

namespace schedule_2.ViewModels
{
    public class EditUserRolesViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public List<UserRolesViewModel> Roles { get; set; } = new List<UserRolesViewModel>();
    }
}