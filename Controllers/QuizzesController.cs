using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AdaptiveLearningSystem.Data;
using AdaptiveLearningSystem.Models;

namespace AdaptiveLearningSystem.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class QuizzesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public QuizzesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var query = _db.Quizzes.Include(q => q.Module).AsQueryable();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Teacher"))
            {
                // Teachers see only quizzes belonging to modules they teach
                query = query.Where(q => q.Module != null && q.Module.TeacherId == currentUser.Id);
            }

            var quizzes = await query.ToListAsync();
            return View(quizzes);
        }

        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var modulesQuery = _db.LearningModules.AsQueryable();
            if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Teacher"))
            {
                // Teachers should only see their own modules
                modulesQuery = modulesQuery.Where(m => m.TeacherId == currentUser.Id);
            }

            var modules = await modulesQuery.ToListAsync();
            ViewBag.Modules = new SelectList(modules, "ModuleId", "Title");

            // Prefill a default deadline so the datetime-local input shows a value
            var vm = new Quiz { Deadline = DateTime.Now.AddDays(7) };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Quiz model)
        {
            // Ensure deadline is provided and in the future
            if (model.Deadline <= DateTime.Now)
            {
                ModelState.AddModelError("Deadline", "Deadline must be a future date and time.");
            }

            // If teacher, ensure the selected module belongs to them
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Teacher"))
            {
                var module = await _db.LearningModules.FindAsync(model.ModuleId);
                if (module == null || module.TeacherId != currentUser.Id)
                {
                    ModelState.AddModelError("ModuleId", "Invalid module selection.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Re-populate modules list respecting teacher scope
                var modulesQuery = _db.LearningModules.AsQueryable();
                if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Teacher"))
                {
                    modulesQuery = modulesQuery.Where(m => m.TeacherId == currentUser.Id);
                }
                ViewBag.Modules = new SelectList(await modulesQuery.ToListAsync(), "ModuleId", "Title");
                return View(model);
            }
            _db.Quizzes.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Quiz created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var quiz = await _db.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound();
            var currentUser = await _userManager.GetUserAsync(User);
            var modulesQuery = _db.LearningModules.AsQueryable();
            if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Teacher"))
            {
                modulesQuery = modulesQuery.Where(m => m.TeacherId == currentUser.Id);
            }
            ViewBag.Modules = new SelectList(await modulesQuery.ToListAsync(), "ModuleId", "Title", quiz.ModuleId);
            return View(quiz);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Quiz model)
        {
            if (id != model.QuizId) return BadRequest();
            if (model.Deadline <= DateTime.Now)
            {
                ModelState.AddModelError("Deadline", "Deadline must be a future date and time.");
            }
            // If teacher, ensure the selected module belongs to them
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Teacher"))
            {
                var module = await _db.LearningModules.FindAsync(model.ModuleId);
                if (module == null || module.TeacherId != currentUser.Id)
                {
                    ModelState.AddModelError("ModuleId", "Invalid module selection.");
                }
            }

            if (!ModelState.IsValid)
            {
                var modulesQuery = _db.LearningModules.AsQueryable();
                if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Teacher"))
                {
                    modulesQuery = modulesQuery.Where(m => m.TeacherId == currentUser.Id);
                }
                ViewBag.Modules = new SelectList(await modulesQuery.ToListAsync(), "ModuleId", "Title", model.ModuleId);
                return View(model);
            }
            _db.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Quiz updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var quiz = await _db.Quizzes.FindAsync(id);
            if (quiz != null)
            {
                // Remove any related student progress entries first to avoid FK constraint errors
                var progresses = await _db.StudentProgresses.Where(p => p.QuizId == id).ToListAsync();
                if (progresses.Any())
                {
                    _db.StudentProgresses.RemoveRange(progresses);
                }

                _db.Quizzes.Remove(quiz);
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    // Log or surface a user-friendly message
                    TempData["Error"] = "Unable to delete quiz. Ensure there are no related records and try again.";
                    return RedirectToAction(nameof(Index));
                }
            }
            TempData["Success"] = "Quiz deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
