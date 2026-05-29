using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelfSampleProRAD_DB_API.DTOs;
using SelfSampleProRAD_DB_API.Models;
using SelfSampleProRAD_DB_API.Data;
using SelfSampleProRAD_DB_API.Services;
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace SelfSampleProRAD_DB_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]      
    public class EmployeeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmployeeController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Add a new employee
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]      
        public async Task<ActionResult> AddEmployee([FromBody] AddEmployeeDTO employee)
        {
            float salary;
            float tax;
            calaculateTax(employee.Position, out salary, out tax);
            var existingEmployee = await _context.Employee
                .Where(e => e.FirstName == employee.FirstName && e.LastName == employee.LastName)
                .FirstOrDefaultAsync();
            if (existingEmployee != null) return BadRequest($"Employee {existingEmployee.FirstName} {existingEmployee.LastName} Already Exisits");
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var newEmployee = new Employee
                {
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    Gender = employee.Gender,
                    Age = employee.Age,
                    Position = employee.Position,
                    Salary = salary,
                    Tax = tax,
                    Category = employee.Category
                };

                _context.Employee.Add(newEmployee);
                await _context.SaveChangesAsync();     

                var userName = $"{newEmployee.LastName}_{newEmployee.FirstName}@{newEmployee.EmployeeId.ToString().Substring(0, 3)}";
                string randomPassword = GenerateRandomPassword();
                var account = new Account
                {
                    UserName = userName,
                    Password = BCrypt.Net.BCrypt.EnhancedHashPassword(randomPassword),
                    Status = 'A'
                };
                
                SaveCredentialsToFile(newEmployee.FirstName, newEmployee.LastName, userName, randomPassword);

                _context.Account.Add(account);
                await _context.SaveChangesAsync();     

                newEmployee.UserId = account.UserId;
                await _context.SaveChangesAsync();

                transaction.Commit();
                return Ok("Successfully added an Employee and created an Account");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Update an existing employee
        /// </summary>
        [HttpPut("{employeeId}")]
        [Authorize(Policy = "RequireEmployee")]
        public async Task<ActionResult> UpdateEmployee(Guid employeeID, [FromBody] EmployeeEditDTO employee)
        {
            bool IsNameChanged = false;
            try
            {
                var emp = await _context.Employee.Where(e => e.EmployeeId == employeeID).FirstOrDefaultAsync();
                if (emp == null) return NotFound("Employee not found.");
                if (emp.FirstName == employee.FirstName || emp.LastName == employee.LastName) IsNameChanged = true;
                emp.FirstName = employee.FirstName;
                emp.LastName = employee.LastName;
                emp.Age = employee.Age;
                emp.Gender = employee.Gender;
                _context.Employee.Update(emp);
                await _context.SaveChangesAsync();
                if (IsNameChanged)
                {
                    var account = await _context.Account.Where(a => a.UserId == emp.UserId).FirstOrDefaultAsync();
                    account.UserName = $"{emp.LastName}_{emp.FirstName}@{employeeID.ToString().Substring(0, 3)}";
                    _context.Account.Update(account);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Unable to Update Employee.\nError: " + ex.Message);
            }
            return Ok("Employee Updated Successfully.");
        }

        /// <summary>
        /// Get employee by ID
        /// </summary>
        [HttpGet("{employeeId}")]
        [Authorize(Policy = "RequireEmployee")]       
        public async Task<ActionResult<Employee>> SelectEmployee(Guid employeeId)
        {
            var employee = await _context.Employee
                .Where(e => e.EmployeeId == employeeId)
                .Select(e => new Employee
                {
                    EmployeeId = e.EmployeeId,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Gender = e.Gender,
                    Age = e.Age,
                    Position = e.Position,
                    Salary = e.Salary,
                    Tax = e.Tax,
                    Category = e.Category,
                    Account = e.Account
                }).FirstOrDefaultAsync();
                
            if (employee == null)
                return NotFound();
                
            return Ok(employee);
        }

        /// <summary>
        /// Get employee by user ID
        /// </summary>
        [HttpGet("by-user/{userId}")]
        [Authorize(Policy = "RequireEmployee")]       
        public async Task<ActionResult<Employee>> SelectEmployeeByUserId(Guid userId)
        {
            var employee = await _context.Employee
                .Where(e => e.UserId == userId)
                .Select(e => new Employee
                {
                    EmployeeId = e.EmployeeId,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Gender = e.Gender,
                    Age = e.Age,
                    Position = e.Position,
                    Salary = e.Salary,
                    Tax = e.Tax,
                    Category = e.Category,
                    Account = e.Account
                }).FirstOrDefaultAsync();
                
            if (employee == null)
                return NotFound();
                
            return Ok(employee);
        }

        /// <summary>
        /// List all developers
        /// </summary>
        [HttpGet("developers")]
        [Authorize(Policy = "RequireEmployee")]       
        public async Task<ActionResult<List<DevEmployeeResponseDTO>>> ListAllDevs()
        {
            var devs = await _context.Employee
                .Where(a => a.Position == "Developer")
                .Select(a => new DevEmployeeResponseDTO()
                {
                    EmployeeID = a.EmployeeId,
                    FullName = $"{a.FirstName} {a.LastName}"
                })
                .ToListAsync();
            return Ok(devs);
        }

        /// <summary>
        /// List all employees
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "RequireAdmin")]       
        public async Task<ActionResult<List<EmployeeResponseDTO>>> ListAllEmployees()
        {
            var employees = await _context.Employee
                .Select(e => new EmployeeResponseDTO
                {
                    EmployeeId = e.EmployeeId,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Gender = e.Gender,
                    Age = e.Age,
                    Position = e.Position,
                    Salary = e.Salary,
                    Tax = e.Tax,
                    Catagory = e.Category,
                    accountdto = new AccountResponseDTO
                    {
                        UserId = e.Account.UserId,
                        UserName = e.Account.UserName,
                        Status = e.Account.Status
                    }
                }).ToListAsync();
            return Ok(employees);
        }

        private void calaculateTax(string Position, out float Salary, out float Tax)
        {
            if (Position == "Developer")
            {
                Salary = 20000f;
                Tax = Salary * (25F / 100);
            }
            else if (Position == "Manager")
            {
                Salary = 30000f;
                Tax = Salary * (35f / 100);
            }
            else 
            {
                Salary = 10000f;
                Tax = Salary * (15f / 100);
            }
        }
        private string GenerateRandomPassword(int length = 12)
        {
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string numbers = "0123456789";
            const string specials = "!@#$%^&*";

            var random = new Random();
            var password = new List<char>
            {
                upperCase[random.Next(upperCase.Length)],
                lowerCase[random.Next(lowerCase.Length)],
                numbers[random.Next(numbers.Length)],
                specials[random.Next(specials.Length)]
            };

            var allChars = upperCase + lowerCase + numbers + specials;
            var remainingLength = length - password.Count;

            password.AddRange(Enumerable.Range(0, remainingLength)
                .Select(_ => allChars[random.Next(allChars.Length)]));

            var shuffledPassword = password.OrderBy(_ => random.Next()).ToArray();
            return new string(shuffledPassword);
        }
        
        private void SaveCredentialsToFile(string firstName, string lastName, string username, string password)
        {
            try
            {
                string directoryPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "EmployeeCredentials");
                if (!System.IO.Directory.Exists(directoryPath))
                {
                    System.IO.Directory.CreateDirectory(directoryPath);
                }
                
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"{username}_{timestamp}.txt";
                string filePath = System.IO.Path.Combine(directoryPath, fileName);
                
                StringBuilder content = new StringBuilder();
                content.AppendLine("EMPLOYEE CREDENTIALS - CONFIDENTIAL");
                content.AppendLine("===============================");
                content.AppendLine($"Date Created: {DateTime.Now}");
                content.AppendLine($"Employee: {firstName} {lastName}");
                content.AppendLine($"Username: {username}");
                content.AppendLine($"Password: {password}");
                content.AppendLine("\nPlease change your password after first login.");
                content.AppendLine("This is an automatically generated file.");
                
                System.IO.File.WriteAllText(filePath, content.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving credentials to file: {ex.Message}");
            }
        }
    }
}
