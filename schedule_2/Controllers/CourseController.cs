using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using System.Linq;
using System.Threading.Tasks;

namespace schedule_2.Controllers
{
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourseController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Course/Index
        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                .Include(c => c.Events)
                .Include(c => c.CourseTeachers)
                .Include(c => c.CourseGroups)
                .Include(c => c.SubgroupCourses)
                .ToListAsync();
            return View(courses);
        }

        // GET: /Course/Details/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> DetailsModal(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Events)
                .Include(c => c.CourseTeachers)
                .Include(c => c.CourseGroups)
                .Include(c => c.SubgroupCourses)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
                return NotFound();

            return PartialView("_DetailsModal", course);
        }

        // GET: /Course/Create
        [HttpGet]
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
        public async Task<IActionResult> Create(Course course, int[] courseTeachers, int[] courseGroups, int[] subgroupCourses)
        {
            if (ModelState.IsValid)
            {
                // Призначення викладачів, якщо є
                if (courseTeachers != null && courseTeachers.Length > 0)
                {
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

            // Повертаємо до виду необхідні дані
            ViewData["Teachers"] = await _context.Teachers.ToListAsync();
            ViewData["Groups"] = await _context.Groups.ToListAsync();
            ViewData["Subgroups"] = await _context.Subgroups.ToListAsync();

            return PartialView("_EditModal", course);
        }


        // POST: /Course/Edit/{id} (AJAX для оновлення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModal(int id, Course course, int[] courseTeachers, int[] courseGroups, int[] subgroupCourses)
        {
            if (id != course.Id)
                return Json(new { success = false, message = "ID курсу не співпадає." });

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

                // Оновлення основних даних курсу
                courseInDb.Name = course.Name;
                courseInDb.Description = course.Description;

                // Оновлення викладачів
                courseInDb.CourseTeachers.Clear();
                if (courseTeachers != null && courseTeachers.Length > 0)
                {
                    foreach (var teacherId in courseTeachers)
                    {
                        var teacher = await _context.Teachers.FindAsync(teacherId);
                        if (teacher != null)
                        {
                            courseInDb.CourseTeachers.Add(new CourseTeacher { Teacher = teacher });
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
                            courseInDb.CourseGroups.Add(new CourseGroup { Group = group });
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
                            courseInDb.SubgroupCourses.Add(new SubgroupCourse { Subgroup = subgroup });
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
