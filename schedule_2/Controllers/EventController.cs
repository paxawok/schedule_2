using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using System.Linq;
using System.Threading.Tasks;

namespace schedule_2.Controllers
{
    public class EventController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Event/Index
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .Include(e => e.Teacher)
                .Include(e => e.Course)
                .Include(e => e.Classroom)
                .Include(e => e.Schedule)
                .Include(e => e.EventGroups)
                    .ThenInclude(eg => eg.Group)
                .Include(e => e.SubgroupEvents)
                    .ThenInclude(se => se.Subgroup)
                .ToListAsync();
            return View(events);
        }

        // GET: /Event/Details/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> DetailsModal(int id)
        {
            var eventItem = await _context.Events
                .Include(e => e.Teacher)
                .Include(e => e.Course)
                .Include(e => e.Classroom)
                .Include(e => e.Schedule)
                .Include(e => e.EventGroups)
                    .ThenInclude(eg => eg.Group)
                .Include(e => e.SubgroupEvents)
                    .ThenInclude(se => se.Subgroup)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (eventItem == null)
                return NotFound();

            return PartialView("_DetailsModal", eventItem);
        }

        // GET: /Event/Create (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> CreateModal()
        {
            ViewBag.Teachers = new SelectList(await _context.Teachers.ToListAsync(), "Id", "FullName");
            ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name");
            ViewBag.Classrooms = new SelectList(await _context.Classrooms.ToListAsync(), "Id", "Name");
            ViewBag.Schedules = new SelectList(await _context.Schedules.ToListAsync(), "Id", "Name");
            ViewBag.Groups = new SelectList(await _context.Groups.ToListAsync(), "Id", "Name");

            return PartialView("_CreateModal");
        }

        // POST: /Event/Create (AJAX для створення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateModal(Event eventItem, int[] selectedGroups, int[] selectedSubgroups)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Додаємо нову подію
                    _context.Add(eventItem);
                    await _context.SaveChangesAsync();

                    // Додаємо зв'язки з групами
                    if (selectedGroups != null)
                    {
                        foreach (var groupId in selectedGroups)
                        {
                            _context.EventGroups.Add(new EventGroup { EventId = eventItem.Id, GroupId = groupId });
                        }
                    }

                    // Додаємо зв'язки з підгрупами
                    if (selectedSubgroups != null)
                    {
                        foreach (var subgroupId in selectedSubgroups)
                        {
                            _context.SubgroupEvents.Add(new SubgroupEvent { EventId = eventItem.Id, SubgroupId = subgroupId });
                        }
                    }

                    await _context.SaveChangesAsync();

                    // Повертаємо результат на сторінку після успішного створення
                    return RedirectToAction("Index"); // Переходимо на сторінку зі списком подій
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Помилка створення події.");
                }
            }

            // Повторно передаємо вибірки для форми у випадку помилки
            ViewBag.Teachers = new SelectList(await _context.Teachers.ToListAsync(), "Id", "FullName", eventItem.TeacherId);
            ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name", eventItem.CourseId);
            ViewBag.Classrooms = new SelectList(await _context.Classrooms.ToListAsync(), "Id", "Name", eventItem.ClassroomId);
            ViewBag.Schedules = new SelectList(await _context.Schedules.ToListAsync(), "Id", "Name", eventItem.ScheduleId);
            ViewBag.Groups = new SelectList(await _context.Groups.ToListAsync(), "Id", "Name");

            return PartialView("_CreateModal", eventItem); // Повертаємо форму в разі помилки
        }


        // GET: /Event/Edit/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            var eventItem = await _context.Events
                .Include(e => e.EventGroups)
                .Include(e => e.SubgroupEvents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (eventItem == null)
                return NotFound();

            ViewBag.Teachers = new SelectList(await _context.Teachers.ToListAsync(), "Id", "FullName", eventItem.TeacherId);
            ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name", eventItem.CourseId);
            ViewBag.Classrooms = new SelectList(await _context.Classrooms.ToListAsync(), "Id", "Name", eventItem.ClassroomId);
            ViewBag.Schedules = new SelectList(await _context.Schedules.ToListAsync(), "Id", "Name", eventItem.ScheduleId);
            ViewBag.Groups = new SelectList(await _context.Groups.ToListAsync(), "Id", "Name", eventItem.EventGroups.Select(eg => eg.GroupId));

            return PartialView("_EditModal", eventItem);
        }

        // POST: /Event/Edit/{id} (AJAX для оновлення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModal(int id, Event eventItem, int[] selectedGroups, int[] selectedSubgroups)
        {
            if (id != eventItem.Id)
                return Json(new { success = false, message = "ID не співпадає." });

            if (ModelState.IsValid)
            {
                try
                {
                    var eventInDb = await _context.Events
                        .Include(e => e.EventGroups)
                        .Include(e => e.SubgroupEvents)
                        .FirstOrDefaultAsync(e => e.Id == id);

                    if (eventInDb == null)
                        return Json(new { success = false, message = "Подія не знайдена." });

                    // Оновлюємо основні поля події
                    eventInDb.Title = eventItem.Title;
                    eventInDb.StartDateTime = eventItem.StartDateTime;
                    eventInDb.EndDateTime = eventItem.EndDateTime;
                    eventInDb.IsRecurring = eventItem.IsRecurring;
                    eventInDb.RecurrencePattern = eventItem.RecurrencePattern;

                    // Очищаємо попередні зв'язки з групами та підгрупами
                    eventInDb.EventGroups.Clear();
                    eventInDb.SubgroupEvents.Clear();

                    // Додаємо нові зв'язки з групами
                    if (selectedGroups != null)
                    {
                        foreach (var groupId in selectedGroups)
                        {
                            eventInDb.EventGroups.Add(new EventGroup { EventId = eventInDb.Id, GroupId = groupId });
                        }
                    }

                    // Додаємо нові зв'язки з підгрупами
                    if (selectedSubgroups != null)
                    {
                        foreach (var subgroupId in selectedSubgroups)
                        {
                            eventInDb.SubgroupEvents.Add(new SubgroupEvent { EventId = eventInDb.Id, SubgroupId = subgroupId });
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

            // Повторно передаємо дані для виправлення помилок
            ViewBag.Teachers = new SelectList(await _context.Teachers.ToListAsync(), "Id", "FullName", eventItem.TeacherId);
            ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name", eventItem.CourseId);
            ViewBag.Classrooms = new SelectList(await _context.Classrooms.ToListAsync(), "Id", "Name", eventItem.ClassroomId);
            ViewBag.Schedules = new SelectList(await _context.Schedules.ToListAsync(), "Id", "Name", eventItem.ScheduleId);
            ViewBag.Groups = new SelectList(await _context.Groups.ToListAsync(), "Id", "Name", eventItem.EventGroups.Select(eg => eg.GroupId));

            return PartialView("_EditModal", eventItem);
        }

        // GET: /Event/Delete/{id} (Partial View для модального вікна підтвердження)
        [HttpGet]
        public async Task<IActionResult> DeleteModal(int id)
        {
            var eventItem = await _context.Events
                .Include(e => e.EventGroups)
                .Include(e => e.SubgroupEvents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (eventItem == null)
                return NotFound();

            return PartialView("_DeleteModal", eventItem);
        }

        // POST: /Event/DeleteConfirmed/{id} (AJAX для видалення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eventItem = await _context.Events
                .Include(e => e.EventGroups)
                .Include(e => e.SubgroupEvents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (eventItem != null)
            {
                _context.Events.Remove(eventItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Подію успішно видалено." });
            }

            return Json(new { success = false, message = "Подію не знайдено." });
        }

        // Допоміжний метод для перевірки існування події
        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }
    }
}
