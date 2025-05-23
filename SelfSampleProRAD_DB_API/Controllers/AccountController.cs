using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelfSampleProRAD_DB_API.Data;
using SelfSampleProRAD_DB_API.DTOs;
using SelfSampleProRAD_DB_API.Models;
using SelfSampleProRAD_DB_API.Services;

namespace SelfSampleProRAD_DB_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;
        private readonly PasswordHashService _passwordHashService;

        public AccountController(AppDbContext context, JwtService jwtService, PasswordHashService passwordHashService)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordHashService = passwordHashService;
        }

        /// <summary>
        /// Login with username and password
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<EmployeeResponseDTO>> Login([FromBody] LoginRequestDTO request)
        {
            // First find the account by username only
            var Account = await _context.Account
                .Include(a => a.Employee)
                .Where(a => a.UserName == request.UserName)
                .FirstOrDefaultAsync();

            // If no account found or password doesn't match
            if (Account == null || !_passwordHashService.VerifyPassword(request.Password, Account.Password))
                return BadRequest("Invalid Username or Password.");

            // Check if account is deactivated
            if (Account.Status == 'D') 
                return BadRequest("Account is deactivated.");

            // Generate JWT token
            var token = _jwtService.GenerateToken(Account.Employee, Account);

            // Map to DTO
            var account = new EmployeeResponseDTO()
            {
                EmployeeId = Account.Employee.EmployeeId,
                FirstName = Account.Employee.FirstName,
                LastName = Account.Employee.LastName,
                Gender = Account.Employee.Gender,
                Age = Account.Employee.Age,
                Position = Account.Employee.Position,
                Salary = Account.Employee.Salary,
                Tax = Account.Employee.Tax,
                Catagory = Account.Employee.Category,
                accountdto = new AccountResponseDTO()
                {
                    UserId = Account.UserId,
                    UserName = Account.UserName,
                    Status = Account.Status
                }
            };

            return Ok(new { Data = account, Token = token, Message = "Login Successful." });
        }

        /// <summary>
        /// Change password for a user
        /// </summary>
        [HttpPost("change-password")]
        [Authorize] // Any authenticated user can change their own password
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDTO request)
        {
            try
            {
                var employee = await _context.Employee.Where(e => e.EmployeeId == request.EmployeeId).FirstOrDefaultAsync();
                var account = await _context.Account.Where(a => a.UserId == employee.UserId).FirstOrDefaultAsync();
                if (account == null) return NotFound("Account not found.");
                if (!_passwordHashService.VerifyPassword(request.OldPassword, account.Password)) return BadRequest("Old Password is incorrect.");
                account.Password = _passwordHashService.HashPassword(request.NewPassword);
                _context.Update(account);
                await _context.SaveChangesAsync();
                
                // Delete any credential files associated with this username
                DeleteCredentialFiles(account.UserName);
                
                return Ok("Password Changed Successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Unable to Change Password.\nError: " + ex.Message);
            }
        }

        /// <summary>
        /// List all accounts
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "RequireAdmin")] // Only admins can list all accounts
        public async Task<ActionResult<List<AccountResponseDTO>>> ListAllAccounts()
        {
            var accounts = await _context.Account
                .Include(e => e.Employee)
                .Select(a => new AccountResponseDTO()
                {
                    UserId = a.UserId,
                    UserName = a.UserName,
                    FullName = $"{a.Employee.FirstName} {a.Employee.LastName}",
                    Status = a.Status,
                })
                .ToListAsync();
            return Ok(accounts);
        }

        /// <summary>
        /// Find account by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "RequireManager")] // Only managers can view account details
        public async Task<ActionResult<Account>> FindAccountByID(Guid id)
        {
            var account = await _context.Account
                .Where(a => a.UserId == id)
                .Select(a => new Account()
                {
                    UserId = a.UserId,
                    UserName = a.UserName,
                    Status = a.Status,
                })
                .FirstOrDefaultAsync();
            if (account == null)
                return NotFound();
                
            return Ok(account);
        }

        /// <summary>
        /// Change account status (Activate/Deactivate)
        /// </summary>
        [HttpPut("{accId}/toggle-status")]
        [Authorize(Policy = "RequireAdmin")] // Only admins can activate/deactivate accounts
        public async Task<ActionResult> ChangeAccountStatus(Guid accId)
        {
            var account = await _context.Account.Where(a => a.UserId == accId).FirstOrDefaultAsync();
            if (account == null) return NotFound("Account not found.");
            if (account.Status == 'A') account.Status = 'D';
            else account.Status = 'A';
            try
            {
                _context.Update(account);
                await _context.SaveChangesAsync();
                var msg = account.Status == 'A' ? "Account Activated Successfully." : "Account Deactivated Successfully.";
                return Ok(msg);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Unable to Change Account Status.\nError: " + ex.Message);
            }
        }
        
        /// <summary>
        /// Delete credential files associated with a username
        /// </summary>
        private void DeleteCredentialFiles(string username)
        {
            try
            {
                string credentialsDirectory = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "EmployeeCredentials");
                if (!System.IO.Directory.Exists(credentialsDirectory))
                {
                    return; // Directory doesn't exist, nothing to delete
                }
                
                // Get all text files in the credentials directory
                var files = System.IO.Directory.GetFiles(credentialsDirectory, "*.txt");
                
                // Find files that start with the username
                var matchingFiles = files.Where(f => System.IO.Path.GetFileName(f).StartsWith(username + "_")).ToList();
                
                // Delete all matching files
                foreach (var file in matchingFiles)
                {
                    System.IO.File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - we don't want to fail password change if file deletion fails
                Console.WriteLine($"Error deleting credential files: {ex.Message}");
            }
        }

    }
}