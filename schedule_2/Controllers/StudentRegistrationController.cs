using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using schedule_2.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace schedule_2.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class StudentRegistrationController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public StudentRegistrationController(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: /StudentRegistration/Register
        public IActionResult Register()
        {
            var groups = _context.Groups.ToList();
            ViewBag.Groups = new SelectList(groups, "Id", "Name");
            return View();
        }

        // POST: /StudentRegistration/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(StudentRegistrationViewModel model)
        {
            var groups = _context.Groups.ToList();
            ViewBag.Groups = new SelectList(groups, "Id", "Name");

            if (ModelState.IsValid)
            {
                // Перевірка існування групи
                var group = await _context.Groups.FindAsync(model.GroupId);
                if (group == null)
                {
                    ModelState.AddModelError("GroupId", "Вибрана група не існує");
                    return View(model);
                }

                // Перевірка чи вже існує студент з таким email
                var studentExists = await _context.Set<Student>().AnyAsync(s => s.Email == model.Email);
                if (studentExists)
                {
                    ModelState.AddModelError("Email", "Студент з такою електронною адресою вже існує");
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
                    // Призначаємо роль "Student"
                    await _userManager.AddToRoleAsync(user, "Student");

                    // Створюємо запис студента
                    var student = new Student
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        UserId = user.Id,
                        GroupId = model.GroupId
                    };

                    _context.Add(student);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Index", "StudentManagement");
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