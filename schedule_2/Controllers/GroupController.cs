using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using System.Linq;
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
        public async Task<IActionResult> Index()
        {
            var groups = await _context.Groups
                .Include(g => g.EventGroups)
                .Include(g => g.CourseGroups)
                .Include(g => g.Subgroups)
                .ToListAsync();
            return View(groups);
        }

        // GET: /Group/Details/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> DetailsModal(int id)
        {
            var group = await _context.Groups
                .Include(g => g.EventGroups)
                .Include(g => g.CourseGroups)
                .Include(g => g.Subgroups)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (group == null)
                return NotFound();

            return PartialView("_DetailsModal", group);
        }

        // GET: /Group/Create
        [HttpGet]
        public IActionResult Create()
        {
            // Ініціалізація колекцій за замовчуванням, щоб уникнути null
            ViewBag.AllEvents = _context.Events.ToList() ?? new List<Event>();
            ViewBag.AllCourses = _context.Courses.ToList() ?? new List<Course>();
            ViewBag.AllSubgroups = _context.Subgroups.ToList() ?? new List<Subgroup>();

            // Повертаємо модальне представлення для створення групи
            return PartialView("_CreateModal");
        }

        // POST: Group/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Group group, int[] EventGroups, int[] CourseGroups, int[] Subgroups)
        {
            if (ModelState.IsValid)
            {
                // Якщо передані EventGroups, CourseGroups, Subgroups, то додаємо їх до групи
                if (EventGroups != null && EventGroups.Length > 0)
                {
                    foreach (var eventId in EventGroups)
                    {
                        var eventGroup = await _context.Events.FindAsync(eventId);
                        if (eventGroup != null)
                        {
                            group.EventGroups.Add(new EventGroup { GroupId = group.Id, EventId = eventGroup.Id });
                        }
                    }
                }

                if (CourseGroups != null && CourseGroups.Length > 0)
                {
                    foreach (var courseId in CourseGroups)
                    {
                        var courseGroup = await _context.Courses.FindAsync(courseId);
                        if (courseGroup != null)
                        {
                            group.CourseGroups.Add(new CourseGroup { GroupId = group.Id, CourseId = courseGroup.Id });
                        }
                    }
                }

                if (Subgroups != null && Subgroups.Length > 0)
                {
                    foreach (var subgroupId in Subgroups)
                    {
                        var subgroup = await _context.Subgroups.FindAsync(subgroupId);
                        if (subgroup != null)
                        {
                            group.Subgroups.Add(subgroup);
                        }
                    }
                }

                _context.Groups.Add(group);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Групу успішно створено!" });
            }

            return Json(new { success = false, message = "Невірні дані. Перевірте форму." });
        }

        // GET: /Group/Edit/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            var group = await _context.Groups
                .Include(g => g.EventGroups)
                .Include(g => g.CourseGroups)
                .Include(g => g.Subgroups)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (group == null)
                return NotFound();

            return PartialView("_EditModal", group);
        }

        // POST: /Group/Edit/{id} (AJAX для оновлення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModal(int id, Group group)
        {
            if (id != group.Id)
                return Json(new { success = false, message = "ID не співпадає." });

            if (ModelState.IsValid)
            {
                try
                {
                    var groupInDb = await _context.Groups
                        .Include(g => g.EventGroups)
                        .Include(g => g.CourseGroups)
                        .Include(g => g.Subgroups)
                        .FirstOrDefaultAsync(g => g.Id == id);

                    if (groupInDb == null)
                        return Json(new { success = false, message = "Група не знайдена." });

                    groupInDb.Name = group.Name;

                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Дані успішно оновлено." });
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Json(new { success = false, message = "Помилка оновлення даних." });
                }
            }
            return PartialView("_EditModal", group);
        }

        // GET: /Group/Delete/{id} (Partial View для модального вікна підтвердження)
        [HttpGet]
        public async Task<IActionResult> DeleteModal(int id)
        {
            var group = await _context.Groups
                .Include(g => g.EventGroups)
                .Include(g => g.CourseGroups)
                .Include(g => g.Subgroups)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (group == null)
                return NotFound();

            return PartialView("_DeleteModal", group);
        }

        // POST: /Group/DeleteConfirmed/{id} (AJAX для видалення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var group = await _context.Groups
                .Include(g => g.EventGroups)
                .Include(g => g.CourseGroups)
                .Include(g => g.Subgroups)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (group == null)
                return Json(new { success = false });

            // Optionally remove related entities before deleting the group
            foreach (var eventGroup in group.EventGroups.ToList())
            {
                _context.EventGroups.Remove(eventGroup);
            }
            foreach (var courseGroup in group.CourseGroups.ToList())
            {
                _context.CourseGroups.Remove(courseGroup);
            }
            foreach (var subgroup in group.Subgroups.ToList())
            {
                _context.Subgroups.Remove(subgroup);
            }

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Група успішно видалена." });
        }
    }
}
