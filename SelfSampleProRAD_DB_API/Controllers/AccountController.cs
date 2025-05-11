using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelfSampleProRAD_DB_API.DTOs;
using SelfSampleProRAD_DB_API.Models;
using SelfSampleProRAD_DB_API.Data;
using SelfSampleProRAD_DB_API.Services;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

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
            // First find the account by username only
            var accountEntity = await _context.Account
                .Include(a => a.Employee)
                .Where(a => a.UserName == request.UserName)
                .FirstOrDefaultAsync();

            // If no account found or password doesn't match (case-sensitive)
            if (accountEntity == null || accountEntity.Password != request.Password)
                return BadRequest("Invalid Username or Password.");

            // Check if account is deactivated
            if (accountEntity.Status == 'D') 
                return BadRequest("Account is deactivated.");

            // Generate JWT token
            var token = _jwtService.GenerateToken(accountEntity.Employee, accountEntity);

            // Map to DTO
            var account = new EmployeeResponseDTO()
            {
                EmployeeId = accountEntity.Employee.EmployeeId,
                FirstName = accountEntity.Employee.FirstName,
                LastName = accountEntity.Employee.LastName,
                Gender = accountEntity.Employee.Gender,
                Age = accountEntity.Employee.Age,
                Position = accountEntity.Employee.Position,
                Salary = accountEntity.Employee.Salary,
                Tax = accountEntity.Employee.Tax,
                Catagory = accountEntity.Employee.Category,
                accountdto = new AccountResponseDTO()
                {
                    UserId = accountEntity.UserId,
                    UserName = accountEntity.UserName,
                    Status = accountEntity.Status
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
                if (account.Password != request.OldPassword) return BadRequest("Old Password is incorrect.");
                account.Password = request.NewPassword;
                _context.Update(account);
                await _context.SaveChangesAsync();
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
                .Select(a => new AccountResponseDTO()
                {
                    UserId = a.UserId,
                    UserName = a.UserName,
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

    }
}