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
                // Teachers see only progress for their modules
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
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = _userManager.GetUserId(User);
            ViewBag.SelectedModuleId = moduleId?.ToString() ?? "";
            ViewBag.SelectedQuizId = quizId ?? 0;
            // If the current user is a student, only show modules they are enrolled in
            if (User.IsInRole("Student"))
            {
                var enrollments = await _db.Enrollments
                    .Include(e => e.Module)
                    .Where(e => e.UserId == currentUserId)
                    .ToListAsync();

                var modules = enrollments.Select(e => e.Module!).Where(m => m != null).Distinct().ToList();
                ViewBag.Modules = new SelectList(modules, "ModuleId", "Title");

                var moduleIds = modules.Select(m => m!.ModuleId).ToList();
                var quizzes = await _db.Quizzes.Where(q => moduleIds.Contains(q.ModuleId)).ToListAsync();
                ViewBag.Quizzes = new SelectList(quizzes, "QuizId", "Title");
                ViewBag.QuizzesRaw = quizzes;

                ViewBag.UserId = currentUserId;
            }
            else
            {
                ViewBag.Modules = new SelectList(await _db.LearningModules.ToListAsync(), "ModuleId", "Title");
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
            if (User.IsInRole("Student"))
            {
                model.UserId = currentUserId ?? string.Empty;
                // If ModelState previously recorded missing UserId, remove it so server-side validation uses this value
                if (ModelState.ContainsKey("UserId")) ModelState.Remove("UserId");
            }

            // Additional validation for student users: ensure they are enrolled in the selected module
            if (User.IsInRole("Student"))
            {
                var isEnrolled = await _db.Enrollments
                    .AnyAsync(e => e.UserId == currentUserId && e.ModuleId == model.ModuleId);
                if (!isEnrolled)
                {
                    ModelState.AddModelError("ModuleId", "You are not enrolled in the selected module.");
                }
            }

            // Validate quiz belongs to selected module
            if (model.QuizId != 0)
            {
                var quiz = await _db.Quizzes.FindAsync(model.QuizId);
                if (quiz == null || quiz.ModuleId != model.ModuleId)
                {
                    ModelState.AddModelError("QuizId", "The selected quiz does not belong to the selected module.");
                }
                else
                {
                    // Determine which input the user provided. Prefer an explicitly provided QuizScore; otherwise use CorrectAnswers
                    var form = Request?.Form;
                    var quizScoreProvided = form != null && form.TryGetValue("QuizScore", out var qsVal) && !string.IsNullOrWhiteSpace(qsVal);
                    var correctProvided = model.CorrectAnswers.HasValue;

                    if (quizScoreProvided)
                    {
                        // Validate provided percent
                        if (model.QuizScore < 0 || model.QuizScore > 100)
                        {
                            ModelState.AddModelError("QuizScore", "Quiz score must be between 0 and 100.");
                        }
                    }
                    else if (correctProvided)
                    {
                        if (model.CorrectAnswers.Value < 0 || model.CorrectAnswers.Value > quiz.TotalItems)
                        {
                            ModelState.AddModelError("CorrectAnswers", $"Correct answers must be between 0 and {quiz.TotalItems}.");
                        }
                        else
                        {
                            model.QuizScore = Math.Round(((double)model.CorrectAnswers.Value / Math.Max(1, quiz.TotalItems)) * 100.0, 2);
                        }
                    }
                    else
                    {
                        // Neither provided — invalid
                        ModelState.AddModelError("QuizScore", "Please provide a Quiz Score or the number of Correct Answers.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                // Collect modelstate errors for easier debugging/display
                var errors = ModelState.Where(kv => kv.Value.Errors.Any())
                    .Select(kv => new {
                        Key = kv.Key,
                        Errors = kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    }).ToList();
                if (errors.Any())
                {
                    var flat = string.Join("; ", errors.SelectMany(e => e.Errors));
                    TempData["ModelErrors"] = flat;
                }
                // Repopulate selects based on role
                if (User.IsInRole("Student"))
                {
                    var enrollments = await _db.Enrollments
                        .Include(e => e.Module)
                        .Where(e => e.UserId == currentUserId)
                        .ToListAsync();

                    var modules = enrollments.Select(e => e.Module!).Where(m => m != null).Distinct().ToList();
                    ViewBag.Modules = new SelectList(modules, "ModuleId", "Title");

                    var moduleIds = modules.Select(m => m!.ModuleId).ToList();
                    var quizzes = await _db.Quizzes.Where(q => moduleIds.Contains(q.ModuleId)).ToListAsync();
                    ViewBag.Quizzes = new SelectList(quizzes, "QuizId", "Title");
                    ViewBag.QuizzesRaw = quizzes;
                }
                else
                {
                    ViewBag.Modules = new SelectList(await _db.LearningModules.ToListAsync(), "ModuleId", "Title");
                    var allQuizzes = await _db.Quizzes.ToListAsync();
                    ViewBag.Quizzes = new SelectList(allQuizzes, "QuizId", "Title");
                    ViewBag.QuizzesRaw = allQuizzes;
                }

                return View(model);
            }

            try
            {
                _db.StudentProgresses.Add(model);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Progress recorded.";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                // capture and return the error to the view for debugging
                TempData["ModelErrors"] = ex.Message;
                // Repopulate selects based on role before returning
                var currentUser = await _userManager.GetUserAsync(User);
                if (User.IsInRole("Student"))
                {
                    var enrollments = await _db.Enrollments
                        .Include(e => e.Module)
                        .Where(e => e.UserId == currentUser!.Id)
                        .ToListAsync();

                    var modules = enrollments.Select(e => e.Module!).Where(m => m != null).Distinct().ToList();
                    ViewBag.Modules = new SelectList(modules, "ModuleId", "Title");

                    var moduleIds = modules.Select(m => m!.ModuleId).ToList();
                    var quizzes = await _db.Quizzes.Where(q => moduleIds.Contains(q.ModuleId)).ToListAsync();
                    ViewBag.Quizzes = new SelectList(quizzes, "QuizId", "Title");
                    ViewBag.QuizzesRaw = quizzes;
                }
                else
                {
                    ViewBag.Modules = new SelectList(await _db.LearningModules.ToListAsync(), "ModuleId", "Title");
                    var allQuizzes = await _db.Quizzes.ToListAsync();
                    ViewBag.Quizzes = new SelectList(allQuizzes, "QuizId", "Title");
                    ViewBag.QuizzesRaw = allQuizzes;
                }

                return View(model);
            }
        }
    }
}
