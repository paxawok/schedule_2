using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using schedule_2.Data;
using schedule_2.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
            // Log received data for debugging
            Console.WriteLine($"Received subgroup data - Name: {subgroup.Name}, GroupId: {subgroup.GroupId}");

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
                try
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

                    return Json(new { success = true, message = "Subgroup created successfully!" });
                }
                catch (Exception ex)
                {
                    // Log any exceptions
                    Console.WriteLine($"Error creating subgroup: {ex.Message}");
                    return Json(new { success = false, message = $"Error creating subgroup: {ex.Message}" });
                }
            }

            // If validation fails, return errors
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            Console.WriteLine($"Validation errors: {string.Join(", ", errors)}");
            return Json(new { success = false, message = "Invalid data. Please check the form.", errors });
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