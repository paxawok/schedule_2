using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using System;
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

        // GET: /Event/Details/{id} - Modal View
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

        // GET: /Event/Create - Modal View
        [HttpGet]
        public async Task<IActionResult> CreateModal()
        {
            // Create a list of teachers with lastname + firstname as display text
            var teachers = await _context.Teachers.ToListAsync();
            var teacherItems = teachers.Select(t => new
            {
                Id = t.Id,
                Name = $"{t.LastName} {t.FirstName}"
            });

            ViewBag.Teachers = new SelectList(teacherItems, "Id", "Name");
            ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name");
            ViewBag.Classrooms = new SelectList(await _context.Classrooms.ToListAsync(), "Id", "Name");
            ViewBag.Schedules = new SelectList(await _context.Schedules.ToListAsync(), "Id", "Name");
            ViewBag.Groups = new SelectList(await _context.Groups.ToListAsync(), "Id", "Name");
            ViewBag.Subgroups = await _context.Subgroups.ToListAsync();

            return PartialView("_CreateModal");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateModal(string Title, DateTime StartDateTime,
    DateTime EndDateTime, int TeacherId, int CourseId, int ClassroomId, int ScheduleId,
    bool IsRecurring, string RecurrencePattern, int[] selectedGroups, int[] selectedSubgroups)
        {
            // Перевіряємо наявність обов'язкових полів
            if (string.IsNullOrEmpty(Title) || TeacherId == 0 || CourseId == 0 ||
                ClassroomId == 0 || ScheduleId == 0)
            {
                var errorList = new List<object>();

                if (TeacherId == 0)
                    errorList.Add(new { key = "Teacher", errors = new[] { "The Teacher field is required." } });

                if (CourseId == 0)
                    errorList.Add(new { key = "Course", errors = new[] { "The Course field is required." } });

                if (ClassroomId == 0)
                    errorList.Add(new { key = "Classroom", errors = new[] { "The Classroom field is required." } });

                if (ScheduleId == 0)
                    errorList.Add(new { key = "Schedule", errors = new[] { "The Schedule field is required." } });

                return Json(new { success = false, message = "Невалідна модель", errors = errorList });
            }

            // Створюємо новий об'єкт події
            var eventItem = new Event
            {
                Title = Title,
                StartDateTime = StartDateTime,
                EndDateTime = EndDateTime,
                TeacherId = TeacherId,
                CourseId = CourseId,
                ClassroomId = ClassroomId,
                ScheduleId = ScheduleId,
                IsRecurring = IsRecurring,
                RecurrencePattern = RecurrencePattern
            };

            try
            {
                // Додаємо нову подію
                _context.Events.Add(eventItem);
                await _context.SaveChangesAsync();

                // Додаємо зв'язки з групами
                if (selectedGroups != null && selectedGroups.Length > 0)
                {
                    foreach (var groupId in selectedGroups)
                    {
                        _context.EventGroups.Add(new EventGroup { EventId = eventItem.Id, GroupId = groupId });
                    }
                }

                // Додаємо зв'язки з підгрупами
                if (selectedSubgroups != null && selectedSubgroups.Length > 0)
                {
                    foreach (var subgroupId in selectedSubgroups)
                    {
                        _context.SubgroupEvents.Add(new SubgroupEvent { EventId = eventItem.Id, SubgroupId = subgroupId });
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Подію успішно створено!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Помилка при створенні події: " + ex.Message });
            }
        }
        // GET: /Event/Edit/{id} - Modal View
        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            var eventItem = await _context.Events
                .Include(e => e.EventGroups)
                .Include(e => e.SubgroupEvents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (eventItem == null)
                return NotFound();

            // Підготовка даних для вибору
            var teachers = await _context.Teachers.ToListAsync();
            var teacherItems = teachers.Select(t => new
            {
                Id = t.Id,
                Name = $"{t.LastName} {t.FirstName}"
            });
            ViewBag.Teachers = new SelectList(teacherItems, "Id", "Name", eventItem.TeacherId);
            ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name", eventItem.CourseId);
            ViewBag.Classrooms = new SelectList(await _context.Classrooms.ToListAsync(), "Id", "Name", eventItem.ClassroomId);
            ViewBag.Schedules = new SelectList(await _context.Schedules.ToListAsync(), "Id", "Name", eventItem.ScheduleId);

            // Отримуємо всі групи та підгрупи
            ViewBag.Groups = await _context.Groups.ToListAsync();
            ViewBag.Subgroups = await _context.Subgroups.ToListAsync();

            // Зберігаємо вибрані групи та підгрупи
            ViewBag.SelectedGroups = eventItem.EventGroups.Select(eg => eg.GroupId).ToList();
            ViewBag.SelectedSubgroups = eventItem.SubgroupEvents.Select(se => se.SubgroupId).ToList();

            return PartialView("_EditModal", eventItem);
        }

        // POST: /Event/Edit/{id} - AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModal(int id, string Title, DateTime StartDateTime,
            DateTime EndDateTime, int TeacherId, int CourseId, int ClassroomId, int ScheduleId,
            bool IsRecurring, string RecurrencePattern, int[] selectedGroups, int[] selectedSubgroups)
        {
            // Перевіряємо наявність обов'язкових полів
            if (string.IsNullOrEmpty(Title) || TeacherId == 0 || CourseId == 0 ||
                ClassroomId == 0 || ScheduleId == 0)
            {
                var errorList = new List<object>();

                if (TeacherId == 0)
                    errorList.Add(new { key = "Teacher", errors = new[] { "The Teacher field is required." } });

                if (CourseId == 0)
                    errorList.Add(new { key = "Course", errors = new[] { "The Course field is required." } });

                if (ClassroomId == 0)
                    errorList.Add(new { key = "Classroom", errors = new[] { "The Classroom field is required." } });

                if (ScheduleId == 0)
                    errorList.Add(new { key = "Schedule", errors = new[] { "The Schedule field is required." } });

                return Json(new { success = false, message = "Невалідна модель", errors = errorList });
            }

            var eventInDb = await _context.Events
                .Include(e => e.EventGroups)
                .Include(e => e.SubgroupEvents)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventInDb == null)
                return Json(new { success = false, message = "Подію не знайдено." });

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Оновлюємо базові властивості події
                    eventInDb.Title = Title;
                    eventInDb.StartDateTime = StartDateTime;
                    eventInDb.EndDateTime = EndDateTime;
                    eventInDb.IsRecurring = IsRecurring;
                    eventInDb.RecurrencePattern = RecurrencePattern;
                    eventInDb.TeacherId = TeacherId;
                    eventInDb.CourseId = CourseId;
                    eventInDb.ClassroomId = ClassroomId;
                    eventInDb.ScheduleId = ScheduleId;

                    // Видаляємо існуючі зв'язки з групами
                    _context.EventGroups.RemoveRange(eventInDb.EventGroups);

                    // Додаємо нові зв'язки з групами
                    if (selectedGroups != null && selectedGroups.Length > 0)
                    {
                        foreach (var groupId in selectedGroups)
                        {
                            _context.EventGroups.Add(new EventGroup { EventId = id, GroupId = groupId });
                        }
                    }

                    // Видаляємо існуючі зв'язки з підгрупами
                    _context.SubgroupEvents.RemoveRange(eventInDb.SubgroupEvents);

                    // Додаємо нові зв'язки з підгрупами
                    if (selectedSubgroups != null && selectedSubgroups.Length > 0)
                    {
                        foreach (var subgroupId in selectedSubgroups)
                        {
                            _context.SubgroupEvents.Add(new SubgroupEvent { EventId = id, SubgroupId = subgroupId });
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Json(new { success = true, message = "Подію успішно оновлено!" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Помилка при оновленні події: " + ex.Message });
                }
            }
        }
        // GET: /Event/Delete/{id} - Modal View
        [HttpGet]
        public async Task<IActionResult> DeleteModal(int id)
        {
            var eventItem = await _context.Events
                .Include(e => e.Teacher)
                .Include(e => e.Course)
                .Include(e => e.Classroom)
                .Include(e => e.Schedule)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (eventItem == null)
                return NotFound();

            return PartialView("_DeleteModal", eventItem);
        }

        // POST: /Event/DeleteConfirmed/{id} - AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var eventItem = await _context.Events
                        .Include(e => e.EventGroups)
                        .Include(e => e.SubgroupEvents)
                        .FirstOrDefaultAsync(m => m.Id == id);

                    if (eventItem == null)
                        return Json(new { success = false, message = "Подію не знайдено." });

                    // Видаляємо всі зв'язки з групами
                    _context.EventGroups.RemoveRange(eventItem.EventGroups);

                    // Видаляємо всі зв'язки з підгрупами
                    _context.SubgroupEvents.RemoveRange(eventItem.SubgroupEvents);

                    // Видаляємо саму подію
                    _context.Events.Remove(eventItem);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Json(new { success = true, message = "Подію успішно видалено!" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Помилка при видаленні події: " + ex.Message });
                }
            }
        }

        // Helper method to check if Event exists
        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }
    }
}