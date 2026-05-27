using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AdaptiveLearningSystem.Data;
using AdaptiveLearningSystem.Models;

namespace AdaptiveLearningSystem.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class QuizzesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public QuizzesController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var quizzes = await _db.Quizzes.Include(q => q.Module).ToListAsync();
            return View(quizzes);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Modules = new SelectList(await _db.LearningModules.ToListAsync(), "ModuleId", "Title");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Quiz model)
        {
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
