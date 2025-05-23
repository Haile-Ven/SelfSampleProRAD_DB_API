using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
            // Set the credentials directory path
            _credentialsDirectory = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "EmployeeCredentials");
            
            // Ensure the directory exists
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
                
                // Get all text files in the credentials directory
                var files = System.IO.Directory.GetFiles(_credentialsDirectory, "*.txt");
                
                // Find files that might contain the username
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
                
                // Use the most recent file if multiple matches found
                string mostRecentFile = matchingFiles
                    .OrderByDescending(f => new System.IO.FileInfo(f).CreationTime)
                    .First();
                
                string fileContent = System.IO.File.ReadAllText(mostRecentFile);
                
                // Parse the content to extract credential information
                var credentialInfo = ParseCredentialFileContent(fileContent);
                
                return Ok(credentialInfo);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error retrieving credential by username");
                return StatusCode(500, $"Error retrieving credential: {ex.Message}");
            }
        }
        
        private CredentialDTO ParseCredentialFileContent(string content)
        {
            var result = new CredentialDTO();
            
            // Use regular expressions to extract the key information
            // Extract date created
            var dateMatch = Regex.Match(content, @"Date Created: (.*?)\r?\n");
            if (dateMatch.Success)
            {
                result.DateCreated = dateMatch.Groups[1].Value.Trim();
            }
            
            // Extract employee name
            var employeeMatch = Regex.Match(content, @"Employee: (.*?)\r?\n");
            if (employeeMatch.Success)
            {
                result.Employee = employeeMatch.Groups[1].Value.Trim();
            }
            
            // Extract username
            var usernameMatch = Regex.Match(content, @"Username: (.*?)\r?\n");
            if (usernameMatch.Success)
            {
                result.Username = usernameMatch.Groups[1].Value.Trim();
            }
            
            // Extract password
            var passwordMatch = Regex.Match(content, @"Password: (.*?)\r?\n");
            if (passwordMatch.Success)
            {
                result.Password = passwordMatch.Groups[1].Value.Trim();
            }
            
            return result;
        }
    }
}
