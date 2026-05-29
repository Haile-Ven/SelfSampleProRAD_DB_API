using Microsoft.EntityFrameworkCore;
using SelfSampleProRAD_DB_API.Models;
using SelfSampleProRAD_DB_API.Services;
namespace SelfSampleProRAD_DB_API.Data
{
    public class SuperAdminSeeder
    {
        private readonly AppDbContext _context;
        private static readonly Guid SuperAdminEmployeeId = new Guid("67f9b050-06e6-4e38-979f-0ae6441b809d");
        private static readonly Guid SuperAdminUserId = new Guid("8f3c6011-5c48-40ba-a08d-2ddb43dfd61a");

        public SuperAdminSeeder(AppDbContext context)
        {
            _context = context;
        }

        public async Task SeedSuperAdminAsync()
        {
            var superAdminUserName = "Doe_John@111";

            if (await _context.Employee.AnyAsync(e => e.EmployeeId == SuperAdminEmployeeId))
                return;

            try
            {
                var employee = new Employee
                {
                    EmployeeId = SuperAdminEmployeeId,      
                    FirstName = "John",
                    LastName = "Doe",
                    Gender = 'M',
                    Age = 35,
                    Position = "Admin",
                    Category = "Permanent",
                    Salary = 50000,
                    Tax = 5000
                };

                _context.Employee.Add(employee);
                await _context.SaveChangesAsync();

                var account = new Account
                {
                    UserId = SuperAdminUserId,      
                    UserName = superAdminUserName,
                    Password = BCrypt.Net.BCrypt.EnhancedHashPassword("p@55w0rd"),
                    Status = 'A'
                };

                _context.Account.Add(account);
                await _context.SaveChangesAsync();

                employee.UserId = account.UserId;
                _context.Employee.Update(employee);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                
            }
        }
    }
}
