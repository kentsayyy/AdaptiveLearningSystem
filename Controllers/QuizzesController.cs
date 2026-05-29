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
            ViewBag.Modules = new SelectList(await _db.LearningModules.ToListAsync(), "ModuleId", "Title");
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

            if (!ModelState.IsValid)
            {
                ViewBag.Modules = new SelectList(await _db.LearningModules.ToListAsync(), "ModuleId", "Title");
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
            ViewBag.Modules = new SelectList(await _db.LearningModules.ToListAsync(), "ModuleId", "Title", quiz.ModuleId);
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

            if (!ModelState.IsValid)
            {
                ViewBag.Modules = new SelectList(await _db.LearningModules.ToListAsync(), "ModuleId", "Title", model.ModuleId);
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
                _db.Quizzes.Remove(quiz);
                await _db.SaveChangesAsync();
            }
            TempData["Success"] = "Quiz deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
