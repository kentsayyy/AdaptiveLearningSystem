# AdaptLearn PH — Setup Instructions
# Adaptive E-Learning System for Public Schools in Rural Philippines
# SDG 4 — Quality Education | ASP.NET Core MVC + EF Core + SQL Server

===========================================================
 STEP-BY-STEP INSTRUCTIONS TO RUN IN VISUAL STUDIO
===========================================================

PREREQUISITES (install these first if not already installed):
  - Visual Studio 2022 (Community, Professional, or Enterprise)
  - .NET 8.0 SDK  →  https://dotnet.microsoft.com/download
  - SQL Server Express  →  https://www.microsoft.com/en-us/sql-server/sql-server-downloads
    (Choose "Express" — free edition)

===========================================================
 STEP 1 — OPEN THE PROJECT
===========================================================
  3. Click "Open a project or solution"
  4. Navigate to the unzipped folder
  5. Select:  AdaptiveLearningSystem.csproj
  6. Click Open

===========================================================
 STEP 2 — RESTORE NUGET PACKAGES
===========================================================
  Visual Studio usually does this automatically. If not:
  1. Go to  Tools → NuGet Package Manager → Manage NuGet Packages for Solution
  2. Click "Restore" button at the top

  OR right-click the project in Solution Explorer → Restore NuGet Packages

===========================================================
 STEP 3 — CHECK YOUR SQL SERVER CONNECTION STRING
===========================================================
  Open:  appsettings.json

  Default connection string:
    "Server=.\\SQLEXPRESS;Database=AdaptiveLearningDB;Trusted_Connection=True;TrustServerCertificate=True"

  If your SQL Server instance name is different, update it.
  Common instance names:
    - .\\SQLEXPRESS         (most common default)
    - .\\MSSQLSERVER        (full SQL Server)
    - (localdb)\\mssqllocaldb  (LocalDB — if you have it)
    - YOUR-PC-NAME\\SQLEXPRESS

  To find your instance name:
    Open SQL Server Management Studio (SSMS) → the server name shown is your instance name.

===========================================================
 STEP 4 — CREATE THE DATABASE (Run Migrations)
===========================================================
  In Visual Studio:
  1. Go to  Tools → NuGet Package Manager → Package Manager Console
  2. Make sure the Default project dropdown shows "AdaptiveLearningSystem"
  3. Run these two commands (one at a time):

     Add-Migration InitialCreate

     Update-Database

  You should see:  "Done." — the database is now created!

  To verify: Open SSMS → connect → you should see "AdaptiveLearningDB" database with all tables.

===========================================================
 STEP 5 — RUN THE APPLICATION
===========================================================
  Press  F5  (or click the green "Play" button in Visual Studio)

  The app will open in your browser at:  https://localhost:7001

===========================================================
 DEFAULT LOGIN CREDENTIALS
===========================================================
  Admin Account (auto-seeded on first run):
    Email:     admin@adaptlearn.ph
    Password:  Admin@123

  To create Teacher / Student accounts:
    Click "Register" on the login page → choose the Role → register

===========================================================
 HOW TO USE THE SYSTEM
===========================================================

  AS ADMIN / TEACHER:
  ┌─────────────────────────────────────────────────────┐
  │ 1. Login with admin credentials                     │
  │ 2. Dashboard → see analytics summary                │
  │ 3. Modules → Add learning modules (subjects)        │
  │ 4. Quizzes → Add quizzes linked to modules          │
  │ 5. Students → view/edit/delete student accounts     │
  │ 6. Enrollments → enroll students in modules         │
  │ 7. Progress Records → filter and view all progress  │
  │    + adaptive recommendations                       │
  └─────────────────────────────────────────────────────┘

  AS STUDENT:
  ┌─────────────────────────────────────────────────────┐
  │ 1. Register as Student                              │
  │ 2. My Modules → see enrolled modules                │
  │ 3. Record Progress → submit quiz scores             │
  │    (scores < 75 → "Review Basic Lesson")            │
  │    (scores ≥ 75 → "Proceed to Advanced Lesson")     │
  └─────────────────────────────────────────────────────┘

===========================================================
 GRADING CHECKLIST (all implemented)
===========================================================
  [✓] User Login & Registration with role selection
  [✓] Admin, Teacher, Student roles (seeded automatically)
  [✓] Role-based [Authorize] on all controllers
  [✓] Dashboard with analytics: students, modules, scores
  [✓] Struggling students table (score < 75)
  [✓] Inactive students list (no activity in 7 days)
  [✓] Adaptive recommendation: Review vs Advanced
  [✓] Students CRUD + search
  [✓] Learning Modules CRUD + subject/level filter
  [✓] Quizzes CRUD
  [✓] Enrollments CRUD + student "My Modules" view
  [✓] Student Progress CRUD + date/name/status filter
  [✓] All 5 database tables (ApplicationUser, LearningModule,
      Enrollment, Quiz, StudentProgress)
  [✓] EF Core Code-First migrations
  [✓] Bootstrap 5 responsive UI with sidebar
  [✓] SDG 4 landing page explanation
  [✓] Validation on all forms
  [✓] Success/error messages

===========================================================
 TROUBLESHOOTING
===========================================================

  Problem: "A network-related error" when running
  Fix: Check your SQL Server is running.
       Open Services (Win+R → services.msc) → start "SQL Server (SQLEXPRESS)"

  Problem: SSL certificate error in browser
  Fix: Click "Advanced" → "Proceed anyway"
       OR run:  dotnet dev-certs https --trust  (in cmd/terminal)

  Problem: "Cannot connect to SQL Server"
  Fix: Update the connection string in appsettings.json with your correct server name

  Problem: Migration fails
  Fix: Make sure SQL Server is running, connection string is correct,
       then re-run:  Update-Database

===========================================================
 PROJECT STRUCTURE
===========================================================
  AdaptiveLearningSystem/
  ├── Controllers/          ← HTTP request handlers
  │   ├── AccountController.cs
  │   ├── DashboardController.cs
  │   ├── StudentsController.cs
  │   ├── LearningModulesController.cs
  │   ├── QuizzesController.cs
  │   ├── StudentProgressController.cs
  │   ├── EnrollmentsController.cs
  │   └── HomeController.cs
  ├── Models/               ← Database entities (5 tables)
  │   ├── ApplicationUser.cs
  │   ├── LearningModule.cs
  │   ├── Enrollment.cs
  │   ├── Quiz.cs
  │   └── StudentProgress.cs
  ├── Data/
  │   └── ApplicationDbContext.cs   ← EF Core DbContext
  ├── ViewModels/
  │   ├── DashboardViewModel.cs
  │   └── AccountViewModels.cs
  ├── Views/                ← Razor HTML templates
  │   ├── Dashboard/
  │   ├── Students/
  │   ├── LearningModules/
  │   ├── Quizzes/
  │   ├── StudentProgress/
  │   ├── Enrollments/
  │   ├── Account/
  │   ├── Home/
  │   └── Shared/_Layout.cshtml
  ├── Program.cs            ← App startup + role seeding
  └── appsettings.json      ← Connection string config