using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdaptiveLearningSystem.Data;
using AdaptiveLearningSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AdaptiveLearningSystem.Controllers
{
    [Authorize]
    public class LearningModulesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public LearningModulesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Index(string? subject, string? level)
        {
            var query = _db.LearningModules.AsQueryable();
            var subjectsQuery = _db.LearningModules.AsQueryable();

            var currentUser = await _userManager.GetUserAsync(User);
            // If current user is a Teacher, limit modules and subject list to modules assigned to them
            if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Teacher"))
            {
                query = query.Where(m => m.TeacherId == currentUser.Id);
                subjectsQuery = subjectsQuery.Where(m => m.TeacherId == currentUser.Id);
            }

            if (!string.IsNullOrEmpty(subject))
                query = query.Where(m => m.Subject == subject);
            if (!string.IsNullOrEmpty(level))
                query = query.Where(m => m.Level == level);

            ViewBag.Subjects = await subjectsQuery
                .Select(m => m.Subject).Distinct().ToListAsync();
            ViewBag.SelectedSubject = subject;
            ViewBag.SelectedLevel = level;
            return View(await query.ToListAsync());
        }

        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            // Only admins may pick a teacher when creating a module. Teachers will be assigned automatically.
            if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
                ViewBag.Teachers = new SelectList(teachers, "Id", "FullName");
            }
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Create(LearningModule model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // If the creator is a teacher, force the TeacherId to the current user.
            if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Teacher"))
            {
                model.TeacherId = currentUser.Id;
            }

            if (!ModelState.IsValid)
            {
                // Repopulate teachers only for admins
                if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin"))
                {
                    var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
                    ViewBag.Teachers = new SelectList(teachers, "Id", "FullName", model.TeacherId);
                }
                return View(model);
            }
            _db.LearningModules.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Module created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int id)
        {
            var module = await _db.LearningModules.FindAsync(id);
            if (module == null) return NotFound();
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
                ViewBag.Teachers = new SelectList(teachers, "Id", "FullName", module.TeacherId);
            }
            return View(module);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int id, LearningModule model)
        {
            if (id != model.ModuleId) return BadRequest();
            var currentUser = await _userManager.GetUserAsync(User);

            // If the editor is a teacher, ensure TeacherId remains the current teacher
            if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Teacher"))
            {
                model.TeacherId = currentUser.Id;
            }

            if (!ModelState.IsValid)
            {
                if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin"))
                {
                    var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
                    ViewBag.Teachers = new SelectList(teachers, "Id", "FullName", model.TeacherId);
                }
                return View(model);
            }
            _db.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Module updated.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var module = await _db.LearningModules
                .Include(m => m.Quizzes)
                .Include(m => m.Teacher)
                .FirstOrDefaultAsync(m => m.ModuleId == id);
            if (module == null) return NotFound();

            // If user is a student, ensure they are enrolled in this module
            if (User.IsInRole("Student"))
            {
                var user = await _userManager.GetUserAsync(User);
                var enrolled = await _db.Enrollments.AnyAsync(e => e.UserId == user!.Id && e.ModuleId == id);
                if (!enrolled)
                {
                    TempData["Error"] = "You are not enrolled in this module.";
                    return RedirectToAction("MyModules", "Enrollments");
                }
            }

            return View(module);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Delete(int id)
        {
            var module = await _db.LearningModules.FindAsync(id);
            if (module != null)
            {
                _db.LearningModules.Remove(module);
                await _db.SaveChangesAsync();
            }
            TempData["Success"] = "Module deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
