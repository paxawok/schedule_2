using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using System.Linq;
using System.Threading.Tasks;

namespace schedule_2.Controllers
{
    public class ClassroomController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClassroomController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Classroom/Index
        public async Task<IActionResult> Index()
        {
            var classrooms = await _context.Classrooms
                .Include(c => c.Events)
                .ToListAsync();
            return View(classrooms);
        }

        // GET: /Classroom/Details/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> DetailsModal(int id)
        {
            var classroom = await _context.Classrooms
                .Include(c => c.Events)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classroom == null)
                return NotFound();

            return PartialView("_DetailsModal", classroom);
        }

        // GET: /Classroom/Create
        [HttpGet]
        public IActionResult Create()
        {
            return PartialView("_CreateModal");
        }

        // POST: /Classroom/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Classroom classroom)
        {
            if (ModelState.IsValid)
            {
                _context.Classrooms.Add(classroom);

                try
                {
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Аудиторія успішно створена!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Сталася помилка при створенні аудиторії: " + ex.Message });
                }
            }

            return Json(new { success = false, message = "Невірні дані. Перевірте форму." });
        }

        // GET: /Classroom/Edit/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            var classroom = await _context.Classrooms
                .Include(c => c.Events)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classroom == null)
                return NotFound();

            return PartialView("_EditModal", classroom);
        }

        // POST: /Classroom/Edit/{id} (AJAX для оновлення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModal(int id, Classroom classroom)
        {
            if (id != classroom.Id)
                return Json(new { success = false, message = "ID не співпадає." });

            if (ModelState.IsValid)
            {
                try
                {
                    var classroomInDb = await _context.Classrooms
                        .Include(c => c.Events)
                        .FirstOrDefaultAsync(c => c.Id == id);

                    if (classroomInDb == null)
                        return Json(new { success = false, message = "Аудиторія не знайдена." });

                    classroomInDb.Name = classroom.Name;
                    classroomInDb.Capacity = classroom.Capacity;

                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Дані успішно оновлено." });
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Json(new { success = false, message = "Помилка оновлення даних." });
                }
            }
            return PartialView("_EditModal", classroom);
        }

        // GET: /Classroom/Delete/{id} (Partial View для модального вікна підтвердження)
        [HttpGet]
        public async Task<IActionResult> DeleteModal(int id)
        {
            var classroom = await _context.Classrooms
                .Include(c => c.Events)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classroom == null)
                return NotFound();

            return PartialView("_DeleteModal", classroom);
        }

        // POST: /Classroom/DeleteConfirmed/{id} (AJAX для видалення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var classroom = await _context.Classrooms
                .Include(c => c.Events)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classroom == null)
                return Json(new { success = false });

            foreach (var eventItem in classroom.Events.ToList())
            {
                _context.Events.Remove(eventItem);
            }

            _context.Classrooms.Remove(classroom);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Аудиторія успішно видалена." });
        }
    }
}

