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

        public AccountController(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Login with username and password
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<EmployeeResponseDTO>> Login([FromBody] LoginRequestDTO request)
        {
            var Account = await _context.Account
                .Include(a => a.Employee)
                .Where(a => a.UserName == request.UserName)
                .FirstOrDefaultAsync();

            if (Account == null || !BCrypt.Net.BCrypt.EnhancedVerify(request.Password, Account.Password))
                return BadRequest("Invalid Username or Password.");

            if (Account.Status == 'D') 
                return BadRequest("Account is deactivated.");

            var token = _jwtService.GenerateToken(Account.Employee, Account);

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
                    Status = Account.Status,
                    FullName = $"{Account.Employee.FirstName} {Account.Employee.LastName}"
                }
            };

            return Ok(new { Data = account, Token = token, Message = "Login Successful." });
        }

        /// <summary>
        /// Change password for a user
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]         
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDTO request)
        {
            try
            {
                var employee = await _context.Employee.Where(e => e.EmployeeId == request.EmployeeId).FirstOrDefaultAsync();
                var account = await _context.Account.Where(a => a.UserId == employee.UserId).FirstOrDefaultAsync();
                if (account == null) return NotFound("Account not found.");
                if (!BCrypt.Net.BCrypt.EnhancedVerify(request.OldPassword, account.Password)) return BadRequest("Old Password is incorrect.");
                account.Password = BCrypt.Net.BCrypt.EnhancedHashPassword(request.NewPassword);
                _context.Update(account);
                await _context.SaveChangesAsync();
                
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
        [Authorize(Policy = "RequireAdmin")]       
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
        [Authorize(Policy = "RequireManager")]       
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
        [Authorize(Policy = "RequireAdmin")]      
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
                    return;       
                }
                
                var files = System.IO.Directory.GetFiles(credentialsDirectory, "*.txt");
                
                var matchingFiles = files.Where(f => System.IO.Path.GetFileName(f).StartsWith(username + "_")).ToList();
                
                foreach (var file in matchingFiles)
                {
                    System.IO.File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting credential files: {ex.Message}");
            }
        }

    }
}