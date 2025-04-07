using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using System;
using System.Linq;
using System.Security.Claims;
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

        // GET: /Event/Details/{id} - Модальне вікно
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

        // GET: /Event/Create - Модальне вікно
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CreateModal()
        {
            // Якщо користувач не адміністратор і не викладач, забороняємо доступ
            if (!User.IsInRole("Administrator") && !User.IsInRole("Teacher"))
            {
                return Forbid();
            }

            // Отримуємо ID поточного користувача
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Створюємо список викладачів з прізвищем + ім'ям як текст для відображення
            var teachers = await _context.Teachers.ToListAsync();
            var teacherItems = teachers.Select(t => new
            {
                Id = t.Id,
                Name = $"{t.LastName} {t.FirstName}"
            });

            // Отримуємо список курсів
            var courses = await _context.Courses.ToListAsync();

            // Для викладачів обмежуємо вибір
            if (User.IsInRole("Teacher") && !User.IsInRole("Administrator"))
            {
                // Знаходимо поточного викладача
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

                if (teacher != null)
                {
                    // Автоматично вибираємо поточного викладача
                    ViewBag.CurrentTeacherId = teacher.Id;

                    // Обмежуємо список викладачів лише поточним
                    teacherItems = teacherItems.Where(t => t.Id == teacher.Id);

                    // Отримуємо тільки курси, пов'язані з цим викладачем
                    var teacherCourseIds = await _context.CourseTeachers
                        .Where(ct => ct.TeacherId == teacher.Id)
                        .Select(ct => ct.CourseId)
                        .ToListAsync();

                    courses = await _context.Courses
                        .Where(c => teacherCourseIds.Contains(c.Id))
                        .ToListAsync();
                }
            }

            ViewBag.Teachers = new SelectList(teacherItems, "Id", "Name");
            ViewBag.Courses = new SelectList(courses, "Id", "Name");
            ViewBag.Classrooms = new SelectList(await _context.Classrooms.ToListAsync(), "Id", "Name");
            ViewBag.Schedules = new SelectList(await _context.Schedules.ToListAsync(), "Id", "Name");
            ViewBag.Groups = new SelectList(await _context.Groups.ToListAsync(), "Id", "Name");

            // Не передаємо підгрупи - вони будуть завантажені через AJAX після вибору групи

            return PartialView("_CreateModal");
        }

        // Додатковий метод для отримання підгруп групи через AJAX
        [HttpGet]
        public async Task<IActionResult> GetSubgroupsByGroup(int groupId)
        {
            if (groupId <= 0)
            {
                return Json(new List<object>());
            }

            var subgroups = await _context.Subgroups
                .Where(s => s.GroupId == groupId)
                .Select(s => new { id = s.Id, name = s.Name })
                .ToListAsync();

            return Json(subgroups);
        }

        // POST: /Event/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CreateModal(string Title, DateTime StartDateTime,
            DateTime EndDateTime, int TeacherId, int CourseId, int ClassroomId, int ScheduleId,
            bool IsRecurring, string RecurrencePattern, int[] selectedGroups, int[] selectedSubgroups)
        {
            // Якщо користувач не адміністратор і не викладач, забороняємо доступ
            if (!User.IsInRole("Administrator") && !User.IsInRole("Teacher"))
            {
                return Json(new { success = false, message = "Недостатньо прав для створення події" });
            }

            // Для викладачів: перевірка, що подія створюється для себе
            if (User.IsInRole("Teacher") && !User.IsInRole("Administrator"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

                if (teacher == null || teacher.Id != TeacherId)
                {
                    return Json(new { success = false, message = "Ви можете створювати події тільки для себе" });
                }

                // Перевірка, що викладач має доступ до вибраного курсу
                var hasCourseAccess = await _context.CourseTeachers
                    .AnyAsync(ct => ct.CourseId == CourseId && ct.TeacherId == teacher.Id);

                if (!hasCourseAccess)
                {
                    return Json(new { success = false, message = "Ви можете створювати події тільки для своїх курсів" });
                }
            }

            // Перевіряємо наявність обов'язкових полів
            if (string.IsNullOrEmpty(Title) || TeacherId == 0 || CourseId == 0 ||
                ClassroomId == 0 || ScheduleId == 0 || selectedGroups == null || selectedGroups.Length == 0)
            {
                var errorList = new List<object>();

                if (string.IsNullOrEmpty(Title))
                    errorList.Add(new { key = "Title", errors = new[] { "Назва події є обов'язковою." } });

                if (TeacherId == 0)
                    errorList.Add(new { key = "Teacher", errors = new[] { "Поле Викладач є обов'язковим." } });

                if (CourseId == 0)
                    errorList.Add(new { key = "Course", errors = new[] { "Поле Курс є обов'язковим." } });

                if (ClassroomId == 0)
                    errorList.Add(new { key = "Classroom", errors = new[] { "Поле Аудиторія є обов'язковим." } });

                if (ScheduleId == 0)
                    errorList.Add(new { key = "Schedule", errors = new[] { "Поле Розклад є обов'язковим." } });

                if (selectedGroups == null || selectedGroups.Length == 0)
                    errorList.Add(new { key = "Groups", errors = new[] { "Має бути вибрана хоча б одна група." } });

                return Json(new { success = false, message = "Невалідна модель", errors = errorList });
            }

            // Перевіряємо, чи існують вибрані групи
            foreach (var groupId in selectedGroups)
            {
                var group = await _context.Groups.FindAsync(groupId);
                if (group == null)
                {
                    return Json(new { success = false, message = $"Група з ID {groupId} не існує" });
                }
            }

            // Якщо вибрані підгрупи, перевіряємо їх приналежність до вибраних груп
            if (selectedSubgroups != null && selectedSubgroups.Length > 0)
            {
                foreach (var subgroupId in selectedSubgroups)
                {
                    var subgroup = await _context.Subgroups
                        .FirstOrDefaultAsync(s => s.Id == subgroupId && selectedGroups.Contains(s.GroupId));

                    if (subgroup == null)
                    {
                        return Json(new { success = false, message = $"Підгрупа з ID {subgroupId} не існує або не належить до вибраних груп" });
                    }
                }
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


        // GET: /Event/Edit/{id} - Модальне вікно
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditModal(int id)
        {
            var eventItem = await _context.Events
                .Include(e => e.EventGroups)
                .Include(e => e.SubgroupEvents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (eventItem == null)
                return NotFound();

            // Доступ для адміністраторів або для викладачів до власних подій
            if (!User.IsInRole("Administrator"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

                if (teacher == null || teacher.Id != eventItem.TeacherId)
                {
                    return Forbid();
                }

                // Перевірка, що викладач має доступ до курсу події
                var hasCourseAccess = await _context.CourseTeachers
                    .AnyAsync(ct => ct.CourseId == eventItem.CourseId && ct.TeacherId == teacher.Id);

                if (!hasCourseAccess)
                {
                    return Forbid();
                }
            }

            // Підготовка даних для вибору
            var teachers = await _context.Teachers.ToListAsync();
            var teacherItems = teachers.Select(t => new
            {
                Id = t.Id,
                Name = $"{t.LastName} {t.FirstName}"
            });

            // Для викладачів обмежуємо вибір тільки собою
            if (User.IsInRole("Teacher") && !User.IsInRole("Administrator"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

                if (teacher != null)
                {
                    teacherItems = teacherItems.Where(t => t.Id == teacher.Id);
                }
            }

            ViewBag.Teachers = new SelectList(teacherItems, "Id", "Name", eventItem.TeacherId);

            // Для викладачів обмежуємо вибір курсів, до яких вони мають доступ
            if (User.IsInRole("Teacher") && !User.IsInRole("Administrator"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

                if (teacher != null)
                {
                    var teacherCourses = await _context.CourseTeachers
                        .Where(ct => ct.TeacherId == teacher.Id)
                        .Select(ct => ct.Course)
                        .ToListAsync();

                    ViewBag.Courses = new SelectList(teacherCourses, "Id", "Name", eventItem.CourseId);
                }
                else
                {
                    ViewBag.Courses = new SelectList(new List<Course>(), "Id", "Name");
                }
            }
            else
            {
                ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name", eventItem.CourseId);
            }

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
        [Authorize]
        public async Task<IActionResult> EditModal(int id, string Title, DateTime StartDateTime,
            DateTime EndDateTime, int TeacherId, int CourseId, int ClassroomId, int ScheduleId,
            bool IsRecurring, string RecurrencePattern, int[] selectedGroups, int[] selectedSubgroups)
        {
            var eventInDb = await _context.Events
                .Include(e => e.EventGroups)
                .Include(e => e.SubgroupEvents)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventInDb == null)
                return Json(new { success = false, message = "Подію не знайдено." });

            // Доступ для адміністраторів або для викладачів до власних подій
            if (!User.IsInRole("Administrator"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

                if (teacher == null || teacher.Id != eventInDb.TeacherId)
                {
                    return Json(new { success = false, message = "У вас немає прав на редагування цієї події" });
                }

                // Перевіряємо, що викладач не намагається змінити TeacherId
                if (TeacherId != teacher.Id)
                {
                    return Json(new { success = false, message = "Ви не можете змінити викладача події" });
                }

                // Перевірка, що викладач має доступ до вибраного курсу
                var hasCourseAccess = await _context.CourseTeachers
                    .AnyAsync(ct => ct.CourseId == CourseId && ct.TeacherId == teacher.Id);

                if (!hasCourseAccess)
                {
                    return Json(new { success = false, message = "Ви можете обирати тільки свої курси" });
                }
            }

            // Перевіряємо наявність обов'язкових полів
            if (string.IsNullOrEmpty(Title) || TeacherId == 0 || CourseId == 0 ||
                ClassroomId == 0 || ScheduleId == 0)
            {
                var errorList = new List<object>();

                if (TeacherId == 0)
                    errorList.Add(new { key = "Teacher", errors = new[] { "Поле Викладач є обов'язковим." } });

                if (CourseId == 0)
                    errorList.Add(new { key = "Course", errors = new[] { "Поле Курс є обов'язковим." } });

                if (ClassroomId == 0)
                    errorList.Add(new { key = "Classroom", errors = new[] { "Поле Аудиторія є обов'язковим." } });

                if (ScheduleId == 0)
                    errorList.Add(new { key = "Schedule", errors = new[] { "Поле Розклад є обов'язковим." } });

                return Json(new { success = false, message = "Невалідна модель", errors = errorList });
            }

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

        // GET: /Event/Delete/{id} - Модальне вікно
        [HttpGet]
        [Authorize]
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

            // Доступ для адміністраторів або для викладачів до власних подій
            if (!User.IsInRole("Administrator"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

                if (teacher == null || teacher.Id != eventItem.TeacherId)
                {
                    return Forbid();
                }
            }

            return PartialView("_DeleteModal", eventItem);
        }

        // POST: /Event/DeleteConfirmed/{id} - AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var eventItem = await _context.Events
                    .Include(e => e.EventGroups)
                    .Include(e => e.SubgroupEvents)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (eventItem == null)
                    return Json(new { success = false, message = "Подію не знайдено." });

                // Доступ для адміністраторів або для викладачів до власних подій
                if (!User.IsInRole("Administrator"))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

                    if (teacher == null || teacher.Id != eventItem.TeacherId)
                    {
                        return Json(new { success = false, message = "У вас немає прав на видалення цієї події" });
                    }
                }

                // Видаляємо всі зв'язки з групами
                _context.EventGroups.RemoveRange(eventItem.EventGroups);

                // Видаляємо всі зв'язки з підгрупами
                _context.SubgroupEvents.RemoveRange(eventItem.SubgroupEvents);

                // Видаляємо саму подію
                _context.Events.Remove(eventItem);

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Подію успішно видалено!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Помилка при видаленні події: " + ex.Message });
            }
        }

        // Допоміжний метод для перевірки існування події
        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }
    }
}