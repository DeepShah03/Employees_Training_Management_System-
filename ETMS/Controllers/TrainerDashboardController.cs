using ETMS.Data;
using ETMS.Models;
using ETMS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ETMS.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class TrainerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TrainerDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 🚀 1️⃣ Display the Manage Modules Page
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.TrainerName = user?.UserName;

            ViewBag.TotalEmployees = await _userManager.Users.CountAsync(u => u.Role == "Employee");
            ViewBag.PendingTests = 5;
            ViewBag.ActiveTrainings = 3;

            return View();
        }



        public async Task<IActionResult> ManageModules()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.TrainerName = user?.UserName;
            var userId = _userManager.GetUserId(User);
            var modules = await _context.TrainingModules
                .Where(m => m.TrainerId == userId)
                .ToListAsync();

            return View(modules);
        }
        // 🚀 6️⃣ Delete a module (folder)
        [HttpPost]
        public async Task<IActionResult> DeleteModule(int moduleId)
        {
            var module = await _context.TrainingModules.FindAsync(moduleId);
            if (module == null) return NotFound();

            // Delete all associated learning materials
            var materials = await _context.LearningMaterials.Where(m => m.ModuleId == moduleId).ToListAsync();
            foreach (var material in materials)
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + material.FilePath);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.LearningMaterials.RemoveRange(materials);
            _context.TrainingModules.Remove(module);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Module and all associated materials deleted successfully!";
            return RedirectToAction("ManageModules");
        }


        // 🚀 2️⃣ Create a new subject/module folder
        [HttpPost]
        public async Task<IActionResult> CreateModule(string subjectName)
        {
           
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(subjectName))
            {
                TempData["ErrorMessage"] = "Subject name cannot be empty.";
                return RedirectToAction("ManageModules");
            }

            var newModule = new TrainingModule
            {
                TrainerId = userId,
                SubjectName = subjectName
            };

            _context.TrainingModules.Add(newModule);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageModules");
        }

        // 🚀 3️⃣ Show materials inside a module
        public async Task<IActionResult> ModuleMaterials(int moduleId)
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.TrainerName = user?.UserName;
            var module = await _context.TrainingModules.FindAsync(moduleId);
            if (module == null) return NotFound();

            var materials = await _context.LearningMaterials
                .Where(m => m.ModuleId == moduleId)
                .ToListAsync();

            var viewModel = new ModuleMaterialsViewModel
            {
                ModuleId = module.Id,
                ModuleName = module.SubjectName,
                LearningMaterials = materials
            };

            return View(viewModel);
        }

        // 🚀 4️⃣ Upload materials (PDF or MP4)
        [HttpPost]
        public async Task<IActionResult> UploadMaterial(int moduleId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid file.";
                return RedirectToAction("ModuleMaterials", new { moduleId });
            }

            var allowedExtensions = new[] { ".pdf", ".mp4" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "Invalid file type. Only PDF and MP4 files are allowed.";
                return RedirectToAction("ModuleMaterials", new { moduleId });
            }

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            Directory.CreateDirectory(uploadsFolder);

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName); // Extract filename without extension
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var learningMaterial = new LearningMaterial
            {
                ModuleId = moduleId,
                FileName = fileNameWithoutExt, // Save the filename without extension
                FilePath = "/uploads/" + uniqueFileName,
                FileType = fileExtension == ".mp4" ? "Video" : "PDF"
            };

            _context.LearningMaterials.Add(learningMaterial);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "File uploaded successfully!";
            return RedirectToAction("ModuleMaterials", new { moduleId });
        }


        // 🚀 5️⃣ Delete material
        [HttpPost]
        public async Task<IActionResult> DeleteMaterial(int id, int moduleId)
        {
            var material = await _context.LearningMaterials.FindAsync(id);
            if (material == null) return NotFound();

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + material.FilePath);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.LearningMaterials.Remove(material);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material deleted successfully!";
            return RedirectToAction("ModuleMaterials", new { moduleId });
        }

       
        public async Task<IActionResult> ManageTraining()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.TrainerName = user?.UserName;
            var trainerId = _userManager.GetUserId(User);

            // Fetch only employees assigned to this trainer
            var assignedEmployees = await _userManager.Users
                .Where(u => u.Role == "Employee" && u.AssignedTrainerId == trainerId)
                .Include(e => e.AssignedModules)
                .ThenInclude(am => am.Module)
                .ToListAsync();

            // ✅ Fetch only modules created by this trainer
            var modules = await _context.TrainingModules
                .Where(m => m.TrainerId == trainerId) // ✅ Filter by TrainerId
                .ToListAsync();

            ViewBag.Modules = modules; // ✅ Now contains only the logged-in trainer's modules

            return View(assignedEmployees);
        }


        [HttpPost]
        public async Task<IActionResult> AssignModule(string employeeId, int moduleId)
        {
            if (string.IsNullOrEmpty(employeeId) || moduleId <= 0)
            {
                return BadRequest("Invalid input.");
            }

            var existingAssignment = await _context.EmployeeModules
                .FirstOrDefaultAsync(em => em.EmployeeId == employeeId && em.ModuleId == moduleId);

            if (existingAssignment == null)
            {
                var newAssignment = new EmployeeModule
                {
                    EmployeeId = employeeId,
                    ModuleId = moduleId
                };

                _context.EmployeeModules.Add(newAssignment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageTraining");
        }

        public async Task<IActionResult> UnassignModule(string employeeId, int moduleId)
        {
            if (string.IsNullOrEmpty(employeeId) || moduleId <= 0)
            {
                return BadRequest("Invalid input.");
            }

            var assignment = await _context.EmployeeModules
                .FirstOrDefaultAsync(em => em.EmployeeId == employeeId && em.ModuleId == moduleId);

            if (assignment != null)
            {
                _context.EmployeeModules.Remove(assignment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageTraining");
        }
        [HttpPost]
        public async Task<IActionResult> UnassignAllModules(string employeeId)
        {
            if (string.IsNullOrEmpty(employeeId))
            {
                return BadRequest("Invalid employee ID.");
            }

            var assignedModules = await _context.EmployeeModules
                .Where(em => em.EmployeeId == employeeId)
                .ToListAsync();

            if (assignedModules.Any())
            {
                _context.EmployeeModules.RemoveRange(assignedModules);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "All modules unassigned successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "No modules found to unassign.";
            }

            return RedirectToAction("ManageTraining");
        }
        // GET: List all projects
        public async Task<IActionResult> ManageProjects()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.TrainerName = user?.UserName;
            var trainerId = _userManager.GetUserId(User); // ✅ Get the logged-in trainer ID

            var projects = await _context.Projects
                .Where(p => p.TrainerId == trainerId) // ✅ Filter projects by trainer
                .ToListAsync();
            /* var projects = _context.Projects.ToList();*/
            return View(projects);
        }


        // POST: Create a project
        [HttpPost]
        public IActionResult CreateProject(string projectName, string projectDescription, DateTime deadline)
        {
            var trainerId = _userManager.GetUserId(User); // ✅ Get logged-in trainer's ID
            if (string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(projectDescription))
            {
                TempData["Error"] = "Project name and description are required.";
                return RedirectToAction("ManageProjects");
            }

            var project = new Project
            {
                ProjectName = projectName,
                Description = projectDescription,
                Deadline = deadline,
                TrainerId = trainerId // ✅ Store trainer's ID
            };

            _context.Projects.Add(project);
            _context.SaveChanges();

            TempData["Success"] = "Project created successfully!";
            return RedirectToAction("ManageProjects");
        }

        // POST: Delete a project
        [HttpPost]
        public IActionResult DeleteProject(int projectId)
        {
            var project = _context.Projects.Find(projectId);
            if (project == null)
            {
                TempData["Error"] = "Project not found.";
                return RedirectToAction("ManageProjects");
            }

            _context.Projects.Remove(project);
            _context.SaveChanges();

            TempData["Success"] = "Project deleted successfully!";
            return RedirectToAction("ManageProjects");
        }
        /*        public async Task<IActionResult> ManagePro()
                {
                    var user = await _userManager.GetUserAsync(User);
                    ViewBag.TrainerName = user?.UserName;

                    var trainerId = _userManager.GetUserId(User);

                    var assignedEmployees = await _userManager.Users
                    .Where(u => u.Role == "Employee" && u.AssignedTrainerId == trainerId)
                     .Include(e => e.AssignedProjects)
                        .ThenInclude(ap => ap.Project)
                    .ToListAsync();

                    var employeesWithProjects = assignedEmployees.Select(e => new ApplicationUser
                    {
                        Id = e.Id,
                        UserName = e.UserName,
                        AssignedTrainerId = e.AssignedTrainerId,
                        AssignedProjects = e.AssignedProjects != null ? e.AssignedProjects.ToList() : new List<EmployeeProject>()
                    }).ToList();


                    var projects = await _context.Projects.ToListAsync();
                    ViewBag.Projects = projects;

                    return View(employeesWithProjects);
                }*/
        public async Task<IActionResult> ManagePro()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.TrainerName = user?.UserName;

            var trainerId = _userManager.GetUserId(User);

            var assignedEmployees = await _userManager.Users
                .Where(u => u.Role == "Employee" && u.AssignedTrainerId == trainerId)
                .Include(e => e.AssignedProjects)
                .ThenInclude(ap => ap.Project)
                .ToListAsync();

            var employeesWithProjects = assignedEmployees.Select(e => new ApplicationUser
            {
                Id = e.Id,
                UserName = e.UserName,
                AssignedTrainerId = e.AssignedTrainerId,
                AssignedProjects = e.AssignedProjects != null ? e.AssignedProjects.ToList() : new List<EmployeeProject>()
            }).ToList();

            // Filter projects by TrainerId to show only the current trainer's projects
            var projects = await _context.Projects
                .Where(p => p.TrainerId == trainerId) // Filter by current trainer's ID
                .ToListAsync();

            ViewBag.Projects = projects;

            return View(employeesWithProjects);
        }

        [HttpPost]
        public async Task<IActionResult> AssignProject(string employeeId, int projectId)
        {

            if (string.IsNullOrEmpty(employeeId) || projectId <= 0)
            {
                return BadRequest("Invalid input.");
            }

            var trainerId = _userManager.GetUserId(User); // Get the logged-in trainer's ID

            // Ensure the project belongs to the logged-in trainer
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId && p.TrainerId == trainerId);

            if (project == null)
            {
                TempData["ErrorMessage"] = "You cannot assign a project that doesn't belong to you.";
                return RedirectToAction("ManagePro");
            }

            var existingAssignment = await _context.EmployeeProjects
                .FirstOrDefaultAsync(ep => ep.EmployeeId == employeeId && ep.ProjectId == projectId);

            if (existingAssignment == null)
            {
                var newAssignment = new EmployeeProject
                {
                    EmployeeId = employeeId,
                    ProjectId = projectId
                };

                _context.EmployeeProjects.Add(newAssignment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManagePro");
        }


        /* [HttpPost]
         public async Task<IActionResult> AssignProject(string employeeId, int projectId)
         {
             if (string.IsNullOrEmpty(employeeId) || projectId <= 0)
             {
                 return BadRequest("Invalid input.");
             }

             var existingAssignment = await _context.EmployeeProjects
                 .FirstOrDefaultAsync(ep => ep.EmployeeId == employeeId && ep.ProjectId == projectId);

             if (existingAssignment == null)
             {
                 var newAssignment = new EmployeeProject
                 {
                     EmployeeId = employeeId,
                     ProjectId = projectId
                 };

                 _context.EmployeeProjects.Add(newAssignment);
                 await _context.SaveChangesAsync();
             }

             return RedirectToAction("ManagePro");
         }*/
        [HttpPost]
        public async Task<IActionResult> UnassignProject(string employeeId, int projectId)
        {
            if (string.IsNullOrEmpty(employeeId) || projectId <= 0)
            {
                return BadRequest("Invalid input.");
            }

            var assignment = await _context.EmployeeProjects
                .FirstOrDefaultAsync(ep => ep.EmployeeId == employeeId && ep.ProjectId == projectId);

            if (assignment != null)
            {
                _context.EmployeeProjects.Remove(assignment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManagePro");
        }
        [HttpPost]
        public async Task<IActionResult> UnassignAllProjects(string employeeId)
        {
            if (string.IsNullOrEmpty(employeeId))
            {
                return BadRequest("Invalid employee ID.");
            }

            var assignedProjects = await _context.EmployeeProjects
                .Where(ep => ep.EmployeeId == employeeId)
                .ToListAsync();

            if (assignedProjects.Any())
            {
                _context.EmployeeProjects.RemoveRange(assignedProjects);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "All projects unassigned successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "No projects found to unassign.";
            }

            return RedirectToAction("ManagePro");
        }


        /*
           public IActionResult ProjectSubmissions()
           {
               var trainerId = _userManager.GetUserId(User); // Get logged-in trainer's ID

               var submissions = _context.ProjectSubmissions
                   .Include(ps => ps.Project)
                   .Include(ps => ps.Employee)
                   .Where(ps => _context.EmployeeProjects
                       .Any(ep => ep.ProjectId == ps.ProjectId && ep.EmployeeId == ps.EmployeeId))
                   .Select(ps => new
                   {
                       EmployeeName = ps.Employee.UserName,
                       ProjectName = ps.Project.ProjectName,
                       Deadline = ps.Project.Deadline,
                       SubmissionDate = ps.SubmittedAt,
                       FilePath = ps.FilePath
                   })
                   .ToList();

               return View(submissions);
           }*/
        public IActionResult ProjectSubmissions()
        {
            var trainerId = _userManager.GetUserId(User); // Get logged-in trainer's ID

            var submissions = _context.ProjectSubmissions
                .Include(ps => ps.Project)
                .Include(ps => ps.Employee)
                .Where(ps => ps.Project.TrainerId == trainerId) // Ensure the submission is for the trainer's project
                .Select(ps => new
                {
                    EmployeeName = ps.Employee.UserName,
                    ProjectName = ps.Project.ProjectName,
                    Deadline = ps.Project.Deadline,
                    SubmissionDate = ps.SubmittedAt,
                    FilePath = ps.FilePath
                })
                .ToList();

            return View(submissions);
        }





    }

}










/*using ETMS.Data;
using ETMS.Models;
using ETMS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ETMS.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class TrainerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TrainerDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 🚀 1️⃣ Display the Manage Modules Page
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.TrainerName = user?.UserName;

            

            return View();
        }



        public async Task<IActionResult> ManageModules()
        {
            var userId = _userManager.GetUserId(User);
            var modules = await _context.TrainingModules
                .Where(m => m.TrainerId == userId)
                .ToListAsync();

            return View(modules);
        }

        // 🚀 2️⃣ Create a new subject/module folder
        [HttpPost]
        public async Task<IActionResult> CreateModule(string subjectName)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(subjectName))
            {
                TempData["ErrorMessage"] = "Subject name cannot be empty.";
                return RedirectToAction("ManageModules");
            }

            var newModule = new TrainingModule
            {
                TrainerId = userId,
                SubjectName = subjectName
            };

            _context.TrainingModules.Add(newModule);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageModules");
        }

        // 🚀 3️⃣ Show materials inside a module
        public async Task<IActionResult> ModuleMaterials(int moduleId)
        {
            var module = await _context.TrainingModules.FindAsync(moduleId);
            if (module == null) return NotFound();

            var materials = await _context.LearningMaterials
                .Where(m => m.ModuleId == moduleId)
                .ToListAsync();

            var viewModel = new ModuleMaterialsViewModel
            {
                ModuleId = module.Id,
                ModuleName = module.SubjectName,
                LearningMaterials = materials
            };

            return View(viewModel);
        }

        // 🚀 4️⃣ Upload materials (PDF or MP4)
        [HttpPost]
        public async Task<IActionResult> UploadMaterial(int moduleId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid file.";
                return RedirectToAction("ModuleMaterials", new { moduleId });
            }

            var allowedExtensions = new[] { ".pdf", ".mp4" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "Invalid file type. Only PDF and MP4 files are allowed.";
                return RedirectToAction("ModuleMaterials", new { moduleId });
            }

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            Directory.CreateDirectory(uploadsFolder);
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var learningMaterial = new LearningMaterial
            {
                ModuleId = moduleId,
                FileName = file.FileName,
                FilePath = "/uploads/" + uniqueFileName,
                FileType = fileExtension == ".mp4" ? "Video" : "PDF"
            };

            _context.LearningMaterials.Add(learningMaterial);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "File uploaded successfully!";
            return RedirectToAction("ModuleMaterials", new { moduleId });
        }

        // 🚀 5️⃣ Delete material
        [HttpPost]
        public async Task<IActionResult> DeleteMaterial(int id, int moduleId)
        {
            var material = await _context.LearningMaterials.FindAsync(id);
            if (material == null) return NotFound();

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + material.FilePath);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.LearningMaterials.Remove(material);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material deleted successfully!";
            return RedirectToAction("ModuleMaterials", new { moduleId });
        }
    }
}
*/