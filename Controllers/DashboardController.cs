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
            var user = await _userManager.GetUserAsync(User);

            // If current user is a student, show only their enrolled modules on the main dashboard
            if (user != null && await _userManager.IsInRoleAsync(user, "Student"))
            {
                var enrollments = await _db.Enrollments
                    .Include(e => e.Module)
                        .ThenInclude(m => m.Teacher)
                    .Where(e => e.UserId == user.Id)
                    .ToListAsync();

                return View("Student", enrollments);
            }

            // Teacher: show analytics limited to modules they teach
            if (user != null && await _userManager.IsInRoleAsync(user, "Teacher"))
            {
                var moduleIds = await _db.LearningModules
                    .Where(m => m.TeacherId == user.Id)
                    .Select(m => m.ModuleId)
                    .ToListAsync();

                var totalModules = moduleIds.Count;

                var studentIds = moduleIds.Any()
                    ? await _db.Enrollments.Where(e => moduleIds.Contains(e.ModuleId)).Select(e => e.UserId).Distinct().ToListAsync()
                    : new List<string>();

                var totalStudents = studentIds.Count;

                var totalCompleted = moduleIds.Any()
                    ? await _db.StudentProgresses.CountAsync(p => moduleIds.Contains(p.ModuleId) && p.CompletionStatus == "Completed")
                    : 0;

                var averageScore = moduleIds.Any() && await _db.StudentProgresses.AnyAsync(p => moduleIds.Contains(p.ModuleId))
                    ? Math.Round(await _db.StudentProgresses.Where(p => moduleIds.Contains(p.ModuleId)).AverageAsync(p => p.QuizScore), 1)
                    : 0;

                var struggling = moduleIds.Any()
                    ? await _db.StudentProgresses
                        .Include(p => p.User)
                        .Include(p => p.Module)
                        .Where(p => moduleIds.Contains(p.ModuleId) && p.QuizScore < 75)
                        .OrderBy(p => p.QuizScore)
                        .Take(10)
                        .ToListAsync()
                    : new List<StudentProgress>();

                var recent = moduleIds.Any()
                    ? await _db.StudentProgresses
                        .Include(p => p.User)
                        .Include(p => p.Module)
                        .Include(p => p.Quiz)
                        .Where(p => moduleIds.Contains(p.ModuleId))
                        .OrderByDescending(p => p.DateCompleted)
                        .Take(10)
                        .ToListAsync()
                    : new List<StudentProgress>();

                var inactiveStudents = new List<ApplicationUser>();
                if (studentIds.Any())
                {
                    inactiveStudents = await _db.Users
                        .Where(u => studentIds.Contains(u.Id) && !_db.StudentProgresses
                            .Any(p => p.UserId == u.Id && p.DateCompleted >= DateTime.Now.AddDays(-7)))
                        .ToListAsync();
                }

                var vmTeacher = new DashboardViewModel
                {
                    TotalStudents = totalStudents,
                    TotalModules = totalModules,
                    TotalCompleted = totalCompleted,
                    AverageScore = averageScore,
                    StrugglingStudents = struggling,
                    RecentProgress = recent,
                    InactiveStudents = inactiveStudents
                };

                return View(vmTeacher);
            }

            // Admins and Teachers see the learning analytics dashboard
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
