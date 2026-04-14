using EasyDisk.Application.DTOs;
using EasyDisk.Application.Interfaces;
using EasyDisk.Domain.Entities;
using EasyDisk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EasyDisk.Infrastructure.Repositories
{
    public class ShareLinkRepository : IShareLinkRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public ShareLinkRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ShareLinkEntity?> GetByTokenAsync(string token)
        {
            return await _dbContext.ShareLinks
                .Include(s => s.File)
                .FirstOrDefaultAsync(s => s.Token == token);
        }

        public Task AddAsync(ShareLinkEntity shareLink)
        {
            _dbContext.ShareLinks.Add(shareLink);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
