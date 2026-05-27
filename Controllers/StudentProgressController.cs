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
    public class StudentProgressController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentProgressController(ApplicationDbContext db,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Index(
            string? studentName, string? status, DateTime? fromDate, DateTime? toDate)
        {
            var query = _db.StudentProgresses
                .Include(p => p.User)
                .Include(p => p.Module)
                .Include(p => p.Quiz)
                .AsQueryable();

            if (!string.IsNullOrEmpty(studentName))
                query = query.Where(p => p.User!.FullName.Contains(studentName));
            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.CompletionStatus == status);
            if (fromDate.HasValue)
                query = query.Where(p => p.DateCompleted >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(p => p.DateCompleted <= toDate.Value);

            ViewBag.StudentName = studentName;
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(await query.OrderByDescending(p => p.DateCompleted).ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.Modules = new SelectList(await _db.LearningModules.ToListAsync(), "ModuleId", "Title");
            ViewBag.Quizzes = new SelectList(await _db.Quizzes.ToListAsync(), "QuizId", "Title");

            if (User.IsInRole("Student"))
                ViewBag.UserId = currentUser?.Id;

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentProgress model)
        {
            if (User.IsInRole("Student"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                model.UserId = currentUser?.Id ?? string.Empty;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Modules = new SelectList(await _db.LearningModules.ToListAsync(), "ModuleId", "Title");
                ViewBag.Quizzes = new SelectList(await _db.Quizzes.ToListAsync(), "QuizId", "Title");
                return View(model);
            }

            _db.StudentProgresses.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Progress recorded.";
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
