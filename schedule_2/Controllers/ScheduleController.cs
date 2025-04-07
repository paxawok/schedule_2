using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace schedule_2.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Schedule/Index
        public async Task<IActionResult> Index()
        {
            var schedules = await _context.Schedules
                .Include(s => s.Events)
                .ToListAsync();
            return View(schedules);
        }

        // GET: /Schedule/Details/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> DetailsModal(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Events)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (schedule == null)
                return NotFound();

            return PartialView("_DetailsModal", schedule);
        }

        // GET: /Schedule/Create
        [HttpGet]
        public IActionResult Create()
        {
            return PartialView("_CreateModal");
        }

        // POST: /Schedule/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Schedule schedule)
        {
            if (ModelState.IsValid)
            {
                _context.Schedules.Add(schedule);

                try
                {
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Розклад успішно створено!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Сталася помилка при створенні розкладу: " + ex.Message });
                }
            }

            return Json(new { success = false, message = "Невірні дані. Перевірте форму." });
        }

        // GET: /Schedule/Edit/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Events)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (schedule == null)
                return NotFound();

            return PartialView("_EditModal", schedule);
        }

        // POST: /Schedule/Edit/{id} (AJAX для оновлення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModal(int id, Schedule schedule)
        {
            if (id != schedule.Id)
                return Json(new { success = false, message = "ID не співпадає." });

            if (ModelState.IsValid)
            {
                try
                {
                    var scheduleInDb = await _context.Schedules
                        .Include(s => s.Events)
                        .FirstOrDefaultAsync(s => s.Id == id);

                    if (scheduleInDb == null)
                        return Json(new { success = false, message = "Розклад не знайдений." });

                    scheduleInDb.Name = schedule.Name;
                    scheduleInDb.StartDate = schedule.StartDate;
                    scheduleInDb.EndDate = schedule.EndDate;

                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Дані успішно оновлено." });
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Json(new { success = false, message = "Помилка оновлення даних." });
                }
            }
            return PartialView("_EditModal", schedule);
        }

        // GET: /Schedule/Delete/{id} (Partial View для модального вікна підтвердження)
        [HttpGet]
        public async Task<IActionResult> DeleteModal(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Events)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (schedule == null)
                return NotFound();

            return PartialView("_DeleteModal", schedule);
        }

        // POST: /Schedule/DeleteConfirmed/{id} (AJAX для видалення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Events)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (schedule == null)
                return Json(new { success = false });

            foreach (var eventItem in schedule.Events.ToList())
            {
                _context.Events.Remove(eventItem);
            }

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Розклад успішно видалено." });
        }

        // GET: /Schedule/Weekly/{id}
        public async Task<IActionResult> Weekly(int id, DateTime? date = null, bool showOnlyMine = false, int subgroupId = 0)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Events)
                    .ThenInclude(e => e.Teacher)
                .Include(s => s.Events)
                    .ThenInclude(e => e.Course)
                .Include(s => s.Events)
                    .ThenInclude(e => e.Classroom)
                .Include(s => s.Events)
                    .ThenInclude(e => e.EventGroups)
                        .ThenInclude(eg => eg.Group)
                .Include(s => s.Events)
                    .ThenInclude(e => e.SubgroupEvents)
                        .ThenInclude(se => se.Subgroup)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (schedule == null)
                return NotFound();

            // Якщо дата не вказана, використовуємо поточну дату
            DateTime currentDate = date ?? DateTime.Today;

            // Розрахуємо початок тижня (понеділок)
            int diff = (7 + (currentDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startDay = currentDate.AddDays(-diff);

            // Створюємо список днів тижня
            var weekDays = Enumerable.Range(0, 7).Select(i => startDay.AddDays(i)).ToList();

            // Визначаємо часові слоти (пари)
            var timeStart = new TimeSpan(9, 0, 0);
            var timeEnd = new TimeSpan(19, 30, 0);
            var lessonDuration = 80;
            var breakDuration = 10;
            var longBreakDuration = 20;

            var timeSlots = new List<(TimeSpan Start, TimeSpan End, string Label)>();
            var currentTime = timeStart;
            int lessonCount = 0;

            while (currentTime < timeEnd)
            {
                var endTime = currentTime.Add(TimeSpan.FromMinutes(lessonDuration));
                var label = $"{currentTime.ToString(@"hh\:mm")} - {endTime.ToString(@"hh\:mm")}";
                timeSlots.Add((currentTime, endTime, label));

                lessonCount++;
                int currentBreakDuration = (lessonCount == 2) ? longBreakDuration : breakDuration;
                currentTime = endTime.Add(TimeSpan.FromMinutes(currentBreakDuration));
            }

            // Створюємо структуру даних для розкладу
            var weeklySchedule = new Dictionary<DateTime, Dictionary<TimeSpan, List<Event>>>();

            foreach (var day in weekDays)
            {
                weeklySchedule[day] = new Dictionary<TimeSpan, List<Event>>();
                foreach (var slot in timeSlots)
                {
                    weeklySchedule[day][slot.Start] = new List<Event>();
                }
            }

            // Початковий список подій
            var eventsToDisplay = schedule.Events.ToList();

            // Отримуємо поточного користувача
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Визначаємо ролі користувача
            bool isTeacher = User.IsInRole("Teacher");
            bool isStudent = User.IsInRole("Student");
            Teacher currentTeacher = null;
            Group studentGroup = null;
            List<Subgroup> groupSubgroups = new List<Subgroup>();

            // Фільтрація для викладачів
            if (isTeacher && showOnlyMine)
            {
                currentTeacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

                if (currentTeacher != null)
                {
                    eventsToDisplay = eventsToDisplay.Where(e => e.TeacherId == currentTeacher.Id).ToList();
                }
            }
            // Фільтрація для студентів: показуємо тільки події їхньої групи та підгрупи, якщо вибрано
            else if (isStudent)
            {
                // Отримуємо дані студента і його групу
                var student = await _context.Set<Student>()
                    .Include(s => s.Group)
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student != null)
                {
                    // Зберігаємо групу студента
                    studentGroup = student.Group;
                    int studentGroupId = student.GroupId;

                    // Отримуємо підгрупи групи студента
                    groupSubgroups = await _context.Subgroups
                        .Where(sg => sg.GroupId == studentGroupId)
                        .ToListAsync();

                    // Спершу фільтруємо події за групою студента
                    eventsToDisplay = eventsToDisplay.Where(e =>
                        e.EventGroups.Any(eg => eg.GroupId == studentGroupId)).ToList();

                    // Якщо обрана конкретна підгрупа (subgroupId > 0) і у групи є підгрупи
                    if (subgroupId > 0 && groupSubgroups.Any())
                    {
                        // Фільтруємо події, які:
                        // 1. Або призначені для обраної підгрупи
                        // 2. Або не призначені для жодної підгрупи (тобто для всієї групи)
                        eventsToDisplay = eventsToDisplay.Where(e =>
                            e.SubgroupEvents.Any(se => se.SubgroupId == subgroupId) ||
                            !e.SubgroupEvents.Any()).ToList();
                    }
                }
            }

            // Заповнюємо розклад подіями
            foreach (var eventItem in eventsToDisplay)
            {
                // Перевіряємо, чи потрапляє подія в поточний тиждень
                if (eventItem.StartDateTime.Date >= weekDays.First().Date &&
                    eventItem.StartDateTime.Date <= weekDays.Last().Date)
                {
                    // Знаходимо найближчий часовий слот
                    var eventDate = eventItem.StartDateTime.Date;
                    var nearestSlot = timeSlots
                        .OrderBy(s => Math.Abs((eventItem.StartDateTime.TimeOfDay - s.Start).TotalMinutes))
                        .First().Start;

                    if (weeklySchedule.ContainsKey(eventDate) && weeklySchedule[eventDate].ContainsKey(nearestSlot))
                    {
                        weeklySchedule[eventDate][nearestSlot].Add(eventItem);
                    }
                }
            }

            // Передаємо всі необхідні дані у ViewBag
            ViewBag.WeekDays = weekDays;
            ViewBag.TimeSlots = timeSlots;
            ViewBag.WeeklySchedule = weeklySchedule;
            ViewBag.CurrentDate = currentDate;
            ViewBag.PreviousWeek = startDay.AddDays(-7);
            ViewBag.NextWeek = startDay.AddDays(7);
            ViewBag.ShowOnlyMine = showOnlyMine;
            ViewBag.SelectedSubgroupId = subgroupId;

            // Ролі користувача
            ViewBag.IsTeacher = isTeacher;
            ViewBag.IsStudent = isStudent;

            // Дані про користувача
            ViewBag.CurrentTeacher = currentTeacher;
            ViewBag.StudentGroup = studentGroup;
            ViewBag.GroupSubgroups = groupSubgroups;

            return View(schedule);
        }
    }
}
