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
        // Відображає список викладачів із їхніми курсами
        public async Task<IActionResult> Index()
        {
            var teachers = await _context.Teachers
                .Include(t => t.Courses) // Завантажуємо курси
                .ToListAsync();
            return View(teachers);
        }

        // GET: /Teacher/Details/{id}
        // Показує деталі викладача, включаючи курси
        public async Task<IActionResult> Details(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.Courses) // Завантажуємо курси
                .FirstOrDefaultAsync(m => m.Id == id);

            if (teacher == null)
                return NotFound();

            return View(teacher);
        }

        // GET: /Teacher/Create
        // Форма створення викладача з вибором курсів
        public IActionResult Create()
        {
            ViewBag.Courses = new MultiSelectList(_context.Courses, "Id", "Name");
            return View();
        }

        // POST: /Teacher/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Teacher teacher, int[] selectedCourses)
        {
            if (ModelState.IsValid)
            {
                _context.Add(teacher);
                await _context.SaveChangesAsync();

                // Додаємо курси до викладача (M:M зв’язок)
                if (selectedCourses != null)
                {
                    teacher.Courses = await _context.Courses
                        .Where(c => selectedCourses.Contains(c.Id))
                        .ToListAsync();
                    _context.Update(teacher);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            ViewBag.Courses = new MultiSelectList(_context.Courses, "Id", "Name");
            return View(teacher);
        }

        // GET: /Teacher/Edit/{id}
        // Форма редагування викладача з вибором курсів
        public async Task<IActionResult> Edit(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.Courses) // Завантажуємо курси
                .FirstOrDefaultAsync(m => m.Id == id);

            if (teacher == null)
                return NotFound();

            ViewBag.Courses = new MultiSelectList(_context.Courses, "Id", "Name", teacher.Courses.Select(c => c.Id));
            return View(teacher);
        }

        // POST: /Teacher/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Teacher teacher, int[] selectedCourses)
        {
            if (id != teacher.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Очищаємо попередні курси
                    teacher.Courses.Clear();
                    _context.Update(teacher);

                    // Додаємо нові курси
                    if (selectedCourses != null)
                    {
                        teacher.Courses = await _context.Courses
                            .Where(c => selectedCourses.Contains(c.Id))
                            .ToListAsync();
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeacherExists(teacher.Id))
                        return NotFound();
                    else
                        throw;
                }
            }
            ViewBag.Courses = new MultiSelectList(_context.Courses, "Id", "Name", teacher.Courses.Select(c => c.Id));
            return View(teacher);
        }

        // POST: /Teacher/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.Courses) // Завантажуємо курси для перевірки
                .FirstOrDefaultAsync(m => m.Id == id);

            if (teacher != null)
            {
                _context.Teachers.Remove(teacher);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Допоміжний метод для перевірки існування викладача
        private bool TeacherExists(int id)
        {
            return _context.Teachers.Any(e => e.Id == id);
        }
    }
}
