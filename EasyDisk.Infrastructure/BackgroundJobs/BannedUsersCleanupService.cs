using EasyDisk.Application.Interfaces.Audit;
using EasyDisk.Application.Interfaces.Files;
using EasyDisk.Domain.Entities;
using EasyDisk.Infrastructure.Data;
using EasyDisk.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.BackgroundJobs
{
    public class BannedUsersCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BannedUsersCleanupService> _logger;

        public BannedUsersCleanupService(IServiceProvider serviceProvider, ILogger<BannedUsersCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Banned Users Cleanup Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await WipeBannedUsersDataAsync();
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }

        private async Task WipeBannedUsersDataAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
                var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var deadline = DateTimeOffset.UtcNow.AddDays(-14);

                var usersToWipe = await userManager.Users
                    .Where(u => u.BannedAt.HasValue && u.BannedAt.Value <= deadline && !u.IsStorageWiped)
                    .ToListAsync();

                if (!usersToWipe.Any()) return;

                _logger.LogInformation($"Found {usersToWipe.Count} banned users for data wiping.");

                foreach (var user in usersToWipe)
                {
                    var files = await dbContext.Set<FileEntity>()
                        .Include(f => f.Versions)
                        .Where(f => f.OwnerId == user.Id)
                        .ToListAsync();

                    foreach (var file in files)
                    {
                        await fileStorageService.DeleteFileAsync(file.PhysicalPath);

                        foreach (var version in file.Versions)
                        {
                            if (!string.IsNullOrEmpty(version.PhysicalPath) && version.PhysicalPath != file.PhysicalPath)
                            {
                                await fileStorageService.DeleteFileAsync(version.PhysicalPath);
                            }
                        }
                    }

                    dbContext.Set<FileEntity>().RemoveRange(files);

                    var folders = await dbContext.Set<FolderEntity>()
                        .Where(f => f.OwnerId == user.Id)
                        .ToListAsync();

                    dbContext.Set<FolderEntity>().RemoveRange(folders);

                    user.UsedQuotaBytes = 0;
                    user.IsStorageWiped = true;
                    await userManager.UpdateAsync(user);

                    await dbContext.SaveChangesAsync();

                    await auditService.LogAsync(
                        action: "System.WipeBannedUserData",
                        entityType: "User",
                        entityId: user.Id,
                        details: new { Email = user.Email, FilesDeleted = files.Count, FoldersDeleted = folders.Count },
                        isSuccess: true
                    );

                    _logger.LogInformation($"Successfully wiped data for banned user {user.Email}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while wiping banned users data.");
            }
        }
    }
}
