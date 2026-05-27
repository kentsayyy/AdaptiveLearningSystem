using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AdaptiveLearningSystem.Data;
using AdaptiveLearningSystem.Models;

namespace AdaptiveLearningSystem.Controllers
{
    [Authorize]
    public class EnrollmentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public EnrollmentsController(ApplicationDbContext db,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Index()
        {
            var enrollments = await _db.Enrollments
                .Include(e => e.User)
                .Include(e => e.Module)
                .ToListAsync();
            return View(enrollments);
        }

        // Student: view own enrolled modules
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyModules()
        {
            var user = await _userManager.GetUserAsync(User);
            var enrollments = await _db.Enrollments
                .Include(e => e.Module)
                .Where(e => e.UserId == user!.Id)
                .ToListAsync();
            return View(enrollments);
        }

        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Create()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            ViewBag.Students = new SelectList(students, "Id", "FullName");
            ViewBag.Modules = new SelectList(await _db.LearningModules.ToListAsync(), "ModuleId", "Title");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Create(Enrollment model)
        {
            var exists = await _db.Enrollments
                .AnyAsync(e => e.UserId == model.UserId && e.ModuleId == model.ModuleId);
            if (exists)
            {
                ModelState.AddModelError("", "Student is already enrolled in this module.");
                var students = await _userManager.GetUsersInRoleAsync("Student");
                ViewBag.Students = new SelectList(students, "Id", "FullName");
                ViewBag.Modules = new SelectList(await _db.LearningModules.ToListAsync(), "ModuleId", "Title");
                return View(model);
            }

            _db.Enrollments.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Student enrolled successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Delete(int id)
        {
            var enrollment = await _db.Enrollments.FindAsync(id);
            if (enrollment != null)
            {
                _db.Enrollments.Remove(enrollment);
                await _db.SaveChangesAsync();
            }
            TempData["Success"] = "Enrollment removed.";
            return RedirectToAction(nameof(Index));
        }
    }
}
