using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AdaptiveLearningSystem.Models;

namespace AdaptiveLearningSystem.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class StudentsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentsController(UserManager<ApplicationUser> userManager)
            => _userManager = userManager;

        public async Task<IActionResult> Index(string? search)
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
