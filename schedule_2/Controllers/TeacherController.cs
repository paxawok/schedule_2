using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using System.Linq;
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
                .Include(t => t.Events)
                .Include(t => t.CourseTeachers) // Для отримання пов'язаних курсів
                .ThenInclude(ct => ct.Course)  // Якщо потрібно отримати інформацію про курси
                .ToListAsync();
            return View(teachers);
        }

        // GET: /Teacher/Details/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> DetailsModal(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.Events)
                .Include(t => t.CourseTeachers)
                .ThenInclude(ct => ct.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (teacher == null)
                return NotFound();

            return PartialView("_DetailsModal", teacher);
        }

        // GET: /Teacher/Create
        [HttpGet]
        public IActionResult Create()
        {
            return PartialView("_CreateModal");
        }

        // POST: /Teacher/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Teacher teacher)
        {
            if (ModelState.IsValid)
            {
                _context.Teachers.Add(teacher);

                try
                {
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Викладач успішно створений!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Сталася помилка при створенні викладача: " + ex.Message });
                }
            }

            return Json(new { success = false, message = "Невірні дані. Перевірте форму." });
        }

        // GET: /Teacher/Edit/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.Events)
                .Include(t => t.CourseTeachers)
                .ThenInclude(ct => ct.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (teacher == null)
                return NotFound();

            return PartialView("_EditModal", teacher);
        }

        // POST: /Teacher/Edit/{id} (AJAX для оновлення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModal(int id, Teacher teacher)
        {
            if (id != teacher.Id)
                return Json(new { success = false, message = "ID не співпадає." });

            if (ModelState.IsValid)
            {
                try
                {
                    var teacherInDb = await _context.Teachers
                        .Include(t => t.Events)
                        .Include(t => t.CourseTeachers)
                        .ThenInclude(ct => ct.Course)
                        .FirstOrDefaultAsync(t => t.Id == id);

                    if (teacherInDb == null)
                        return Json(new { success = false, message = "Викладач не знайдений." });

                    teacherInDb.FirstName = teacher.FirstName;
                    teacherInDb.LastName = teacher.LastName;
                    teacherInDb.Email = teacher.Email;

                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Дані успішно оновлено." });
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Json(new { success = false, message = "Помилка оновлення даних." });
                }
            }
            return PartialView("_EditModal", teacher);
        }

        // GET: /Teacher/Delete/{id} (Partial View для модального вікна підтвердження)
        [HttpGet]
        public async Task<IActionResult> DeleteModal(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.Events)
                .Include(t => t.CourseTeachers)
                .ThenInclude(ct => ct.Course)
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
                .Include(t => t.Events)
                .Include(t => t.CourseTeachers)
                .ThenInclude(ct => ct.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (teacher == null)
                return Json(new { success = false });

            foreach (var eventItem in teacher.Events.ToList())
            {
                _context.Events.Remove(eventItem);
            }

            foreach (var courseTeacher in teacher.CourseTeachers.ToList())
            {
                _context.CourseTeachers.Remove(courseTeacher);
            }

            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Викладач успішно видалений." });
        }
    }
}
