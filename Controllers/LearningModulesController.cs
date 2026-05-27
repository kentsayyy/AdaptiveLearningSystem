using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdaptiveLearningSystem.Data;
using AdaptiveLearningSystem.Models;

namespace AdaptiveLearningSystem.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class LearningModulesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public LearningModulesController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? subject, string? level)
        {
            var query = _db.LearningModules.AsQueryable();
            if (!string.IsNullOrEmpty(subject))
                query = query.Where(m => m.Subject == subject);
            if (!string.IsNullOrEmpty(level))
                query = query.Where(m => m.Level == level);

            ViewBag.Subjects = await _db.LearningModules
                .Select(m => m.Subject).Distinct().ToListAsync();
            ViewBag.SelectedSubject = subject;
            ViewBag.SelectedLevel = level;
            return View(await query.ToListAsync());
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LearningModule model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.LearningModules.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Module created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var module = await _db.LearningModules.FindAsync(id);
            if (module == null) return NotFound();
            return View(module);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LearningModule model)
        {
            if (id != model.ModuleId) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            _db.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Module updated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var module = await _db.LearningModules
                .Include(m => m.Quizzes)
                .FirstOrDefaultAsync(m => m.ModuleId == id);
            if (module == null) return NotFound();
            return View(module);
        }

        [HttpPost, ValidateAntiForgeryToken]
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
