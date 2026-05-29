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

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Teacher"))
            {
                query = query.Where(p => p.Module != null && p.Module.TeacherId == currentUser.Id);
            }

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

        public async Task<IActionResult> Create(int? moduleId, int? quizId)
        {
            var currentUserId = _userManager.GetUserId(User);
            ViewBag.SelectedModuleId = moduleId?.ToString() ?? "";
            ViewBag.SelectedQuizId = quizId ?? 0;

            if (User.IsInRole("Student"))
            {
                var enrollments = await _db.Enrollments
                    .Include(e => e.Module)
                    .Where(e => e.UserId == currentUserId)
                    .ToListAsync();

                var modules = enrollments.Select(e => e.Module!)
                    .Where(m => m != null).Distinct().ToList();
                ViewBag.Modules = new SelectList(modules, "ModuleId", "Title");

                var moduleIds = modules.Select(m => m!.ModuleId).ToList();
                var quizzes = await _db.Quizzes
                    .Where(q => moduleIds.Contains(q.ModuleId)).ToListAsync();
                ViewBag.Quizzes = new SelectList(quizzes, "QuizId", "Title");
                ViewBag.QuizzesRaw = quizzes;
                ViewBag.UserId = currentUserId;
            }
            else
            {
                var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
                List<ApplicationUser> students = new();
                if (studentRole != null)
                {
                    var studentIds = await _db.UserRoles
                        .Where(ur => ur.RoleId == studentRole.Id)
                        .Select(ur => ur.UserId)
                        .ToListAsync();
                    students = await _db.Users
                        .Where(u => studentIds.Contains(u.Id))
                        .OrderBy(u => u.FullName)
                        .ToListAsync();
                }
                ViewBag.Students = new SelectList(students, "Id", "FullName");
                ViewBag.Modules = new SelectList(
                    await _db.LearningModules.ToListAsync(), "ModuleId", "Title");
                var allQuizzes = await _db.Quizzes.ToListAsync();
                ViewBag.Quizzes = new SelectList(allQuizzes, "QuizId", "Title");
                ViewBag.QuizzesRaw = allQuizzes;
            }

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentProgress model)
        {
            var currentUserId = _userManager.GetUserId(User);

            ModelState.Remove("DateCompleted");
            ModelState.Remove("User");
            ModelState.Remove("Module");
            ModelState.Remove("Quiz");

            if (User.IsInRole("Student"))
            {
                model.UserId = currentUserId ?? string.Empty;
                ModelState.Remove("UserId");

                var isEnrolled = await _db.Enrollments
                    .AnyAsync(e => e.UserId == currentUserId && e.ModuleId == model.ModuleId);
                if (!isEnrolled)
                    ModelState.AddModelError("ModuleId", "You are not enrolled in the selected module.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.UserId))
                    ModelState.AddModelError("UserId", "Please select a student.");
                else
                    ModelState.Remove("UserId");
            }

            if (model.QuizId != 0)
            {
                var quiz = await _db.Quizzes.FindAsync(model.QuizId);
                if (quiz == null || quiz.ModuleId != model.ModuleId)
                {
                    ModelState.AddModelError("QuizId",
                        "The selected quiz does not belong to the selected module.");
                }
                else
                {
                    bool correctProvided = model.CorrectAnswers.HasValue;
                    var form = Request?.Form;
                    bool quizScoreProvided = form != null
                        && form.TryGetValue("QuizScore", out var qsVal)
                        && !string.IsNullOrWhiteSpace(qsVal)
                        && qsVal != "0";

                    if (correctProvided)
                    {
                        if (model.CorrectAnswers!.Value < 0 || model.CorrectAnswers.Value > quiz.TotalItems)
                        {
                            ModelState.AddModelError("CorrectAnswers",
                                $"Correct answers must be between 0 and {quiz.TotalItems}.");
                        }
                        else
                        {
                            // Store raw score (e.g. 5), not percent
                            model.QuizScore = model.CorrectAnswers.Value;
                            ModelState.Remove("QuizScore");
                        }
                    }
                    else if (quizScoreProvided)
                    {
                        if (model.QuizScore < 0 || model.QuizScore > quiz.TotalItems)
                            ModelState.AddModelError("QuizScore",
                                $"Quiz score must be between 0 and {quiz.TotalItems}.");
                    }
                    else
                    {
                        ModelState.AddModelError("QuizScore",
                            "Please provide a Quiz Score or the number of Correct Answers.");
                    }
                }
            }
            else
            {
                ModelState.AddModelError("QuizId", "Please select a quiz.");
            }

            if (!ModelState.IsValid)
            {
                var flat = string.Join("; ", ModelState
                    .Where(kv => kv.Value!.Errors.Any())
                    .SelectMany(kv => kv.Value!.Errors.Select(e => $"{kv.Key}: {e.ErrorMessage}")));
                TempData["ModelErrors"] = flat;

                await RepopulateViewBags(currentUserId);
                return View(model);
            }

            try
            {
                _db.StudentProgresses.Add(model);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Progress recorded successfully.";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ModelErrors"] = ex.InnerException?.Message ?? ex.Message;
                await RepopulateViewBags(currentUserId);
                return View(model);
            }
        }

        private async Task RepopulateViewBags(string? currentUserId)
        {
            if (User.IsInRole("Student"))
            {
                var enrollments = await _db.Enrollments
                    .Include(e => e.Module)
                    .Where(e => e.UserId == currentUserId)
                    .ToListAsync();

                var modules = enrollments.Select(e => e.Module!)
                    .Where(m => m != null).Distinct().ToList();
                ViewBag.Modules = new SelectList(modules, "ModuleId", "Title");

                var moduleIds = modules.Select(m => m!.ModuleId).ToList();
                var quizzes = await _db.Quizzes
                    .Where(q => moduleIds.Contains(q.ModuleId)).ToListAsync();
                ViewBag.Quizzes = new SelectList(quizzes, "QuizId", "Title");
                ViewBag.QuizzesRaw = quizzes;
            }
            else
            {
                var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
                List<ApplicationUser> students = new();
                if (studentRole != null)
                {
                    var studentIds = await _db.UserRoles
                        .Where(ur => ur.RoleId == studentRole.Id)
                        .Select(ur => ur.UserId)
                        .ToListAsync();
                    students = await _db.Users
                        .Where(u => studentIds.Contains(u.Id))
                        .OrderBy(u => u.FullName)
                        .ToListAsync();
                }
                ViewBag.Students = new SelectList(students, "Id", "FullName");
                ViewBag.Modules = new SelectList(
                    await _db.LearningModules.ToListAsync(), "ModuleId", "Title");
                var allQuizzes = await _db.Quizzes.ToListAsync();
                ViewBag.Quizzes = new SelectList(allQuizzes, "QuizId", "Title");
                ViewBag.QuizzesRaw = allQuizzes;
            }
        }
    }
}