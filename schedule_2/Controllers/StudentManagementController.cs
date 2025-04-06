using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using schedule_2.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace schedule_2.Controllers
{
    [Authorize]
    public class StudentManagementController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public StudentManagementController(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: /StudentManagement/Index
        public async Task<IActionResult> Index()
        {
            // Якщо це адміністратор, показуємо всіх студентів
            if (User.IsInRole("Administrator"))
            {
                var students = await _context.Set<Student>()
                    .Include(s => s.Group)
                    .ToListAsync();
                return View(students);
            }
            // Якщо це викладач, показуємо лише студентів з груп, до яких у нього є доступ
            else if (User.IsInRole("Teacher"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

                if (teacher == null)
                    return NotFound();

                // Отримуємо ID груп, які має цей викладач
                var teacherCourses = await _context.CourseTeachers
                    .Where(ct => ct.TeacherId == teacher.Id)
                    .Select(ct => ct.CourseId)
                    .ToListAsync();

                var groupIds = await _context.CourseGroups
                    .Where(cg => teacherCourses.Contains(cg.CourseId))
                    .Select(cg => cg.GroupId)
                    .Distinct()
                    .ToListAsync();

                // Отримуємо студентів з цих груп
                var students = await _context.Set<Student>()
                    .Include(s => s.Group)
                    .Where(s => groupIds.Contains(s.GroupId))
                    .ToListAsync();

                return View(students);
            }
            // Якщо це студент, показуємо тільки його профіль
            else if (User.IsInRole("Student"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var student = await _context.Set<Student>()
                    .Include(s => s.Group)
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                    return NotFound();

                var students = new List<Student> { student };
                return View(students);
            }

            // Якщо роль не визначена
            return Forbid();
        }

        // GET: /StudentManagement/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var student = await _context.Set<Student>()
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound();

            // Перевірка прав доступу
            bool canView = await CanAccessStudent(student);
            if (!canView)
                return Forbid();

            return View(student);
        }

        // GET: /StudentManagement/Edit/{id}
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int id)
        {
            var student = await _context.Set<Student>()
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound();

            var groups = await _context.Groups.ToListAsync();
            ViewBag.Groups = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(groups, "Id", "Name", student.GroupId);

            return View(student);
        }

        // POST: /StudentManagement/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int id, Student student)
        {
            if (id != student.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var studentInDb = await _context.Set<Student>().FindAsync(id);
                    if (studentInDb == null)
                        return NotFound();

                    // Оновлюємо необхідні поля
                    studentInDb.FirstName = student.FirstName;
                    studentInDb.LastName = student.LastName;
                    studentInDb.GroupId = student.GroupId;

                    // Email можна змінити, але потрібно також оновити в Identity
                    if (studentInDb.Email != student.Email)
                    {
                        var user = await _userManager.FindByIdAsync(studentInDb.UserId);
                        if (user != null)
                        {
                            user.Email = student.Email;
                            user.UserName = student.Email;
                            await _userManager.UpdateAsync(user);
                        }

                        studentInDb.Email = student.Email;
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            var groups = await _context.Groups.ToListAsync();
            ViewBag.Groups = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(groups, "Id", "Name", student.GroupId);
            return View(student);
        }

        // GET: /StudentManagement/Delete/{id}
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _context.Set<Student>()
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound();

            return View(student);
        }

        // POST: /StudentManagement/DeleteConfirmed/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Set<Student>().FindAsync(id);
            if (student == null)
                return NotFound();

            // Видаляємо користувача Identity
            var user = await _userManager.FindByIdAsync(student.UserId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            // Видаляємо студента
            _context.Set<Student>().Remove(student);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Допоміжний метод для перевірки доступу до студента
        private async Task<bool> CanAccessStudent(Student student)
        {
            // Адміністратор має доступ до всіх студентів
            if (User.IsInRole("Administrator"))
                return true;

            // Якщо поточний користувач - це сам студент
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (student.UserId == userId)
                return true;

            // Якщо це викладач, перевіряємо чи має він доступ до групи цього студента
            if (User.IsInRole("Teacher"))
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
                if (teacher == null)
                    return false;

                // Перевіряємо, чи є у викладача курси для групи студента
                var courseIds = await _context.CourseTeachers
                    .Where(ct => ct.TeacherId == teacher.Id)
                    .Select(ct => ct.CourseId)
                    .ToListAsync();

                return await _context.CourseGroups
                    .AnyAsync(cg => courseIds.Contains(cg.CourseId) && cg.GroupId == student.GroupId);
            }

            return false;
        }

        // Перевірка існування студента
        private bool StudentExists(int id)
        {
            return _context.Set<Student>().Any(e => e.Id == id);
        }
    }
}