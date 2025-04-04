using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelfSampleProRAD_DB.DTOs;
using SelfSampleProRAD_DB.Model;
using SelfSampleProRAD_DB_API.Data;
using System.Threading.Tasks;

namespace SelfSampleProRAD_DB.Controller
{
    [ApiController]
    [Route("api/[controller]")]
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
                    Catagory = employee.Category
                };

                _context.Employee.Add(newEmployee);
                await _context.SaveChangesAsync(); // Saves and generates EmployeeId

                var userName = $"{newEmployee.LastName}_{newEmployee.FirstName}@{newEmployee.EmployeeId.ToString().Substring(0,3)}";
                var account = new Account
                {
                    UserName = userName,
                    Password = GenerateRandomPassword(),
                    Status = 'A'
                };

                _context.Account.Add(account);
                await _context.SaveChangesAsync(); // Saves and generates AccountId/UserId

                // Link Employee to Account
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
        [HttpPut]
        public async Task<ActionResult> UpdateEmployee([FromBody] EmployeeEditDTO employee)
        {
            bool IsNameChanged = false;
            try
            {
                var emp = await _context.Employee.Where(e => e.EmployeeId == employee.EmployeeId).FirstOrDefaultAsync();
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
                    account.UserName = $"{emp.LastName}_{emp.FirstName}@{employee.EmployeeId.ToString().Substring(0, 3)}";
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
                    Catagory = e.Catagory,
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
                    Catagory = e.Catagory,
                    Account = e.Account
                }).FirstOrDefaultAsync();
                
            if (employee == null)
                return NotFound();
                
            return Ok(employee);
        }

        /// <summary>
        /// List all employees
        /// </summary>
        [HttpGet]
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
                    Catagory = e.Catagory,
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
                // Ensure at least one of each required character type
                upperCase[random.Next(upperCase.Length)],
                lowerCase[random.Next(lowerCase.Length)],
                numbers[random.Next(numbers.Length)],
                specials[random.Next(specials.Length)]
            };

            // Fill the rest with random characters from all types
            var allChars = upperCase + lowerCase + numbers + specials;
            var remainingLength = length - password.Count;

            password.AddRange(Enumerable.Range(0, remainingLength)
                .Select(_ => allChars[random.Next(allChars.Length)]));

            // Shuffle the password characters
            var shuffledPassword = password.OrderBy(_ => random.Next()).ToArray();
            return new string(shuffledPassword);
        }
    }
}
