using ETMS.Data;
using ETMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using ETMS.Services;

namespace ETMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {

        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        public AdminDashboardController(UserManager<ApplicationUser> userManager, ApplicationDbContext context , IEmailService emailService)
        {
            _userManager = userManager;
            _context = context;
            _emailService = emailService;
        }


        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.AdminName = user?.UserName;

            ViewBag.TotalEmployees = await _userManager.Users.CountAsync(u => u.Role == "Employee");
            ViewBag.TotalTrainer = await _userManager.Users.CountAsync(u => u.Role == "Trainer");

            /*ViewBag.PendingTests = 5;*/
            ViewBag.ActiveTrainings = 0;

            return View();
        }

        
        public async Task<IActionResult> TestManagement()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.AdminName = user?.UserName;

            var testModes = await _context.TestModes.ToListAsync();
            foreach (var testType in new[] { "Pre-training", "Mid-training", "Post-training" })
            {
                var mode = testModes.FirstOrDefault(m => m.TestType == testType)?.Mode ?? "Inactive";
                ViewData[$"Mode_{testType}"] = mode;
            }

            return View();
        }


        [HttpGet]
        public IActionResult CreateQuestion(string testType)
        {
            if (string.IsNullOrEmpty(testType)) return BadRequest();

            ViewBag.TestType = testType;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateQuestion(Question model, string testType)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.TestType = testType;
                return View(model);
            }

            model.TestType = testType;
            _context.Questions.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("ViewQuestions", new { testType });
        }

        public IActionResult ViewQuestions(string testType)
        {
            if (string.IsNullOrEmpty(testType)) return BadRequest();

            var questions = _context.Questions
                                    .Where(q => q.TestType == testType)
                                    .ToList();

            ViewBag.TestType = testType;
            return View(questions);
        }

        public IActionResult EditQuestion(int id)
        {
            var question = _context.Questions.Find(id);
            if (question == null) return NotFound();
            return View(question);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestion(Question model)
        {
            if (!ModelState.IsValid) return View(model);

            _context.Questions.Update(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("TestManagement");
        }

        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null) return NotFound();

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return RedirectToAction("ViewQuestions", new { testType = question.TestType });
        }

        
        [HttpPost]
        public async Task<IActionResult> UpdateTestMode([FromBody] TestModeUpdate model)
        {
            Console.WriteLine($"Id: {model.Id}, Mode: {model.Mode}, TestType: {model.TestType}");

            if (model == null || string.IsNullOrEmpty(model.TestType) || string.IsNullOrEmpty(model.Mode))
            {
                return BadRequest(new { success = false, message = "Invalid data" });
            }

            // Search by Id if provided, otherwise by TestType
            var testMode = model.Id != 0
                ? await _context.TestModes.FindAsync(model.Id)
                : await _context.TestModes.FirstOrDefaultAsync(t => t.TestType == model.TestType);

            if (testMode == null) return NotFound(new { success = false, message = "Test mode not found" });

            // Update fields
            testMode.Mode = model.Mode;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Mode updated successfully!" });
        }


        public async Task<IActionResult> ViewCompletedTests(string testType)
        {
            var allEmployees = await _context.Users
                .Where(u => u.Role == "Employee")
                .ToListAsync();

            var completedEmployees = await _context.TestSubmissions
                .Where(t => t.TestType == testType)
                .Include(t => t.ApplicationUser)
                .Select(t => t.ApplicationUser.UserName)
                .Distinct()
                .ToListAsync();

            var remainingEmployees = allEmployees
                .Select(e => e.UserName)
                .Except(completedEmployees)
                .ToList();

            var model = new TestCompletionStatusViewModel
            {
                TestType = testType,  // ✅ Include TestType in the model
                CompletedEmployees = completedEmployees,
                RemainingEmployees = remainingEmployees
            };

            return View(model);
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNotification([FromBody] NotificationViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.TestType))
            {
                return Json(new { success = false, message = "Invalid request data." });
            }

            // Get all employees
            var allEmployees = await _userManager.GetUsersInRoleAsync("Employee");
            var allEmployeeEmails = allEmployees.Select(e => e.Email).ToList();

            // Get employees who have completed the test
            var completedEmployeeEmails = await _context.TestSubmissions
                .Where(t => t.TestType == model.TestType)
                .Select(t => t.ApplicationUser.Email)
                .Distinct()
                .ToListAsync();

            // Find remaining employees who haven't completed the test
            var remainingEmployees = allEmployeeEmails.Except(completedEmployeeEmails).ToList();

            if (!remainingEmployees.Any())
            {
                return Json(new { success = false, message = "All employees have completed the test. No notification needed." });
            }


            // Email Subject & Body
            string subject = $"Upcoming {model.TestType} Test Notification";
            string body = $"Dear Employee,<br/><br/>You have a scheduled {model.TestType} test on <strong>{model.Date} at {model.Time}</strong>.<br/>Please be prepared.<br/><br/>Best Regards,<br/>Admin Team";

            // Send email only to remaining employees
            await _emailService.SendEmailAsync(remainingEmployees, subject, body);

            return Json(new { success = true, message = "Notification emails sent to remaining employees successfully!" });
        }
        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.AdminName = user?.UserName;
            var reports = await _context.EmployeeReports
                .Include(r => r.Employee)
                .Include(r => r.Trainer)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reports); // ← Pass to the Reports.cshtml view
        }


    }
}









/* [HttpPost]
         public async Task<IActionResult> UpdateTestMode([FromBody] TestModeUpdate model)
         {
             Console.WriteLine($"Id: {model.Id}, Mode: {model.Mode}, TestType: {model.TestType}");

             if (model == null || string.IsNullOrEmpty(model.TestType) || string.IsNullOrEmpty(model.Mode))
             {
                 return BadRequest(new { error = "Invalid data" });
             }

             // Search by Id if provided, otherwise by TestType
             var testMode = model.Id != 0
                 ? await _context.TestModes.FindAsync(model.Id)
                 : await _context.TestModes.FirstOrDefaultAsync(t => t.TestType == model.TestType);

             if (testMode == null) return NotFound(new { error = "Test mode not found" });

             // Update fields
             testMode.Mode = model.Mode;

             await _context.SaveChangesAsync();
             return Ok(new { message = "Mode updated successfully!" });
         }*/