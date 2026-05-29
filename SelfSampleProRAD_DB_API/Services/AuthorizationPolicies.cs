using Microsoft.AspNetCore.Authorization;

namespace SelfSampleProRAD_DB_API.Services
{
    public static class AuthorizationPolicies
    {
        public static void ConfigurePolicies(AuthorizationOptions options)
        {
            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy("RequireManager", policy =>
                policy.RequireRole("Manager", "Admin"));

            options.AddPolicy("RequireDeveloper", policy =>
                policy.RequireRole("Developer", "Manager", "Admin"));

            options.AddPolicy("RequireEmployee", policy =>
                policy.RequireRole("Developer", "Manager", "Admin"));
        }
    }
}
