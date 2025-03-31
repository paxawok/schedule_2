using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using schedule_2.ViewModels;
using System.Threading.Tasks;

namespace schedule_2.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class TeacherRegistrationController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public TeacherRegistrationController(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(TeacherRegistrationViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Перевірка чи вже існує викладач з таким email
                var teacherExists = await _context.Teachers.AnyAsync(t => t.Email == model.Email);
                if (teacherExists)
                {
                    ModelState.AddModelError("Email", "Викладач з такою електронною адресою вже існує");
                    return View(model);
                }

                // Створюємо користувача Identity
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true // Для спрощення, без підтвердження email
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Призначаємо роль "Teacher"
                    await _userManager.AddToRoleAsync(user, "Teacher");

                    // Створюємо запис викладача
                    var teacher = new Teacher
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        UserId = user.Id
                    };

                    _context.Teachers.Add(teacher);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Index", "Teacher");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
    }
}
