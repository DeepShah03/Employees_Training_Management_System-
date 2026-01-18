using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using ETMS.Models;
using ETMS.Data; // Adjust namespace if needed

[Authorize(Roles = "Trainer")]
public class EmployeeProgressController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private static readonly Dictionary<string, string> FeedbackCriteriaMap = new()
{
    { "Timeliness", "Punctuality & Attendance - Timeliness in attending training sessions" },
    { "Break Schedule", "Punctuality & Attendance - Adherence to break schedules" },
    { "Attendance", "Punctuality & Attendance - Consistency in attendance" },

    { "Interaction", "Participation & Engagement - Level of interaction during training" },
    { "Asking Questions", "Participation & Engagement - Willingness to ask questions" },
    { "Group Work", "Participation & Engagement - Contribution to group discussions" },

    { "Assessment Performance", "Learning Progress - Performance in assessments" },
    { "Application", "Learning Progress - Ability to apply learned concepts" },
    { "Improvement", "Learning Progress - Improvement over time" },

    { "Analysis", "Problem-Solving - Ability to analyze scenarios" },
    { "Troubleshooting", "Problem-Solving - Approach to troubleshooting issues" },
    { "Creativity", "Problem-Solving - Creativity in problem-solving" },

    { "Clarity", "Communication Skills - Clarity in expressing thoughts" },
    { "Confidence", "Communication Skills - Confidence in presenting information" },
    { "Listening", "Communication Skills - Active listening skills" },

    { "Teamwork", "Teamwork - Ability to work effectively in a team" },
    { "Support", "Teamwork - Willingness to support peers" },
    { "Adaptability", "Teamwork - Adaptability to different team roles" },

    { "Feedback Acceptance", "Adaptability - Readiness to accept feedback" },
    { "Learning", "Adaptability - Willingness to learn new concepts" },
    { "Training Styles", "Adaptability - Ability to adjust to training styles" },

    { "Hands-on", "Practical Application - Hands-on implementation of training" },
    { "Task Quality", "Practical Application - Quality and accuracy of task completion" },
    { "Efficiency", "Practical Application - Efficiency in real-world scenarios" },

    { "Respect", "Professionalism - Respect for trainers and peers" },
    { "Discipline", "Professionalism - Discipline in following guidelines" },
    { "Accountability", "Professionalism - Accountability for assigned tasks" },

    { "Beyond Basics", "Self-Motivation - Willingness to go beyond basic requirements" },
    { "Proactive", "Self-Motivation - Proactive approach to learning" },
    { "Additional Resources", "Self-Motivation - Seeking additional learning resources" }
};


    public EmployeeProgressController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        ViewBag.TrainerName = user?.UserName;
        var trainerId = _userManager.GetUserId(User); // Get the logged-in trainer ID

        // Fetch employees assigned to this trainer
        var employees = await _context.Users
            .Where(e => e.AssignedTrainerId == trainerId && e.Role == "Employee")
            .Select(e => new { e.Id, e.UserName })
            .ToListAsync();

        if (employees == null || !employees.Any()) // Ensure employees exist
        {
            ViewBag.Employees = new SelectList(new List<SelectListItem>()); // Provide empty list
        }
        else
        {
            ViewBag.Employees = new SelectList(employees, "Id", "UserName");
        }

        // Fetch existing reports for display
        var reports = await _context.EmployeeReports
    .Include(r => r.Employee)
    .Where(r => r.TrainerId != null && r.TrainerId == trainerId)
    .OrderByDescending(r => r.CreatedAt)
    .ToListAsync();


        return View(new EmployeeFeedbackViewModel { Reports = reports });
    }


   
    [HttpGet]
    public async Task<IActionResult> GetEmployeeTestMarks(string employeeId)
    {
        if (string.IsNullOrEmpty(employeeId))
            return BadRequest("Invalid Employee ID");

        var testResults = await _context.TestSubmissions
            .Where(ts => ts.UserId == employeeId)
            .Select(ts => new
            {
                TestType = ts.TestType,
                Marks = ts.Marks // Return individual marks, not sum
            })
            .ToListAsync();

        if (!testResults.Any())
            return Json(new { message = "No test records found." });

        return Json(testResults);
    }

    [HttpPost]
    public IActionResult GenerateReport(EmployeeFeedbackViewModel model, List<string> feedbackCriteria, IFormCollection formCollection)
    {
        if (ModelState.IsValid)
        {
            var trainerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value; // Get current trainer ID

            if (string.IsNullOrEmpty(trainerId))
            {
                ModelState.AddModelError("", "Trainer ID is missing.");
                return View(model);
            }

            // ✅ Fetch test marks from TestSubmissions instead of TestResults
            var preTrainingMarks = _context.TestSubmissions
                .Where(t => t.UserId == model.EmployeeId && t.TestType == "Pre-Training")
                .Select(t => t.Marks)
                .FirstOrDefault();

            var midTrainingMarks = _context.TestSubmissions
                .Where(t => t.UserId == model.EmployeeId && t.TestType == "Mid-Training")
                .Select(t => t.Marks)
                .FirstOrDefault();

            var postTrainingMarks = _context.TestSubmissions
                .Where(t => t.UserId == model.EmployeeId && t.TestType == "Post-Training")
                .Select(t => t.Marks)
                .FirstOrDefault();
            // Extract selected feedback criteria from the form collection
            var feedbackCriteriaList = formCollection.Keys
                .Where(k => k.StartsWith("feedbackCriteria["))
                .Select(k => formCollection[k])
                .ToList();

            var feedbackCriteriaString = feedbackCriteriaList.Any()
                ? string.Join(", ", feedbackCriteriaList)
                : "";

            var report = new EmployeeReport
            {
                Id = Guid.NewGuid(),
                EmployeeId = model.EmployeeId,
                TrainerId = _userManager.GetUserId(User),  // ✅ Include TrainerId
                PreTrainingMarks = preTrainingMarks > 0 ? preTrainingMarks : -1,
                MidTrainingMarks = midTrainingMarks > 0 ? midTrainingMarks : -1,
                PostTrainingMarks = postTrainingMarks > 0 ? postTrainingMarks : -1,
                ProjectMarks = model.ProjectMarks,
                Feedback = model.Feedback,

                /*FeedbackCriteria = feedbackCriteria != null ? string.Join(",", feedbackCriteria) : "",*/
                FeedbackCriteria = feedbackCriteriaString,
                CreatedAt = DateTime.Now
            };

            _context.EmployeeReports.Add(report);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        return View(model);
    }




    [HttpPost]
    public IActionResult DeleteReport(Guid reportId)
    {
        var report = _context.EmployeeReports.Find(reportId);
        if (report == null)
        {
            return NotFound();
        }

        _context.EmployeeReports.Remove(report);
        _context.SaveChanges();
        return Ok();
    }

    /*[HttpGet]
    public IActionResult GetReportDetails(Guid reportId) // Change int to Guid
    {
        var report = _context.EmployeeReports
            .Include(r => r.Employee)
            .FirstOrDefault(r => r.Id == reportId); // No type mismatch now

        if (report == null)
        {
            return NotFound();
        }

        return Json(new
        {
            employeeName = report.Employee?.UserName ?? "Unknown",
            preTrainingMarks = report.PreTrainingMarks >= 0
           ? report.PreTrainingMarks.ToString()  // Convert to string
           : "Remaining Test",
            midTrainingMarks = report.MidTrainingMarks >= 0
           ? report.MidTrainingMarks.ToString()  // Convert to string
           : "Remaining Test",
            postTrainingMarks = report.PostTrainingMarks >= 0
           ? report.PostTrainingMarks.ToString()  // Convert to string
           : "Remaining Test",
            projectMarks = report.ProjectMarks, // This remains an integer (if needed, convert it to string)
            feedback = report.Feedback,
            feedbackCriteria = string.IsNullOrWhiteSpace(report.FeedbackCriteria)
           ? new List<string>()
           : report.FeedbackCriteria.Split(',').Select(c => c.Trim()).ToList(),
            createdAt = report.CreatedAt.ToShortDateString()
        });

    }*/
    [HttpGet]
    public IActionResult GetReportDetails(Guid reportId)
    {
        var report = _context.EmployeeReports
            .Include(r => r.Employee)
            .FirstOrDefault(r => r.Id == reportId);

        if (report == null)
        {
            return NotFound();
        }

        // Process feedback criteria to get full questions
        var criteriaKeys = string.IsNullOrWhiteSpace(report.FeedbackCriteria)
            ? new List<string>()
            : report.FeedbackCriteria.Split(',').Select(c => c.Trim()).ToList();

        var fullQuestions = criteriaKeys
            .Select(key => FeedbackCriteriaMap.TryGetValue(key, out var question) ? question : key)
            .ToList();

        return Json(new
        {
            employeeName = report.Employee?.UserName ?? "Unknown",
            preTrainingMarks = report.PreTrainingMarks >= 0 ? report.PreTrainingMarks.ToString() : "Remaining Test",
            midTrainingMarks = report.MidTrainingMarks >= 0 ? report.MidTrainingMarks.ToString() : "Remaining Test",
            postTrainingMarks = report.PostTrainingMarks >= 0 ? report.PostTrainingMarks.ToString() : "Remaining Test",
            projectMarks = report.ProjectMarks,
            feedback = report.Feedback,
            feedbackCriteria = fullQuestions, // 🔥 Return full questions instead of just keys
            createdAt = report.CreatedAt.ToShortDateString()
        });
    }

}





















/*[HttpPost]
public IActionResult GenerateReport(EmployeeFeedbackViewModel model)
{
    if (ModelState.IsValid)
    {
        var employee = _context.Users.Find(model.EmployeeId);
        if (employee == null)
        {
            return BadRequest("Employee not found.");
        }

        // ✅ Fetch test marks from TestSubmissions instead of TestResults
        var preTrainingMarks = _context.TestSubmissions
            .Where(t => t.UserId == model.EmployeeId && t.TestType == "Pre-Training")
            .Select(t => t.Marks)
            .FirstOrDefault();

        var midTrainingMarks = _context.TestSubmissions
            .Where(t => t.UserId == model.EmployeeId && t.TestType == "Mid-Training")
            .Select(t => t.Marks)
            .FirstOrDefault();

        var postTrainingMarks = _context.TestSubmissions
            .Where(t => t.UserId == model.EmployeeId && t.TestType == "Post-Training")
            .Select(t => t.Marks)
            .FirstOrDefault();

        var report = new EmployeeReport
        {
            Id = Guid.NewGuid(),
            EmployeeId = model.EmployeeId,
            TrainerId = _userManager.GetUserId(User),  // ✅ Include TrainerId
            PreTrainingMarks = preTrainingMarks > 0 ? preTrainingMarks : -1,
            MidTrainingMarks = midTrainingMarks > 0 ? midTrainingMarks : -1,
            PostTrainingMarks = postTrainingMarks > 0 ? postTrainingMarks : -1,
            ProjectMarks = model.ProjectMarks,
            Feedback = model.Feedback,
            CreatedAt = DateTime.Now
        };

        _context.EmployeeReports.Add(report);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    return View(model);
}*/
/*[HttpPost]
public IActionResult GenerateReport(EmployeeFeedbackViewModel model, List<string> feedbackCriteria)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var report = new EmployeeReport
    {
        Id = Guid.NewGuid(),
        EmployeeId = model.EmployeeId,

        TrainerId = User.Identity.Name, // Assuming Trainer is logged in
        ProjectMarks = model.ProjectMarks,
        Feedback = model.Feedback,

        // ✅ Convert List<string> to a single comma-separated string
        FeedbackCriteria = feedbackCriteria != null ? string.Join(",", feedbackCriteria) : "",

        CreatedAt = DateTime.UtcNow
    };

    _context.EmployeeReports.Add(report);
    _context.SaveChanges();

    return RedirectToAction("Index");
}*/
/*    [HttpPost]
    public IActionResult GenerateReport(EmployeeFeedbackViewModel model, List<string> feedbackCriteria)
    {
        if (ModelState.IsValid)
        {
            var trainerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value; // Get current trainer ID

            if (string.IsNullOrEmpty(trainerId))
            {
                ModelState.AddModelError("", "Trainer ID is missing.");
                return View(model);
            }

            var report = new EmployeeReport
            {
                Id = Guid.NewGuid(),
                EmployeeId = model.EmployeeId,
                TrainerId = trainerId,
                ProjectMarks = model.ProjectMarks,
                Feedback = model.Feedback,
                FeedbackCriteria = feedbackCriteria != null ? string.Join(",", feedbackCriteria) : "",
                CreatedAt = DateTime.UtcNow
            };


            _context.EmployeeReports.Add(report);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        return View(model);
    }*/