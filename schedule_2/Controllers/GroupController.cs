using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using System.Threading.Tasks;

namespace schedule_2.Controllers
{
    public class GroupController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GroupController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Group/Index
        // Відображає список груп із подіями та курсами
        public async Task<IActionResult> Index()
        {
            var groups = await _context.Groups
                .Include(g => g.Events)    // Завантажуємо події
                .Include(g => g.Courses)   // Завантажуємо курси
                .ToListAsync();
            return View(groups);
        }

        // GET: /Group/Details/{id}
        // Показує деталі групи, включаючи події та курси
        public async Task<IActionResult> Details(int id)
        {
            var group = await _context.Groups
                .Include(g => g.Events)    // Завантажуємо події
                .Include(g => g.Courses)   // Завантажуємо курси
                .FirstOrDefaultAsync(m => m.Id == id);

            if (group == null)
                return NotFound();

            return View(group);
        }

        // GET: /Group/Create
        // Форма створення групи з вибором подій і курсів
        public IActionResult Create()
        {
            ViewBag.Events = new MultiSelectList(_context.Events, "Id", "Title");
            ViewBag.Courses = new MultiSelectList(_context.Courses, "Id", "Name");
            return View();
        }

        // POST: /Group/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Group group, int[] selectedEvents, int[] selectedCourses)
        {
            if (ModelState.IsValid)
            {
                _context.Add(group);
                await _context.SaveChangesAsync();

                // Додаємо події до групи (M:M зв’язок)
                if (selectedEvents != null)
                {
                    group.Events = await _context.Events
                        .Where(e => selectedEvents.Contains(e.Id))
                        .ToListAsync();
                }

                // Додаємо курси до групи (M:M зв’язок)
                if (selectedCourses != null)
                {
                    group.Courses = await _context.Courses
                        .Where(c => selectedCourses.Contains(c.Id))
                        .ToListAsync();
                }

                _context.Update(group);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            ViewBag.Events = new MultiSelectList(_context.Events, "Id", "Title");
            ViewBag.Courses = new MultiSelectList(_context.Courses, "Id", "Name");
            return View(group);
        }

        // GET: /Group/Edit/{id}
        // Форма редагування групи з вибором подій і курсів
        public async Task<IActionResult> Edit(int id)
        {
            var group = await _context.Groups
                .Include(g => g.Events)    // Завантажуємо події
                .Include(g => g.Courses)   // Завантажуємо курси
                .FirstOrDefaultAsync(m => m.Id == id);

            if (group == null)
                return NotFound();

            ViewBag.Events = new MultiSelectList(_context.Events, "Id", "Title", group.Events.Select(e => e.Id));
            ViewBag.Courses = new MultiSelectList(_context.Courses, "Id", "Name", group.Courses.Select(c => c.Id));
            return View(group);
        }

        // POST: /Group/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Group group, int[] selectedEvents, int[] selectedCourses)
        {
            if (id != group.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Очищаємо попередні зв’язки
                    group.Events.Clear();
                    group.Courses.Clear();
                    _context.Update(group);

                    // Додаємо нові події
                    if (selectedEvents != null)
                    {
                        group.Events = await _context.Events
                            .Where(e => selectedEvents.Contains(e.Id))
                            .ToListAsync();
                    }

                    // Додаємо нові курси
                    if (selectedCourses != null)
                    {
                        group.Courses = await _context.Courses
                            .Where(c => selectedCourses.Contains(c.Id))
                            .ToListAsync();
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GroupExists(group.Id))
                        return NotFound();
                    else
                        throw;
                }
            }
            ViewBag.Events = new MultiSelectList(_context.Events, "Id", "Title", group.Events.Select(e => e.Id));
            ViewBag.Courses = new MultiSelectList(_context.Courses, "Id", "Name", group.Courses.Select(c => c.Id));
            return View(group);
        }

        // POST: /Group/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var group = await _context.Groups
                .Include(g => g.Events)    // Завантажуємо події для перевірки
                .Include(g => g.Courses)   // Завантажуємо курси для перевірки
                .FirstOrDefaultAsync(m => m.Id == id);

            if (group != null)
            {
                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Допоміжний метод для перевірки існування групи
        private bool GroupExists(int id)
        {
            return _context.Groups.Any(e => e.Id == id);
        }
    }
}