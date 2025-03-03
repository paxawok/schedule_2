using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
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
        // Відображає список курсів із викладачами та групами
        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                .Include(c => c.Teachers) // Завантажуємо викладачів
                .Include(c => c.Groups)   // Завантажуємо групи
                .ToListAsync();
            return View(courses);
        }

        // GET: /Course/Details/{id}
        // Показує деталі курсу, включаючи викладачів і групи
        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Teachers) // Завантажуємо викладачів
                .Include(c => c.Groups)   // Завантажуємо групи
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
                return NotFound();

            return View(course);
        }

        // GET: /Course/Create
        // Форма створення курсу з вибором викладачів і груп
        public IActionResult Create()
        {
            ViewBag.Teachers = new MultiSelectList(_context.Teachers, "Id", "LastName");
            ViewBag.Groups = new MultiSelectList(_context.Groups, "Id", "Name");
            return View();
        }

        // POST: /Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Course course, int[] selectedTeachers, int[] selectedGroups)
        {
            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();

                // Додаємо викладачів до курсу (M:M зв’язок)
                if (selectedTeachers != null)
                {
                    course.Teachers = await _context.Teachers
                        .Where(t => selectedTeachers.Contains(t.Id))
                        .ToListAsync();
                }

                // Додаємо групи до курсу (M:M зв’язок)
                if (selectedGroups != null)
                {
                    course.Groups = await _context.Groups
                        .Where(g => selectedGroups.Contains(g.Id))
                        .ToListAsync();
                }

                _context.Update(course);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            ViewBag.Teachers = new MultiSelectList(_context.Teachers, "Id", "LastName");
            ViewBag.Groups = new MultiSelectList(_context.Groups, "Id", "Name");
            return View(course);
        }

        // GET: /Course/Edit/{id}
        // Форма редагування курсу з вибором викладачів і груп
        public async Task<IActionResult> Edit(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Teachers) // Завантажуємо викладачів
                .Include(c => c.Groups)   // Завантажуємо групи
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
                return NotFound();

            ViewBag.Teachers = new MultiSelectList(_context.Teachers, "Id", "LastName", course.Teachers.Select(t => t.Id));
            ViewBag.Groups = new MultiSelectList(_context.Groups, "Id", "Name", course.Groups.Select(g => g.Id));
            return View(course);
        }

        // POST: /Course/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Course course, int[] selectedTeachers, int[] selectedGroups)
        {
            if (id != course.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var courseInDb = await _context.Courses
                        .Include(c => c.Teachers)
                        .Include(c => c.Groups)
                        .FirstOrDefaultAsync(c => c.Id == id);

                    if (courseInDb == null)
                        return NotFound();

                    // Оновлюємо основні поля курсу
                    courseInDb.Name = course.Name;
                    courseInDb.Description = course.Description;

                    // Оновлення зв’язків із викладачами
                    var teacherIds = selectedTeachers ?? Array.Empty<int>();
                    var currentTeachers = courseInDb.Teachers.Select(t => t.Id).ToList();

                    // Видаляємо викладачів, які більше не вибрані
                    var teachersToRemove = courseInDb.Teachers.Where(t => !teacherIds.Contains(t.Id)).ToList();
                    foreach (var teacher in teachersToRemove)
                    {
                        courseInDb.Teachers.Remove(teacher);
                    }

                    // Додаємо нових викладачів, якщо їх ще немає
                    var teachersToAdd = await _context.Teachers
                        .Where(t => teacherIds.Contains(t.Id) && !currentTeachers.Contains(t.Id))
                        .ToListAsync();
                    foreach (var teacher in teachersToAdd)
                    {
                        courseInDb.Teachers.Add(teacher);
                    }

                    // Оновлення зв’язків із групами (аналогічно)
                    var groupIds = selectedGroups ?? Array.Empty<int>();
                    var currentGroups = courseInDb.Groups.Select(g => g.Id).ToList();

                    var groupsToRemove = courseInDb.Groups.Where(g => !groupIds.Contains(g.Id)).ToList();
                    foreach (var group in groupsToRemove)
                    {
                        courseInDb.Groups.Remove(group);
                    }

                    var groupsToAdd = await _context.Groups
                        .Where(g => groupIds.Contains(g.Id) && !currentGroups.Contains(g.Id))
                        .ToListAsync();
                    foreach (var group in groupsToAdd)
                    {
                        courseInDb.Groups.Add(group);
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id))
                        return NotFound();
                    else
                        throw;
                }
            }
            ViewBag.Teachers = new MultiSelectList(_context.Teachers, "Id", "LastName", course.Teachers.Select(t => t.Id));
            ViewBag.Groups = new MultiSelectList(_context.Groups, "Id", "Name", course.Groups.Select(g => g.Id));
            return View(course);
        }

        // POST: /Course/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Teachers) // Завантажуємо викладачів для перевірки
                .Include(c => c.Groups)   // Завантажуємо групи для перевірки
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Допоміжний метод для перевірки існування курсу
        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}