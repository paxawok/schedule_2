using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using System.Threading.Tasks;

namespace schedule_2.Controllers
{
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TeacherController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Teacher/Index
        public async Task<IActionResult> Index()
        {
            var teachers = await _context.Teachers
                .Include(t => t.CourseTeachers)
                    .ThenInclude(ct => ct.Course)
                .Include(t => t.Events)
                .ToListAsync();
            return View(teachers);
        }

        // GET: /Teacher/Details/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> DetailsModal(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.CourseTeachers)
                    .ThenInclude(ct => ct.Course)
                .Include(t => t.Events)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (teacher == null)
                return NotFound();

            return PartialView("_DetailsModal", teacher);
        }

        // GET: /Teacher/Edit/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.CourseTeachers)
                    .ThenInclude(ct => ct.Course)
                .Include(t => t.Events)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (teacher == null)
                return NotFound();

            ViewBag.Courses = new MultiSelectList(_context.Courses, "Id", "Name", teacher.CourseTeachers.Select(ct => ct.CourseId));
            return PartialView("_EditModal", teacher);
        }

        // POST: /Teacher/Edit/{id} (AJAX для оновлення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModal(int id, Teacher teacher, int[] selectedCourses)
        {
            if (id != teacher.Id)
                return Json(new { success = false, message = "ID не співпадає." });

            if (ModelState.IsValid)
            {
                try
                {
                    var teacherInDb = await _context.Teachers
                        .Include(t => t.CourseTeachers)
                        .Include(t => t.Events)
                        .FirstOrDefaultAsync(t => t.Id == id);

                    if (teacherInDb == null)
                        return Json(new { success = false, message = "Викладач не знайдений." });

                    // Оновлюємо основні поля викладача
                    teacherInDb.FirstName = teacher.FirstName;
                    teacherInDb.LastName = teacher.LastName;
                    teacherInDb.Email = teacher.Email;

                    // Очищаємо попередні курси
                    teacherInDb.CourseTeachers.Clear();

                    // Додаємо нові курси через CourseTeacher
                    if (selectedCourses != null)
                    {
                        foreach (var courseId in selectedCourses)
                        {
                            teacherInDb.CourseTeachers.Add(new CourseTeacher { TeacherId = teacher.Id, CourseId = courseId });
                        }
                    }

                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Дані успішно оновлено." });
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Json(new { success = false, message = "Помилка оновлення даних." });
                }
            }
            ViewBag.Courses = new MultiSelectList(_context.Courses, "Id", "Name", teacher.CourseTeachers.Select(ct => ct.CourseId));
            return PartialView("_EditModal", teacher);
        }

        // GET: /Teacher/Delete/{id} (Partial View для модального вікна підтвердження)
        [HttpGet]
        public async Task<IActionResult> DeleteModal(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.CourseTeachers)
                    .ThenInclude(ct => ct.Course)
                .Include(t => t.Events)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (teacher == null)
                return NotFound();

            return PartialView("_DeleteModal", teacher);
        }

        // POST: /Teacher/DeleteConfirmed/{id} (AJAX для видалення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.CourseTeachers)
                    .ThenInclude(ct => ct.Course)
                .Include(t => t.Events)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (teacher != null)
            {
                _context.Teachers.Remove(teacher);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Викладач успішно видалено." });
            }

            return Json(new { success = false, message = "Викладач не знайдено." });
        }

        // Допоміжний метод для перевірки існування викладача
        private bool TeacherExists(int id)
        {
            return _context.Teachers.Any(e => e.Id == id);
        }
    }
}