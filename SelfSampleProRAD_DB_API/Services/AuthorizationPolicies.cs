using Microsoft.AspNetCore.Authorization;

namespace SelfSampleProRAD_DB_API.Services
{
    public static class AuthorizationPolicies
    {
        public static void ConfigurePolicies(AuthorizationOptions options)
        {
            // Admin has highest level of access
            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireRole("Admin"));

            // Manager can do everything except admin-specific tasks
            options.AddPolicy("RequireManager", policy =>
                policy.RequireRole("Manager", "Admin"));

            // Developer can access developer-specific endpoints
            options.AddPolicy("RequireDeveloper", policy =>
                policy.RequireRole("Developer", "Manager", "Admin"));

            // Other employees have limited access
            options.AddPolicy("RequireEmployee", policy =>
                policy.RequireRole("Developer", "Manager", "Admin"));
        }
    }
}
