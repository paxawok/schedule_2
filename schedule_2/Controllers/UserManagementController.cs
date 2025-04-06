using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using schedule_2.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace schedule_2.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UserManagementController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
                var student = await _context.Set<Student>().FirstOrDefaultAsync(s => s.UserId == user.Id);

                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    Roles = roles.ToList(),
                    IsTeacher = teacher != null,
                    TeacherId = teacher?.Id,
                    IsStudent = student != null,
                    StudentId = student?.Id,
                    GroupName = student?.Group?.Name
                });
            }

            return View(userViewModels);
        }

        public async Task<IActionResult> EditRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditUserRolesViewModel
            {
                UserId = userId,
                UserName = user.UserName
            };

            foreach (var role in _roleManager.Roles)
            {
                var userRolesViewModel = new UserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    IsSelected = await _userManager.IsInRoleAsync(user, role.Name)
                };
                model.Roles.Add(userRolesViewModel);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditRoles(EditUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, roles);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Не вдалося видалити існуючі ролі");
                return View(model);
            }

            result = await _userManager.AddToRolesAsync(user,
                model.Roles.Where(x => x.IsSelected).Select(y => y.RoleName));

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Не вдалося додати вибрані ролі");
                return View(model);
            }

            return RedirectToAction("Index");
        }

        // GET: /UserManagement/CreateTeacher
        public IActionResult CreateTeacher()
        {
            return View();
        }

        // POST: /UserManagement/CreateTeacher
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher(TeacherRegistrationViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Перевірка чи вже існує користувач з таким email
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Користувач з таким email вже існує");
                    return View(model);
                }

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

                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: /UserManagement/CreateStudent
        public IActionResult CreateStudent()
        {
            var groups = _context.Groups.ToList();
            ViewBag.Groups = new SelectList(groups, "Id", "Name");
            return View();
        }

        // POST: /UserManagement/CreateStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent(StudentRegistrationViewModel model)
        {
            var groups = _context.Groups.ToList();
            ViewBag.Groups = new SelectList(groups, "Id", "Name", model.GroupId);

            if (ModelState.IsValid)
            {
                // Перевірка чи вже існує користувач з таким email
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Користувач з таким email вже існує");
                    return View(model);
                }

                // Перевірка чи вже існує студент з таким email
                var studentExists = await _context.Set<Student>().AnyAsync(s => s.Email == model.Email);
                if (studentExists)
                {
                    ModelState.AddModelError("Email", "Студент з такою електронною адресою вже існує");
                    return View(model);
                }

                // Перевірка існування групи
                var group = await _context.Groups.FindAsync(model.GroupId);
                if (group == null)
                {
                    ModelState.AddModelError("GroupId", "Вибрана група не існує");
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

                    return RedirectToAction("Index");
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