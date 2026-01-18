using ETMS.Data;
using ETMS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace ETMS.Controllers
{
    public class AssignTrainerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssignTrainerController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.AdminName = user?.UserName;

            // Fetch trainers and employees
            var trainers = _userManager.Users.Where(u => u.Role == "Trainer").ToList();
            var employees = _userManager.Users.Where(u => u.Role == "Employee").ToList();

            ViewBag.Trainers = trainers;
            ViewBag.Employees = employees;

            return View();
        }
        [HttpPost]
        public IActionResult Assign(string trainerId, List<string> employeeIds)
        {
            var trainer = _context.Users.FirstOrDefault(u => u.Id == trainerId);
            if (trainer == null) return NotFound();

            foreach (var employeeId in employeeIds)
            {
                var employee = _context.Users.FirstOrDefault(u => u.Id == employeeId);
                if (employee != null && employee.AssignedTrainerId != trainerId)
                {
                    employee.AssignedTrainerId = trainerId;
                    _context.Update(employee);
                }
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult Unassign(string employeeId)
        {
            var employee = _context.Users.FirstOrDefault(u => u.Id == employeeId);
            if (employee == null) return NotFound();

            employee.AssignedTrainerId = null; // Clear the trainer assignment
            _context.Update(employee);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UnassignAll(string trainerId)
        {
            // Find all employees assigned to this trainer
            var assignedEmployees = _context.Users
                .Where(u => u.AssignedTrainerId == trainerId)
                .ToList();

            if (assignedEmployees.Any())
            {
                foreach (var employee in assignedEmployees)
                {
                    employee.AssignedTrainerId = null; // Unassign
                }
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }


       
    }
}
