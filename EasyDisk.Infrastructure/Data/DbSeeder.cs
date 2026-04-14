using EasyDisk.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    logger.LogInformation($"Role '{roleName}' successfully created.");
                }
            }
            var adminEmail = configuration["AdminSettings:Email"];
            var adminPassword = configuration["AdminSettings:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                logger.LogWarning("Credits not found.");
                return;
            }

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var creatPowerUser = await userManager.CreateAsync(newAdmin, adminPassword);
                if (creatPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                    logger.LogInformation($"Admin '{adminEmail}' successfully created.");
                }
                else
                {
                    logger.LogError($"Error creating admin '{adminEmail}': {string.Join(", ", creatPowerUser.Errors.Select(e => e.Description))}");
                    foreach (var error in creatPowerUser.Errors)
                    {
                        logger.LogError($"Error: {error.Code} - {error.Description}");
                    }
                }
            }
        }
    }
}
