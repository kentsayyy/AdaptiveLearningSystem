using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AdaptiveLearningSystem.Models;
using AdaptiveLearningSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AdaptiveLearningSystem.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class StudentsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public StudentsController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Admins see all students
            if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                var students = await _userManager.GetUsersInRoleAsync("Student");
                if (!string.IsNullOrEmpty(search))
                    students = students
                        .Where(s => s.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                                 || s.Email!.Contains(search, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                ViewBag.Search = search;
                return View(students);
            }

            // Teachers: show only students enrolled in modules assigned to this teacher
            if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Teacher"))
            {
                var moduleIds = await _db.LearningModules
                    .Where(m => m.TeacherId == currentUser.Id)
                    .Select(m => m.ModuleId)
                    .ToListAsync();

                if (!moduleIds.Any())
                {
                    ViewBag.Search = search;
                    return View(new List<ApplicationUser>());
                }

                var studentIds = await _db.Enrollments
                    .Where(e => moduleIds.Contains(e.ModuleId))
                    .Select(e => e.UserId)
                    .Distinct()
                    .ToListAsync();

                var allStudents = await _userManager.GetUsersInRoleAsync("Student");
                var students = allStudents.Where(s => studentIds.Contains(s.Id)).ToList();

                if (!string.IsNullOrEmpty(search))
                    students = students
                        .Where(s => s.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                                 || s.Email!.Contains(search, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                ViewBag.Search = search;
                return View(students);
            }

            // Fallback: no students
            ViewBag.Search = search;
            return View(new List<ApplicationUser>());
        }

        public async Task<IActionResult> Details(string id)
        {
            var student = await _userManager.FindByIdAsync(id);
            if (student == null) return NotFound();
            return View(student);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var student = await _userManager.FindByIdAsync(id);
            if (student == null) return NotFound();
            return View(student);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ApplicationUser model)
        {
            var student = await _userManager.FindByIdAsync(id);
            if (student == null) return NotFound();
            student.FullName = model.FullName;
            student.Email = model.Email;
            student.UserName = model.Email;
            await _userManager.UpdateAsync(student);
            TempData["Success"] = "Student updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var student = await _userManager.FindByIdAsync(id);
            if (student != null) await _userManager.DeleteAsync(student);
            TempData["Success"] = "Student deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
