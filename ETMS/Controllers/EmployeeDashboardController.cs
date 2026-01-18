using ETMS.Data;
using ETMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using ETMS.Models.ViewModels;

namespace ETMS.Controllers
{
    [Authorize]
    public class EmployeeDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public EmployeeDashboardController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.Username = user?.UserName;
            ViewBag.Role = user?.Role;
            return View();
        }
        
        public async Task<IActionResult> StartTest(string testType)
        {
            var userId = _userManager.GetUserId(User);

            // Check if the user has already submitted this test
            bool alreadySubmitted = await _context.TestSubmissions
                .AnyAsync(ts => ts.UserId == userId && ts.TestType == testType);

            if (alreadySubmitted)
            {
                TempData["Error"] = "You have already completed this test.";
                return RedirectToAction("MyTest");
            }

            // If not submitted, redirect to the question page
            return RedirectToAction("AnswerQuestions", new { testType });
        }

        public async Task<IActionResult> MyTest()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.Username = user?.UserName;
            ViewBag.Role = user?.Role;

            var userId = user?.Id;

            // Fetch active test modes
            var testModes = await _context.TestModes.ToListAsync();

            // Check completed tests for the user
            var completedTests = await _context.UserResponses
                .Where(r => r.UserId == userId)
                .Select(r => r.TestType)
                .Distinct()
                .ToListAsync();

            foreach (var testType in new[] { "Pre-training", "Mid-training", "Post-training" })
            {
                var mode = testModes.FirstOrDefault(m => m.TestType == testType)?.Mode ?? "Inactive";
                ViewData[$"Mode_{testType}"] = mode;
                ViewData[$"Completed_{testType}"] = completedTests.Contains(testType);
            }

            return View();
        }


        public async Task<IActionResult> AnswerQuestions(string testType)
        {
            var user = await _userManager.GetUserAsync(User);

            ViewBag.Username = user?.UserName;
            ViewBag.Email = user?.Email;
            ViewBag.TestType = testType;

            var questions = await _context.Questions
                .Where(q => q.TestType == testType)
                .ToListAsync();

            ViewBag.TestDuration = 30; // Example: 50 minutes

            return View(questions);
        }
              

        public async Task<IActionResult> TestResults(string testType)
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.Username = user?.UserName;  // Ensure Username is set

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var results = _context.UserResponses
                                  .Where(r => r.UserId == userId)
                                  .Include(r => r.Question)
                                  .ToList();

            return View(results);
        }


        /*[HttpPost]
        public async Task<IActionResult> SubmitAnswers(List<UserResponse> answers, string testType)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User not found. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            // Check if the user has already submitted this test
            bool alreadySubmitted = await _context.TestSubmissions
                .AnyAsync(ts => ts.UserId == userId && ts.TestType == testType);

            if (alreadySubmitted)
            {
                TempData["Error"] = "You have already completed this test.";
                return RedirectToAction("MyTest");
            }

            // Fetch existing user responses
            var existingResponses = (await _context.UserResponses
            .Where(r => r.UserId == userId && r.TestType == testType)
            .Select(r => r.QuestionId)
             .ToListAsync()) // ✅ Convert to List first
            .ToHashSet();   // ✅ Then convert to HashSet


            var allQuestions = await _context.Questions
                .Where(q => q.TestType == testType)
                .ToListAsync();

            var answeredQuestionIds = answers.Select(a => a.QuestionId).ToHashSet();

            var newResponses = answers
                .Where(a => !existingResponses.Contains(a.QuestionId)) // Ensure no duplicate answers
                .Select(a => new UserResponse
                {
                    UserId = userId,
                    QuestionId = a.QuestionId,
                    TestType = testType,
                    SelectedOption = string.IsNullOrEmpty(a.SelectedOption) ? "No Answer" : a.SelectedOption,
                    SubmittedAt = DateTime.UtcNow
                })
                .ToList();

            var unansweredResponses = allQuestions
                .Where(q => !existingResponses.Contains(q.Id) && !answeredQuestionIds.Contains(q.Id))
                .Select(q => new UserResponse
                {
                    UserId = userId,
                    QuestionId = q.Id,
                    TestType = testType,
                    SelectedOption = "No Answer",
                    SubmittedAt = DateTime.UtcNow
                })
                .ToList();

            // Ensure we don't add duplicate responses
            _context.UserResponses.AddRange(newResponses);
            _context.UserResponses.AddRange(unansweredResponses);

            // Record test submission only if responses are being added
            if (newResponses.Any() || unansweredResponses.Any())
            {
                var testSubmission = new TestSubmission
                {
                    UserId = userId,
                    TestType = testType,
                    SubmittedAt = DateTime.UtcNow
                };
                _context.TestSubmissions.Add(testSubmission);
            }

            await _context.SaveChangesAsync();

            int totalQuestions = allQuestions.Count;
            int correctAnswers = newResponses.Count(a =>
            {
                var correctAnswer = allQuestions.FirstOrDefault(q => q.Id == a.QuestionId)?.CorrectAnswer;
                return correctAnswer != null && a.SelectedOption.Trim().ToLower() == correctAnswer.Trim().ToLower();
            });

            double percentageScore = totalQuestions > 0 ? ((double)correctAnswers / totalQuestions) * 100 : 0;

            TempData["Success"] = $"Test submitted successfully! 🎯 You answered {correctAnswers} out of {totalQuestions} correctly. " +
                                  $"Your final score is {percentageScore:F2}%.";

            return RedirectToAction("MyTest");
        }*/
        [HttpPost]
        public async Task<IActionResult> SubmitAnswers(List<UserResponse> answers, string testType)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User not found. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            bool alreadySubmitted = await _context.TestSubmissions
                .AnyAsync(ts => ts.UserId == userId && ts.TestType == testType);

            if (alreadySubmitted)
            {
                TempData["Error"] = "You have already completed this test.";
                return RedirectToAction("MyTest");
            }

            var allQuestions = await _context.Questions
                .Where(q => q.TestType == testType)
                .ToListAsync();

            int totalQuestions = allQuestions.Count;
            int correctAnswers = 0;

            var newResponses = new List<UserResponse>();

            foreach (var answer in answers)
            {
                var question = allQuestions.FirstOrDefault(q => q.Id == answer.QuestionId);
                bool isCorrect = question != null && question.CorrectAnswer.Trim().ToLower() == answer.SelectedOption.Trim().ToLower();

                if (isCorrect) correctAnswers++;

                newResponses.Add(new UserResponse
                {
                    UserId = userId,
                    QuestionId = answer.QuestionId,
                    TestType = testType,
                    SelectedOption = string.IsNullOrEmpty(answer.SelectedOption) ? "No Answer" : answer.SelectedOption,
                    SubmittedAt = DateTime.UtcNow
                });
            }

            _context.UserResponses.AddRange(newResponses);

            double percentageScore = totalQuestions > 0 ? ((double)correctAnswers / totalQuestions) * 100 : 0;

            // ✅ Save marks in TestSubmission
            var testSubmission = new TestSubmission
            {
                UserId = userId,
                TestType = testType,
                /* Marks = (int)percentageScore, // Store as integer*/
                Marks = correctAnswers,
                SubmittedAt = DateTime.UtcNow
            };
            _context.TestSubmissions.Add(testSubmission);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Test submitted successfully! 🎯 You answered {correctAnswers} out of {totalQuestions} correctly. " +
                                  $"Your final score is {percentageScore:F2}%.";

            return RedirectToAction("MyTest");
        }



        public async Task<IActionResult> AnswerSheet(string testType)
        {
            var userId = _userManager.GetUserId(User);

            // Fetch user's answers with the related questions for the specified testType
            var userResponses = await _context.UserResponses
                .Where(r => r.UserId == userId && r.TestType == testType)
                .Include(r => r.Question)
                .ToListAsync();

            if (!userResponses.Any())
            {
                TempData["Error"] = "No responses found for this test.";
                return RedirectToAction("TestResults");
            }

            ViewBag.TestType = testType;
            return View(userResponses);
        }

        
        public async Task<IActionResult> MyTraining()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.Username = user?.UserName;
            var userId = _userManager.GetUserId(User);

            // Get assigned modules for the logged-in employee
            var assignedModules = await _context.EmployeeModules
                .Where(em => em.EmployeeId == userId)
                .Include(em => em.Module)
                .ThenInclude(m => m.LearningMaterials)
                .Select(em => new ModuleMaterialsViewModel
                {
                    ModuleId = em.Module.Id,
                    ModuleName = em.Module.SubjectName,
                    LearningMaterials = em.Module.LearningMaterials.ToList()
                })
                .ToListAsync();

            return View(assignedModules);
        }
        public async Task<IActionResult> ViewMaterials(int moduleId)
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.Username = user?.UserName;
            var userId = _userManager.GetUserId(User);

            // Check if the user is assigned to this module
            var isAssigned = await _context.EmployeeModules
                .AnyAsync(em => em.EmployeeId == userId && em.ModuleId == moduleId);

            if (!isAssigned)
            {
                TempData["Error"] = "You do not have access to this module's materials.";
                return RedirectToAction("MyTraining");
            }

            // Get module details with learning materials
            var moduleMaterials = await _context.TrainingModules
                .Where(m => m.Id == moduleId)
                .Include(m => m.LearningMaterials)
                .Select(m => new ModuleMaterialsViewModel
                {
                    ModuleId = m.Id,
                    ModuleName = m.SubjectName,
                    LearningMaterials = m.LearningMaterials.ToList()
                })
                .FirstOrDefaultAsync();

            if (moduleMaterials == null)
            {
                TempData["Error"] = "Module materials not found.";
                return RedirectToAction("MyTraining");
            }

            return View(moduleMaterials);
        }

        // Get Assigned Projects
        public async Task<IActionResult> AssignedProjects()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.Username = user?.UserName;
            /*var user = await _userManager.GetUserAsync(User);*/
            if (user == null) return RedirectToAction("Login", "Account");

            var assignedProjects = await _context.EmployeeProjects
                .Where(ep => ep.EmployeeId == user.Id)
                .Include(ep => ep.Project)
                .Select(ep => new AssignedProjectViewModel
                {
                    ProjectId = ep.Project.Id,
                    ProjectName = ep.Project.ProjectName,
                    Description = ep.Project.Description,
                    Deadline = ep.Project.Deadline,
                    HasSubmitted = _context.ProjectSubmissions.Any(ps => ps.ProjectId == ep.Project.Id && ps.EmployeeId == user.Id)
                })
                .ToListAsync();

            return View(assignedProjects);
        }

        // Upload Project Submission
        [HttpPost]
        public async Task<IActionResult> UploadProject(int ProjectId, IFormFile ProjectFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (ProjectFile == null || ProjectFile.Length == 0)
            {
                TempData["Error"] = "Please select a ZIP file to upload.";
                return RedirectToAction("AssignedProjects");
            }

            // Ensure directory exists
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "UploadedProjects");
            Directory.CreateDirectory(uploadsFolder);

            // Save File
            string uniqueFileName = $"{user.Id}_{ProjectId}_{DateTime.UtcNow.Ticks}.zip";
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await ProjectFile.CopyToAsync(fileStream);
            }

            // Save submission record
            var projectSubmission = new ProjectSubmission
            {
                EmployeeId = user.Id,
                ProjectId = ProjectId,
                FilePath = uniqueFileName,
                SubmittedAt = DateTime.UtcNow
            };

            _context.ProjectSubmissions.Add(projectSubmission);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Project uploaded successfully!";
            return RedirectToAction("AssignedProjects");
        }
        public async Task<IActionResult> MyFeedback()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.Username = user?.UserName;
            
            if (user == null) return RedirectToAction("Login", "Account");

            var feedbackReports = await _context.EmployeeReports
                .Where(r => r.EmployeeId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(feedbackReports);
        }



    }


}















/*[HttpPost]
       public async Task<IActionResult> SubmitAnswers(List<UserResponse> answers, string testType)
       {
           var userId = _userManager.GetUserId(User);

           if (string.IsNullOrEmpty(userId))
           {
               TempData["Error"] = "User not found. Please log in again.";
               return RedirectToAction("Login", "Account");
           }

           bool alreadySubmitted = await _context.UserResponses
               .AnyAsync(r => r.UserId == userId && r.TestType == testType);

           if (alreadySubmitted)
           {
               TempData["Error"] = "You have already completed this test.";
               return RedirectToAction("MyTest");
           }

           var allQuestions = await _context.Questions
               .Where(q => q.TestType == testType)
               .ToListAsync();

           var answeredQuestionIds = answers.Select(a => a.QuestionId).ToHashSet();

           var unansweredResponses = allQuestions
               .Where(q => !answeredQuestionIds.Contains(q.Id))
               .Select(q => new UserResponse
               {
                   UserId = userId,
                   QuestionId = q.Id,
                   TestType = testType,
                   SelectedOption = "No Answer",
                   SubmittedAt = DateTime.UtcNow
               })
               .ToList();

           // Score Calculation
           int totalQuestions = allQuestions.Count;
           int attemptedQuestions = answers.Count;
           int correctAnswers = 0;

           foreach (var answer in answers)
           {
               answer.UserId = userId;
               answer.TestType = testType;
               answer.SubmittedAt = DateTime.UtcNow;

               if (string.IsNullOrEmpty(answer.SelectedOption))
               {
                   answer.SelectedOption = "No Answer";
               }

               var correctAnswer = allQuestions.FirstOrDefault(q => q.Id == answer.QuestionId)?.CorrectAnswer;
               if (correctAnswer != null && answer.SelectedOption == correctAnswer)
               {
                   correctAnswers++;
               }
           }

           _context.UserResponses.AddRange(answers);
           _context.UserResponses.AddRange(unansweredResponses);
           await _context.SaveChangesAsync();

           // Calculate score
           double percentageScore = ((double)correctAnswers / totalQuestions) * 100;
           double attemptPercentage = ((double)attemptedQuestions / totalQuestions) * 100;

           TempData["Success"] = $"Test submitted successfully! 🎯 You answered {correctAnswers} out of {attemptedQuestions} correctly. " +
                                 $"Your final score is {percentageScore:F2}% (Attempted {attemptPercentage:F2}% of the test).";

           return RedirectToAction("MyTest");
       }*/

/*  [HttpPost]
          public async Task<IActionResult> SubmitAnswers(List<UserResponse> answers, string testType)
          {
              var userId = _userManager.GetUserId(User);

              // Ensure test is not already submitted
              bool alreadySubmitted = await _context.UserResponses
                  .AnyAsync(r => r.UserId == userId && r.TestType == testType);

              if (alreadySubmitted)
              {
                  TempData["Error"] = "You have already completed this test.";
                  return RedirectToAction("MyTest");
              }

              // Collect existing answers for the user and testType
              var existingAnswers = await _context.UserResponses
                  .Where(r => r.UserId == userId && r.TestType == testType)
                  .Select(r => r.QuestionId)
                  .ToListAsync();

              // Filter new answers (skip if already exists)
              var newAnswers = answers.Where(a => !existingAnswers.Contains(a.QuestionId)).ToList();

              if (!newAnswers.Any())
              {
                  TempData["Error"] = "No new answers to submit.";
                  return RedirectToAction("MyTest");
              }

              // Save the new answers
              foreach (var answer in newAnswers)
              {
                  answer.UserId = userId;
                  answer.SubmittedAt = DateTime.UtcNow;
                  answer.TestType = testType;
                  _context.UserResponses.Add(answer);
              }

              await _context.SaveChangesAsync();

              TempData["Success"] = "Your answers have been submitted successfully!";
              return RedirectToAction("MyTest");
          }*/
