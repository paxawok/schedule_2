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

            // Підготовка даних для представлення
            ViewBag.Groups = new SelectList(await _context.Groups.OrderBy(g => g.Name).ToListAsync(), "Id", "Name", subgroup.GroupId);
            ViewBag.Courses = await _context.Courses.OrderBy(c => c.Name).ToListAsync();

            // Отримуємо ID курсів, пов'язаних з цією підгрупою
            ViewBag.SelectedCourses = subgroup.SubgroupCourses
                .Select(sc => sc.CourseId)
                .ToList();

            return PartialView("_EditModal", subgroup);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModal(int id, Subgroup subgroup, int[] selectedCourses)
        {
            if (id != subgroup.Id)
                return Json(new { success = false, message = "ID не співпадає." });

            // Валідація даних
            if (string.IsNullOrWhiteSpace(subgroup.Name))
            {
                return Json(new { success = false, message = "Назва підгрупи обов'язкова." });
            }

            if (subgroup.GroupId <= 0)
            {
                return Json(new { success = false, message = "Потрібно вибрати групу." });
            }

            // Перевірка на дублікати
            var duplicateName = await _context.Subgroups
                .AnyAsync(s => s.GroupId == subgroup.GroupId && s.Name == subgroup.Name && s.Id != id);

            if (duplicateName)
            {
                return Json(new { success = false, message = "Підгрупа з такою назвою вже існує в цій групі." });
            }

            try
            {
                // Отримуємо існуючу підгрупу з бази
                var subgroupToUpdate = await _context.Subgroups
                    .Include(s => s.SubgroupCourses)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (subgroupToUpdate == null)
                    return Json(new { success = false, message = "Підгрупу не знайдено." });

                // Оновлення базових властивостей
                subgroupToUpdate.Name = subgroup.Name;
                subgroupToUpdate.GroupId = subgroup.GroupId;

                // Починаємо транзакцію для забезпечення цілісності даних
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Видаляємо всі існуючі зв'язки з курсами
                        _context.SubgroupCourses.RemoveRange(subgroupToUpdate.SubgroupCourses);
                        await _context.SaveChangesAsync();

                        // Додаємо нові зв'язки з курсами
                        if (selectedCourses != null && selectedCourses.Length > 0)
                        {
                            foreach (var courseId in selectedCourses)
                            {
                                _context.SubgroupCourses.Add(new SubgroupCourse
                                {
                                    SubgroupId = id,
                                    CourseId = courseId
                                });
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return Json(new { success = true, message = "Підгрупу успішно оновлено." });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw; // Повторно кидаємо виняток для зовнішнього обробника
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Помилка при оновленні підгрупи: {ex.Message}" });
            }
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

                        // Отримуємо всіх студентів, пов'язаних з цією групою
                        var students = await _context.Students
                            .Where(s => s.GroupId == groupId)
                            .ToListAsync();

                        if (students.Any() && createdSubgroups.Any())
                        {
                            // Перемішуємо список студентів для рандомного розподілу
                            var random = new Random();
                            var shuffledStudents = students.OrderBy(s => random.Next()).ToList();

                            // Розраховуємо кількість студентів у кожній підгрупі
                            int studentsPerSubgroup = shuffledStudents.Count / createdSubgroups.Count;
                            int remainingStudents = shuffledStudents.Count % createdSubgroups.Count;

                            int currentStudentIndex = 0;

                            // Розподіляємо студентів по підгрупах
                            for (int i = 0; i < createdSubgroups.Count; i++)
                            {
                                var subgroup = createdSubgroups[i];

                                // Кількість студентів для цієї підгрупи
                                // Якщо є залишок, додаємо по одному додатковому студенту
                                int subgroupSize = studentsPerSubgroup + (i < remainingStudents ? 1 : 0);

                                for (int j = 0; j < subgroupSize && currentStudentIndex < shuffledStudents.Count; j++)
                                {
                                    var student = shuffledStudents[currentStudentIndex++];

                                    // Створюємо зв'язок між студентом і підгрупою
                                    _context.StudentSubgroups.Add(new StudentSubgroup
                                    {
                                        StudentId = student.Id,
                                        SubgroupId = subgroup.Id
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