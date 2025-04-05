using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace schedule_2.Controllers
{
    [Authorize]
    public class SubgroupController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubgroupController> _logger;

        public SubgroupController(ApplicationDbContext context, ILogger<SubgroupController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Subgroup/Index
        public async Task<IActionResult> Index()
        {
            var subgroups = await _context.Subgroups
                .Include(s => s.Group)
                .Include(s => s.SubgroupCourses)
                    .ThenInclude(sc => sc.Course)
                .Include(s => s.SubgroupEvents)
                    .ThenInclude(se => se.Event)
                .OrderBy(s => s.Group.Name)
                .ThenBy(s => s.Name)
                .ToListAsync();
            return View(subgroups);
        }

        // GET: /Subgroup/Details/{id} (Partial View for modal window)
        [HttpGet]
        public async Task<IActionResult> DetailsModal(int id)
        {
            var subgroup = await _context.Subgroups
                .Include(s => s.Group)
                .Include(s => s.SubgroupCourses)
                    .ThenInclude(sc => sc.Course)
                .Include(s => s.SubgroupEvents)
                    .ThenInclude(se => se.Event)
                        .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (subgroup == null)
                return NotFound();

            return PartialView("_DetailsModal", subgroup);
        }

        // GET: /Subgroup/Create
        [HttpGet]
        public IActionResult Create()
        {
            PrepareViewBagForCreate();
            return PartialView("_CreateModal");
        }

        // POST: /Subgroup/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Subgroup subgroup, [FromForm] int[] selectedCourses)
        {
            try
            {
                // Server-side validation
                if (string.IsNullOrWhiteSpace(subgroup.Name))
                {
                    ModelState.AddModelError("Name", "Name is required");
                }
                else if (subgroup.Name.Length > 18)
                {
                    ModelState.AddModelError("Name", "Name cannot exceed 18 characters");
                }

                if (subgroup.GroupId == 0)
                {
                    ModelState.AddModelError("GroupId", "Group is required");
                }

                // Check for duplicate subgroup name within the same group
                var duplicateName = await _context.Subgroups
                    .AnyAsync(s => s.GroupId == subgroup.GroupId && s.Name == subgroup.Name);

                if (duplicateName)
                {
                    ModelState.AddModelError("Name", "A subgroup with this name already exists in the selected group");
                }

                if (ModelState.IsValid)
                {
                    _context.Subgroups.Add(subgroup);
                    await _context.SaveChangesAsync();

                    // Add course relationships
                    if (selectedCourses != null && selectedCourses.Length > 0)
                    {
                        foreach (var courseId in selectedCourses)
                        {
                            _context.SubgroupCourses.Add(new SubgroupCourse
                            {
                                SubgroupId = subgroup.Id,
                                CourseId = courseId
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    return Json(new { success = true, message = "Підгрупу успішно створено!" });
                }

                // If validation fails, return errors
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Json(new { success = false, message = "Невірні дані. Перевірте форму.", errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при створенні підгрупи");
                return Json(new { success = false, message = $"Помилка при створенні підгрупи: {ex.Message}" });
            }
        }

        // GET: /Subgroup/Edit/{id} (Partial View for modal window)
        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            var subgroup = await _context.Subgroups
                .Include(s => s.Group)
                .Include(s => s.SubgroupCourses)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (subgroup == null)
                return NotFound();

            PrepareViewBagForEdit(subgroup);
            return PartialView("_EditModal", subgroup);
        }

        // POST: /Subgroup/Edit/{id} (AJAX for updating via modal window)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModal(int id, [FromForm] Subgroup subgroup, [FromForm] int[] selectedCourses)
        {
            if (id != subgroup.Id)
                return Json(new { success = false, message = "ID mismatch." });

            // Server-side validation
            if (string.IsNullOrWhiteSpace(subgroup.Name))
            {
                ModelState.AddModelError("Name", "Name is required");
            }
            else if (subgroup.Name.Length > 18)
            {
                ModelState.AddModelError("Name", "Name cannot exceed 18 characters");
            }

            if (subgroup.GroupId == 0)
            {
                ModelState.AddModelError("GroupId", "Group is required");
            }

            // Check for duplicate subgroup name within the same group (excluding current subgroup)
            var duplicateName = await _context.Subgroups
                .AnyAsync(s => s.GroupId == subgroup.GroupId && s.Name == subgroup.Name && s.Id != id);

            if (duplicateName)
            {
                ModelState.AddModelError("Name", "A subgroup with this name already exists in the selected group");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var subgroupToUpdate = await _context.Subgroups
                        .Include(s => s.SubgroupCourses)
                        .FirstOrDefaultAsync(s => s.Id == id);

                    if (subgroupToUpdate == null)
                        return Json(new { success = false, message = "Subgroup not found." });

                    // Update basic properties
                    subgroupToUpdate.Name = subgroup.Name;
                    subgroupToUpdate.GroupId = subgroup.GroupId;

                    // Update course relationships
                    // First, remove existing relationships
                    _context.SubgroupCourses.RemoveRange(subgroupToUpdate.SubgroupCourses);

                    // Then add new relationships
                    if (selectedCourses != null && selectedCourses.Length > 0)
                    {
                        foreach (var courseId in selectedCourses)
                        {
                            _context.SubgroupCourses.Add(new SubgroupCourse
                            {
                                SubgroupId = subgroup.Id,
                                CourseId = courseId
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Data updated successfully." });
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Json(new { success = false, message = "Error updating data. The record may have been modified by another user." });
                }
            }

            // If validation fails, return errors
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return Json(new { success = false, message = "Invalid data. Please check the form.", errors });
        }

        // GET: /Subgroup/Delete/{id} (Partial View for confirmation modal)
        [HttpGet]
        public async Task<IActionResult> DeleteModal(int id)
        {
            var subgroup = await _context.Subgroups
                .Include(s => s.Group)
                .Include(s => s.SubgroupCourses)
                    .ThenInclude(sc => sc.Course)
                .Include(s => s.SubgroupEvents)
                    .ThenInclude(se => se.Event)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (subgroup == null)
                return NotFound();

            return PartialView("_DeleteModal", subgroup);
        }

        // POST: /Subgroup/DeleteConfirmed/{id} (AJAX for deletion via modal window)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subgroup = await _context.Subgroups
                .Include(s => s.SubgroupCourses)
                .Include(s => s.SubgroupEvents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (subgroup == null)
                return Json(new { success = false, message = "Subgroup not found." });

            // Check if subgroup is used in events
            if (subgroup.SubgroupEvents.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Cannot delete this subgroup because it's used in one or more events. Please remove these relationships first."
                });
            }

            // Remove all related entities
            _context.SubgroupCourses.RemoveRange(subgroup.SubgroupCourses);
            _context.Subgroups.Remove(subgroup);

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Subgroup deleted successfully." });
        }

        // AJAX: Get subgroups for selected groups
        [HttpGet]
        public JsonResult GetSubgroupsByGroups(int[] groupIds)
        {
            if (groupIds == null || groupIds.Length == 0)
                return Json(new List<object>());

            var subgroups = _context.Subgroups
                .Where(s => groupIds.Contains(s.GroupId))
                .Select(s => new {
                    id = s.Id,
                    name = s.Name,
                    groupId = s.GroupId,
                    groupName = s.Group.Name
                })
                .OrderBy(s => s.groupName)
                .ThenBy(s => s.name)
                .ToList();

            return Json(subgroups);
        }

        [HttpGet]
        public async Task<IActionResult> DivideGroupModal(int id)
        {
            try
            {
                // Явно перевіряємо існування групи
                var group = await _context.Groups.FindAsync(id);

                if (group == null)
                {
                    _logger.LogWarning($"Групу з ідентифікатором {id} не знайдено");
                    return NotFound($"Групу з ідентифікатором {id} не знайдено");
                }

                // Перевірка на права доступу (опційно)
                bool isAdministrator = User.IsInRole("Administrator");
                bool isTeacher = User.IsInRole("Teacher");
                ViewBag.IsAdministrator = isAdministrator;
                ViewBag.IsTeacher = isTeacher;

                // Загальна підготовка представлення
                ViewBag.Courses = await _context.Courses.OrderBy(c => c.Name).ToListAsync();

                return PartialView("_DivideGroupModal", group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Помилка при завантаженні форми поділу групи: {ex.Message}");
                return Content($"Помилка при завантаженні форми поділу групи: {ex.Message}");
            }
        }

        // Updated DivideGroup method to properly handle selectedCourses
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DivideGroup(
     int groupId,
     int numberOfSubgroups,
     string prefixStyle,
     string[] subgroupNames,
     int[] selectedCourses)
        {
            try
            {
                // Перевірка, чи вибрана кількість підгруп
                if (numberOfSubgroups <= 0 || numberOfSubgroups > 10)
                {
                    return Json(new { success = false, message = "Недійсна кількість підгруп. Виберіть від 1 до 10." });
                }

                if (subgroupNames == null || subgroupNames.Length == 0)
                {
                    return Json(new { success = false, message = "Не вказано жодної назви для підгруп." });
                }

                // Пошук групи з явною перевіркою
                var group = await _context.Groups.FindAsync(groupId);
                if (group == null)
                {
                    return Json(new { success = false, message = "Батьківська група не знайдена." });
                }

                // Початок транзакції
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Створюємо нові підгрупи
                        var createdSubgroups = new List<Subgroup>();

                        foreach (var name in subgroupNames)
                        {
                            if (string.IsNullOrWhiteSpace(name))
                                continue;

                            var newSubgroup = new Subgroup
                            {
                                Name = name,
                                GroupId = groupId
                            };

                            _context.Subgroups.Add(newSubgroup);
                            await _context.SaveChangesAsync();

                            createdSubgroups.Add(newSubgroup);
                        }

                        // Додаємо зв'язки з курсами, якщо вони вибрані
                        if (selectedCourses != null && selectedCourses.Length > 0 && createdSubgroups.Any())
                        {
                            foreach (var subgroup in createdSubgroups)
                            {
                                foreach (var courseId in selectedCourses)
                                {
                                    _context.SubgroupCourses.Add(new SubgroupCourse
                                    {
                                        SubgroupId = subgroup.Id,
                                        CourseId = courseId
                                    });
                                }
                            }

                            await _context.SaveChangesAsync();
                        }

                        // Підтверджуємо транзакцію
                        await transaction.CommitAsync();

                        return Json(new
                        {
                            success = true,
                            message = $"Успішно створено {createdSubgroups.Count} підгруп для групи {group.Name}."
                        });
                    }
                    catch (Exception ex)
                    {
                        // Відкат при помилці
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Помилка при створенні підгруп: {ex.Message}");
                return Json(new { success = false, message = $"Помилка при створенні підгруп: {ex.Message}" });
            }
        }

        // Helper methods for ViewBag preparation
        private void PrepareViewBagForCreate()
        {
            ViewBag.Groups = new SelectList(_context.Groups.OrderBy(g => g.Name), "Id", "Name");
            ViewBag.Courses = _context.Courses.OrderBy(c => c.Name).ToList();
        }

        private void PrepareViewBagForEdit(Subgroup subgroup)
        {
            ViewBag.Groups = new SelectList(_context.Groups.OrderBy(g => g.Name), "Id", "Name", subgroup.GroupId);
            ViewBag.Courses = _context.Courses.OrderBy(c => c.Name).ToList();

            // Get IDs of courses associated with this subgroup
            ViewBag.SelectedCourses = subgroup.SubgroupCourses
                .Select(sc => sc.CourseId)
                .ToList();
        }
    }
}