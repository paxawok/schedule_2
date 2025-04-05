using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System;

namespace schedule_2.Controllers
{
    [Authorize] // Дозволити доступ тільки авторизованим користувачам
    public class ClassroomController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ClassroomController(
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

        // GET: /Classroom/Index
        public async Task<IActionResult> Index()
        {
            var classrooms = await _context.Classrooms
                .Include(c => c.Events)
                .ToListAsync();

            // Передаємо інформацію про роль користувача у ViewBag
            ViewBag.IsAdministrator = await IsAdministratorAsync();

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
        [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть створювати аудиторії
        public IActionResult Create()
        {
            return PartialView("_CreateModal");
        }

        // POST: /Classroom/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть створювати аудиторії
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
        [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть редагувати аудиторії
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
        [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть редагувати аудиторії
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
            return Json(new { success = false, message = "Невірні дані форми." });
        }

        // GET: /Classroom/Delete/{id} (Partial View для модального вікна підтвердження)
        [HttpGet]
        [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть видаляти аудиторії
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
        [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть видаляти аудиторії
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var classroom = await _context.Classrooms
                .Include(c => c.Events)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classroom == null)
                return Json(new { success = false });

            // Перевірка, чи є пов'язані події
            if (classroom.Events.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Неможливо видалити аудиторію, оскільки вона використовується в подіях. Спочатку видаліть або змініть пов'язані події."
                });
            }

            _context.Classrooms.Remove(classroom);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Аудиторія успішно видалена." });
        }
    }
}