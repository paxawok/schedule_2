using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace schedule_2.Controllers
{
    [Authorize] // Дозволити доступ тільки авторизованим користувачам
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CourseController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Метод для перевірки, чи є користувач адміністратором
        private async Task<bool> IsAdministratorAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return false;
            return await _userManager.IsInRoleAsync(user, "Administrator");
        }

        // Метод для отримання ID викладача, пов'язаного з поточним користувачем
        private async Task<int?> GetCurrentTeacherIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserId == userId);

            return teacher?.Id;
        }

        // Метод для перевірки, чи є поточний користувач викладачем даного курсу
        private async Task<bool> IsTeacherOfCourseAsync(int courseId)
        {
            // Якщо адміністратор, повертаємо true одразу
            if (await IsAdministratorAsync()) return true;

            var teacherId = await GetCurrentTeacherIdAsync();
            if (!teacherId.HasValue) return false;

            return await _context.CourseTeachers
                .AnyAsync(ct => ct.CourseId == courseId && ct.TeacherId == teacherId.Value);
        }

        // GET: /Course/Index
        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                .Include(c => c.Events)
                .Include(c => c.CourseTeachers)
                    .ThenInclude(ct => ct.Teacher)
                .Include(c => c.CourseGroups)
                    .ThenInclude(cg => cg.Group)
                .Include(c => c.SubgroupCourses)
                    .ThenInclude(sg => sg.Subgroup)
                .ToListAsync();

            // Перевірка на null і ініціалізація колекцій, якщо вони порожні
            foreach (var course in courses)
            {
                course.CourseGroups ??= new List<CourseGroup>();
                course.CourseTeachers ??= new List<CourseTeacher>();
                course.SubgroupCourses ??= new List<SubgroupCourse>();
            }

            // Передаємо додаткові дані для перевірки прав у View
            ViewBag.IsAdministrator = await IsAdministratorAsync();
            ViewBag.TeacherId = await GetCurrentTeacherIdAsync();

            // Перевіряємо чи користувач у ролі Teacher
            var user = await _userManager.GetUserAsync(User);
            ViewBag.IsTeacher = user != null && await _userManager.IsInRoleAsync(user, "Teacher");

            return View(courses);
        }

        // GET: /Course/Details/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> DetailsModal(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Events)
                .Include(c => c.CourseTeachers)
                    .ThenInclude(ct => ct.Teacher)
                .Include(c => c.CourseGroups)
                    .ThenInclude(cg => cg.Group)
                .Include(c => c.SubgroupCourses)
                    .ThenInclude(sg => sg.Subgroup)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
                return NotFound();

            // Передаємо додаткові дані для перевірки прав у View
            ViewBag.IsAdministrator = await IsAdministratorAsync();
            ViewBag.IsTeacherOfCourse = await IsTeacherOfCourseAsync(id);

            return PartialView("_DetailsModal", course);
        }

        // GET: /Course/Create
        [HttpGet]
        [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть створювати курси
        public IActionResult Create()
        {
            ViewData["Teachers"] = _context.Teachers.ToList();
            ViewData["Groups"] = _context.Groups.ToList();
            ViewData["Subgroups"] = _context.Subgroups.ToList();

            return PartialView("_CreateModal");
        }

        // POST: /Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть створювати курси
        public async Task<IActionResult> Create(Course course, int[] courseTeachers, int[] courseGroups, int[] subgroupCourses)
        {
            if (ModelState.IsValid)
            {
                // Призначення викладачів, якщо є
                if (courseTeachers != null && courseTeachers.Length > 0)
                {
                    course.CourseTeachers = new List<CourseTeacher>();
                    foreach (var teacherId in courseTeachers)
                    {
                        var teacher = await _context.Teachers.FindAsync(teacherId);
                        if (teacher != null)
                        {
                            course.CourseTeachers.Add(new CourseTeacher { Teacher = teacher });
                        }
                    }
                }

                // Призначення груп, якщо є
                if (courseGroups != null && courseGroups.Length > 0)
                {
                    course.CourseGroups = new List<CourseGroup>();
                    foreach (var groupId in courseGroups)
                    {
                        var group = await _context.Groups.FindAsync(groupId);
                        if (group != null)
                        {
                            course.CourseGroups.Add(new CourseGroup { Group = group });
                        }
                    }
                }

                // Призначення підгруп, якщо є
                if (subgroupCourses != null && subgroupCourses.Length > 0)
                {
                    course.SubgroupCourses = new List<SubgroupCourse>();
                    foreach (var subgroupId in subgroupCourses)
                    {
                        var subgroup = await _context.Subgroups.FindAsync(subgroupId);
                        if (subgroup != null)
                        {
                            course.SubgroupCourses.Add(new SubgroupCourse { Subgroup = subgroup });
                        }
                    }
                }

                // Додати курс в базу
                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Курс успішно створено!" });
            }

            return Json(new { success = false, message = "Невірні дані форми." });
        }

        // GET: /Course/Edit/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            var course = await _context.Courses
                .Include(c => c.CourseTeachers)
                .ThenInclude(ct => ct.Teacher)
                .Include(c => c.CourseGroups)
                .ThenInclude(cg => cg.Group)
                .Include(c => c.SubgroupCourses)
                .ThenInclude(sc => sc.Subgroup)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFound();

            // Перевірка прав доступу
            bool isAdmin = await IsAdministratorAsync();
            bool isTeacherOfCourse = await IsTeacherOfCourseAsync(id);

            if (!isAdmin && !isTeacherOfCourse)
                return Forbid(); // Повернути 403 Forbidden, якщо немає прав

            // Повертаємо до виду необхідні дані
            ViewData["Teachers"] = await _context.Teachers.ToListAsync();
            ViewData["Groups"] = await _context.Groups.ToListAsync();
            ViewData["Subgroups"] = await _context.Subgroups.ToListAsync();
            ViewBag.IsAdministrator = isAdmin;

            return PartialView("_EditModal", course);
        }

        // POST: /Course/Edit/{id} (AJAX для оновлення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModal(int id, Course course, int[] courseTeachers, int[] courseGroups, int[] subgroupCourses)
        {
            if (id != course.Id)
                return Json(new { success = false, message = "ID курсу не співпадає." });

            // Перевірка прав доступу
            bool isAdmin = await IsAdministratorAsync();
            bool isTeacherOfCourse = await IsTeacherOfCourseAsync(id);

            if (!isAdmin && !isTeacherOfCourse)
                return Json(new { success = false, message = "У вас немає прав для редагування цього курсу." });

            if (ModelState.IsValid)
            {
                var courseInDb = await _context.Courses
                    .Include(c => c.CourseTeachers)
                        .ThenInclude(ct => ct.Teacher)
                    .Include(c => c.CourseGroups)
                        .ThenInclude(cg => cg.Group)
                    .Include(c => c.SubgroupCourses)
                        .ThenInclude(sc => sc.Subgroup)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (courseInDb == null)
                    return Json(new { success = false, message = "Курс не знайдено." });

                // Базові поля (доступні для редагування всім з правами редагування)
                courseInDb.Name = course.Name;
                courseInDb.Description = course.Description;

                // Якщо користувач адміністратор, він може змінювати викладачів, групи та підгрупи
                if (isAdmin)
                {
                    // Оновлення викладачів
                    courseInDb.CourseTeachers.Clear();
                    if (courseTeachers != null && courseTeachers.Length > 0)
                    {
                        foreach (var teacherId in courseTeachers)
                        {
                            var teacher = await _context.Teachers.FindAsync(teacherId);
                            if (teacher != null)
                            {
                                courseInDb.CourseTeachers.Add(new CourseTeacher { TeacherId = teacherId, CourseId = id });
                            }
                        }
                    }

                    // Оновлення груп
                    courseInDb.CourseGroups.Clear();
                    if (courseGroups != null && courseGroups.Length > 0)
                    {
                        foreach (var groupId in courseGroups)
                        {
                            var group = await _context.Groups.FindAsync(groupId);
                            if (group != null)
                            {
                                courseInDb.CourseGroups.Add(new CourseGroup { GroupId = groupId, CourseId = id });
                            }
                        }
                    }

                    // Оновлення підгруп
                    courseInDb.SubgroupCourses.Clear();
                    if (subgroupCourses != null && subgroupCourses.Length > 0)
                    {
                        foreach (var subgroupId in subgroupCourses)
                        {
                            var subgroup = await _context.Subgroups.FindAsync(subgroupId);
                            if (subgroup != null)
                            {
                                courseInDb.SubgroupCourses.Add(new SubgroupCourse { SubgroupId = subgroupId, CourseId = id });
                            }
                        }
                    }
                }

                // Збереження змін
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Курс успішно оновлено." });
            }

            return Json(new { success = false, message = "Невірні дані форми." });
        }

        // GET: /Course/Delete/{id} (Partial View для модального вікна підтвердження)
        [HttpGet]
        [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть видаляти курси
        public async Task<IActionResult> DeleteModal(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Events)
                .Include(c => c.CourseTeachers)
                .Include(c => c.CourseGroups)
                .Include(c => c.SubgroupCourses)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
                return NotFound();

            return PartialView("_DeleteModal", course);
        }

        // POST: /Course/DeleteConfirmed/{id} (AJAX для видалення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть видаляти курси
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Events)
                .Include(c => c.CourseTeachers)
                .Include(c => c.CourseGroups)
                .Include(c => c.SubgroupCourses)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
                return Json(new { success = false });

            foreach (var eventItem in course.Events.ToList())
            {
                _context.Events.Remove(eventItem);
            }

            foreach (var courseTeacher in course.CourseTeachers.ToList())
            {
                _context.CourseTeachers.Remove(courseTeacher);
            }

            foreach (var courseGroup in course.CourseGroups.ToList())
            {
                _context.CourseGroups.Remove(courseGroup);
            }

            foreach (var subgroupCourse in course.SubgroupCourses.ToList())
            {
                _context.SubgroupCourses.Remove(subgroupCourse);
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Курс успішно видалений." });
        }
    }
}