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
            var query = _db.Enrollments
                .Include(e => e.User)
                .Include(e => e.Module)
                .AsQueryable();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Teacher"))
            {
                // Teachers see only enrollments for modules they teach
                query = query.Where(e => e.Module != null && e.Module.TeacherId == currentUser.Id);
            }

            var enrollments = await query.ToListAsync();
            return View(enrollments);
        }

        // Student: view own enrolled modules
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyModules()
        {
            // MyModules dashboard removed — redirect students to main Dashboard which already
            // displays their enrolled modules. Keep this route for backward compatibility.
            return RedirectToAction("Index", "Dashboard");
        }

        // Allow students to enroll themselves in a module
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Enroll()
        {
            var modules = await _db.LearningModules.ToListAsync();
            ViewBag.Modules = new SelectList(modules, "ModuleId", "Title");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Enroll(int[] moduleIds)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (moduleIds == null || moduleIds.Length == 0)
            {
                TempData["Error"] = "Please select at least one module to enroll.";
                return RedirectToAction(nameof(Enroll));
            }

            var modules = await _db.LearningModules
                .Where(m => moduleIds.Contains(m.ModuleId))
                .ToListAsync();

            var added = 0;
            foreach (var module in modules.Distinct())
            {
                var exists = await _db.Enrollments
                    .AnyAsync(e => e.UserId == user.Id && e.ModuleId == module.ModuleId);
                if (exists) continue;

                var enrollment = new Enrollment
                {
                    UserId = user.Id,
                    ModuleId = module.ModuleId,
                    EnrolledDate = DateTime.Now,
                    Status = "Active"
                };
                _db.Enrollments.Add(enrollment);
                added++;
            }

            if (added > 0)
            {
                await _db.SaveChangesAsync();
                TempData["Success"] = added == 1 ? "Enrolled successfully." : $"Enrolled in {added} modules.";
            }
            else
            {
                TempData["Error"] = "No new enrollments were created (you may already be enrolled).";
            }

            return RedirectToAction("Index", "Dashboard");
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
