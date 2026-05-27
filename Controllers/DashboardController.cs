using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdaptiveLearningSystem.Data;
using AdaptiveLearningSystem.Models;
using AdaptiveLearningSystem.ViewModels;

namespace AdaptiveLearningSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext db,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");

            var vm = new DashboardViewModel
            {
                TotalStudents = students.Count,
                TotalModules = await _db.LearningModules.CountAsync(),
                TotalCompleted = await _db.StudentProgresses
                    .CountAsync(p => p.CompletionStatus == "Completed"),
                AverageScore = await _db.StudentProgresses.AnyAsync()
                    ? Math.Round(await _db.StudentProgresses.AverageAsync(p => p.QuizScore), 1)
                    : 0,
                StrugglingStudents = await _db.StudentProgresses
                    .Include(p => p.User)
                    .Include(p => p.Module)
                    .Where(p => p.QuizScore < 75)
                    .OrderBy(p => p.QuizScore)
                    .Take(10)
                    .ToListAsync(),
                RecentProgress = await _db.StudentProgresses
                    .Include(p => p.User)
                    .Include(p => p.Module)
                    .Include(p => p.Quiz)
                    .OrderByDescending(p => p.DateCompleted)
                    .Take(10)
                    .ToListAsync(),
                InactiveStudents = students
                    .Where(s => !_db.StudentProgresses
                        .Any(p => p.UserId == s.Id &&
                            p.DateCompleted >= DateTime.Now.AddDays(-7)))
                    .ToList()
            };

            return View(vm);
        }
    }
}
