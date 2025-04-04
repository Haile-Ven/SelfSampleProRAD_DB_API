using Microsoft.EntityFrameworkCore;
using SelfSampleProRAD_DB_API.Data;

namespace SelfSampleProRAD_DB_API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals;
                });

            // Configure Logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Information);
            // Disable EF Core query logging
            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Query", LogLevel.Error);
            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Error);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
            });

            // Add HttpContextAccessor
            builder.Services.AddHttpContextAccessor();

            // Add DbContext with explicit connection string to avoid environment variable override
            var sqlServerConnectionString = "Data Source=HAILE-WORK;Initial Catalog=EmployeeTaskDB;Integrated Security=True;Encrypt=True;Trust Server Certificate=True";
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(sqlServerConnectionString));
            builder.Services.AddScoped<SuperAdminSeeder>();

            var app = builder.Build();

            // Seed the database
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var seeder = services.GetRequiredService<SuperAdminSeeder>();
                    await seeder.SeedSuperAdminAsync();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => 
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
                    c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
                });
            }

            // Configure forwarded headers
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                                 Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
            });

            // Only use HTTPS redirection if not running behind a proxy (like ngrok)
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_FORWARDEDHEADERS_ENABLED")))
            {
                app.UseHttpsRedirection();
            }

            // Use CORS before auth
            app.UseCors("AllowAll");
            app.UseAuthorization();
            app.MapControllers();
            await app.RunAsync();
        }
    }
}
