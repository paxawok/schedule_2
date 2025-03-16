using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using System;
using System.Linq;
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
    }
}
