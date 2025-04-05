using Microsoft.AspNetCore.Mvc;

namespace schedule_2.wwwroot
{
    public class LoginRedirectMiddleware : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
