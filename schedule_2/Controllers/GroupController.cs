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
    public class GroupController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public GroupController(
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

        // GET: /Group/Index
        public async Task<IActionResult> Index()
        {
            var groups = await _context.Groups
                .Include(g => g.EventGroups)
                    .ThenInclude(eg => eg.Event)
                .Include(g => g.CourseGroups)
                    .ThenInclude(cg => cg.Course)
                .Include(g => g.Subgroups)
                .ToListAsync();

            // Передаємо інформацію про роль користувача у ViewBag
            ViewBag.IsAdministrator = await IsAdministratorAsync();

            return View(groups);
        }

        // GET: /Group/Details/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> DetailsModal(int id)
        {
            var group = await _context.Groups
                .Include(g => g.EventGroups)
                    .ThenInclude(eg => eg.Event)
                .Include(g => g.CourseGroups)
                    .ThenInclude(cg => cg.Course)
                .Include(g => g.Subgroups)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (group == null)
                return NotFound();

            return PartialView("_DetailsModal", group);
        }

        // GET: /Group/Create
        [HttpGet]
        [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть створювати групи
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
        [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть створювати групи
        public async Task<IActionResult> Create(Group group, int[] EventGroups, int[] CourseGroups, int[] Subgroups)
        {
            if (ModelState.IsValid)
            {
                // Якщо передані EventGroups, CourseGroups, Subgroups, то додаємо їх до групи
                if (EventGroups != null && EventGroups.Length > 0)
                {
                    group.EventGroups = new List<EventGroup>();
                    foreach (var eventId in EventGroups)
                    {
                        var eventItem = await _context.Events.FindAsync(eventId);
                        if (eventItem != null)
                        {
                            group.EventGroups.Add(new EventGroup { Event = eventItem });
                        }
                    }
                }

                if (CourseGroups != null && CourseGroups.Length > 0)
                {
                    group.CourseGroups = new List<CourseGroup>();
                    foreach (var courseId in CourseGroups)
                    {
                        var course = await _context.Courses.FindAsync(courseId);
                        if (course != null)
                        {
                            group.CourseGroups.Add(new CourseGroup { Course = course });
                        }
                    }
                }

                if (Subgroups != null && Subgroups.Length > 0)
                {
                    group.Subgroups = new List<Subgroup>();
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
        [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть редагувати групи
        public async Task<IActionResult> EditModal(int id)
        {
            var group = await _context.Groups
                .Include(g => g.EventGroups)
                    .ThenInclude(eg => eg.Event)
                .Include(g => g.CourseGroups)
                    .ThenInclude(cg => cg.Course)
                .Include(g => g.Subgroups)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (group == null)
                return NotFound();

            ViewBag.AllEvents = _context.Events.ToList() ?? new List<Event>();
            ViewBag.AllCourses = _context.Courses.ToList() ?? new List<Course>();
            ViewBag.AllSubgroups = _context.Subgroups.ToList() ?? new List<Subgroup>();

            return PartialView("_EditModal", group);
        }

        // POST: /Group/Edit/{id} (AJAX для оновлення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть редагувати групи
        public async Task<IActionResult> EditModal(int id, Group group, int[] EventGroups, int[] CourseGroups, int[] Subgroups)
        {
            if (id != group.Id)
                return Json(new { success = false, message = "ID не співпадає." });

            if (ModelState.IsValid)
            {
                var groupInDb = await _context.Groups
                        .Include(g => g.EventGroups)
                        .Include(g => g.CourseGroups)
                        .Include(g => g.Subgroups)
                        .FirstOrDefaultAsync(g => g.Id == id);

                if (groupInDb == null)
                    return Json(new { success = false, message = "Група не знайдена." });

                groupInDb.Name = group.Name;

                // Оновлення зв'язків з подіями
                groupInDb.EventGroups.Clear();
                if (EventGroups != null && EventGroups.Length > 0)
                {
                    foreach (var eventId in EventGroups)
                    {
                        groupInDb.EventGroups.Add(new EventGroup { GroupId = id, EventId = eventId });
                    }
                }

                // Оновлення зв'язків з курсами
                groupInDb.CourseGroups.Clear();
                if (CourseGroups != null && CourseGroups.Length > 0)
                {
                    foreach (var courseId in CourseGroups)
                    {
                        groupInDb.CourseGroups.Add(new CourseGroup { GroupId = id, CourseId = courseId });
                    }
                }

                // Оновлення зв'язків з підгрупами
                groupInDb.Subgroups.Clear();
                if (Subgroups != null && Subgroups.Length > 0)
                {
                    foreach (var subgroupId in Subgroups)
                    {
                        var subgroup = await _context.Subgroups.FindAsync(subgroupId);
                        if (subgroup != null)
                        {
                            groupInDb.Subgroups.Add(subgroup);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Дані успішно оновлено." });
            }
            return Json(new { success = false, message = "Невірні дані форми." });
        }

        // GET: /Group/Delete/{id} (Partial View для модального вікна підтвердження)
        [HttpGet]
        [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть видаляти групи
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
        [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть видаляти групи
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var group = await _context.Groups
                .Include(g => g.EventGroups)
                .Include(g => g.CourseGroups)
                .Include(g => g.Subgroups)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (group == null)
                return Json(new { success = false });

            // Видалення пов'язаних сутностей перед видаленням групи
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