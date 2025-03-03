using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
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

        // GET: /Schedule
        public async Task<IActionResult> Index()
        {
            var schedules = await _context.Schedules
                .Include(s => s.Groups)
                .Include(s => s.Teacher)
                .Include(s => s.Course)
                .Include(s => s.Classroom)
                .ToListAsync();
            return View(schedules);
        }

        // GET: /Schedule/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Groups)
                .Include(s => s.Teacher)
                .Include(s => s.Course)
                .Include(s => s.Classroom)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (schedule == null)
                return NotFound();
            return View(schedule);
        }

        // GET: /Schedule/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Schedule/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Schedule schedule)
        {
            if (ModelState.IsValid)
            {
                _context.Add(schedule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(schedule);
        }

        // GET: /Schedule/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
                return NotFound();
            return View(schedule);
        }

        // POST: /Schedule/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Schedule schedule)
        {
            if (id != schedule.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(schedule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(schedule);
        }

        // POST: /Schedule/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule != null)
            {
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
