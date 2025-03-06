using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace schedule_2.Controllers
{
    public class SubgroupController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SubgroupController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Subgroup/Index
        public async Task<IActionResult> Index()
        {
            var subgroups = await _context.Subgroups
                .Include(s => s.Group)
                .Include(s => s.SubgroupCourses)
                .Include(s => s.SubgroupEvents)
                .ToListAsync();
            return View(subgroups);
        }

        // GET: /Subgroup/Details/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> DetailsModal(int id)
        {
            var subgroup = await _context.Subgroups
                .Include(s => s.Group)
                .Include(s => s.SubgroupCourses)
                .Include(s => s.SubgroupEvents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (subgroup == null)
                return NotFound();

            return PartialView("_DetailsModal", subgroup);
        }

        // GET: /Subgroup/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.AllGroups = _context.Groups.ToList() ?? new List<Models.Group>();
            ViewBag.AllCourses = _context.Courses.ToList() ?? new List<Course>();
            ViewBag.AllEvents = _context.Events.ToList() ?? new List<Event>();

            return PartialView("_CreateModal");
        }

        // POST: /Subgroup/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Subgroup subgroup)
        {
            if (ModelState.IsValid)
            {
                _context.Subgroups.Add(subgroup);

                try
                {
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Підгрупу успішно створено!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Сталася помилка при створенні підгрупи: " + ex.Message });
                }
            }

            return Json(new { success = false, message = "Невірні дані. Перевірте форму." });
        }

        // GET: /Subgroup/Edit/{id} (Partial View для модального вікна)
        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            var subgroup = await _context.Subgroups
                .Include(s => s.Group)
                .Include(s => s.SubgroupCourses)
                .Include(s => s.SubgroupEvents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (subgroup == null)
                return NotFound();

            return PartialView("_EditModal", subgroup);
        }

        // POST: /Subgroup/Edit/{id} (AJAX для оновлення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModal(int id, Subgroup subgroup)
        {
            if (id != subgroup.Id)
                return Json(new { success = false, message = "ID не співпадає." });

            if (ModelState.IsValid)
            {
                try
                {
                    var subgroupInDb = await _context.Subgroups
                        .Include(s => s.Group)
                        .Include(s => s.SubgroupCourses)
                        .Include(s => s.SubgroupEvents)
                        .FirstOrDefaultAsync(s => s.Id == id);

                    if (subgroupInDb == null)
                        return Json(new { success = false, message = "Підгрупу не знайдено." });

                    subgroupInDb.Name = subgroup.Name;
                    subgroupInDb.GroupId = subgroup.GroupId;

                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Дані успішно оновлено." });
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Json(new { success = false, message = "Помилка оновлення даних." });
                }
            }
            return PartialView("_EditModal", subgroup);
        }

        // GET: /Subgroup/Delete/{id} (Partial View для модального вікна підтвердження)
        [HttpGet]
        public async Task<IActionResult> DeleteModal(int id)
        {
            var subgroup = await _context.Subgroups
                .Include(s => s.Group)
                .Include(s => s.SubgroupCourses)
                .Include(s => s.SubgroupEvents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (subgroup == null)
                return NotFound();

            return PartialView("_DeleteModal", subgroup);
        }

        // POST: /Subgroup/DeleteConfirmed/{id} (AJAX для видалення через модальне вікно)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subgroup = await _context.Subgroups
                .Include(s => s.SubgroupCourses)
                .Include(s => s.SubgroupEvents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (subgroup == null)
                return Json(new { success = false });

            foreach (var subgroupCourse in subgroup.SubgroupCourses.ToList())
            {
                _context.SubgroupCourses.Remove(subgroupCourse);
            }

            foreach (var subgroupEvent in subgroup.SubgroupEvents.ToList())
            {
                _context.SubgroupEvents.Remove(subgroupEvent);
            }

            _context.Subgroups.Remove(subgroup);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Підгрупу успішно видалено." });
        }
    }
}
