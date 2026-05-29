using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;
using SelfSampleProRAD_DB_API.DTOs;

namespace SelfSampleProRAD_DB_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireAdmin")]
    public class CredentialsController : ControllerBase
    {
        private readonly string _credentialsDirectory;
        private readonly ILogger<CredentialsController> _logger;

        public CredentialsController(ILogger<CredentialsController> logger)
        {
            _logger = logger;
            _credentialsDirectory = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "EmployeeCredentials");
            
            if (!System.IO.Directory.Exists(_credentialsDirectory))
            {
                System.IO.Directory.CreateDirectory(_credentialsDirectory);
            }
        }

        /// <summary>
        /// Get credential file content by username
        /// </summary>
        [HttpGet("{username}")]
        public ActionResult<CredentialDTO> GetCredentialByUsername(string username)
        {
            try
            {
                if (!System.IO.Directory.Exists(_credentialsDirectory))
                {
                    return NotFound("No credential files found.");
                }
                
                var files = System.IO.Directory.GetFiles(_credentialsDirectory, "*.txt");
                
                var matchingFiles = new List<string>();
                foreach (var file in files)
                {
                    string content = System.IO.File.ReadAllText(file);
                    if (content.Contains($"Username: {username}", StringComparison.OrdinalIgnoreCase))
                    {
                        matchingFiles.Add(file);
                    }
                }
                
                if (matchingFiles.Count == 0)
                {
                    return NotFound($"No credential file found for username '{username}'.");
                }
                
                string mostRecentFile = matchingFiles
                    .OrderByDescending(f => new System.IO.FileInfo(f).CreationTime)
                    .First();
                
                string fileContent = System.IO.File.ReadAllText(mostRecentFile);
                
                var credentialInfo = ParseCredentialFileContent(fileContent);
                
                return Ok(credentialInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving credential: {ex.Message}");
            }
        }
        
        private CredentialDTO ParseCredentialFileContent(string content)
        {
            var result = new CredentialDTO();
            
            var dateMatch = Regex.Match(content, @"Date Created: (.*?)\r?\n");
            if (dateMatch.Success)
            {
                result.DateCreated = dateMatch.Groups[1].Value.Trim();
            }
            
            var employeeMatch = Regex.Match(content, @"Employee: (.*?)\r?\n");
            if (employeeMatch.Success)
            {
                result.Employee = employeeMatch.Groups[1].Value.Trim();
            }
            
            var usernameMatch = Regex.Match(content, @"Username: (.*?)\r?\n");
            if (usernameMatch.Success)
            {
                result.Username = usernameMatch.Groups[1].Value.Trim();
            }
            
            var passwordMatch = Regex.Match(content, @"Password: (.*?)\r?\n");
            if (passwordMatch.Success)
            {
                result.Password = passwordMatch.Groups[1].Value.Trim();
            }
            
            return result;
        }
    }
}
