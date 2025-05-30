using Microsoft.IdentityModel.Tokens;
using SelfSampleProRAD_DB_API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SelfSampleProRAD_DB_API.Services
{
    public class JwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(Employee employee, Account account)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "DefaultSecretKey12345678901234567890"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("userId", account.UserId.ToString()),
                new Claim("employeeId", employee.EmployeeId.ToString()),
                new Claim(ClaimTypes.Role, employee.Position) // Using Position as the role
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "SelfSampleProRAD_DB_API",
                audience: _configuration["Jwt:Audience"] ?? "SelfSampleProRAD_DB_API_Users",
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static Guid ExtractEmployeeIDClaimsFromJWT(ClaimsPrincipal User)
        {
            string? employeeID = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            if (employeeID == null) return Guid.Empty;
            return Guid.Parse(employeeID);
        }
    }
}
