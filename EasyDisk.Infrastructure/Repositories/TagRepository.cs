using EasyDisk.Application.Interfaces.Tag;
using EasyDisk.Domain.Entities;
using EasyDisk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public TagRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(TagEntity tag)
        {
            await _dbContext.Tags.AddAsync(tag);
        }

        public void Delete(TagEntity tag)
        {
            _dbContext.Tags.Remove(tag);
        }

        public async Task<List<TagEntity>> GetAllByOwnerAsync(string ownerId)
        {
            return await _dbContext.Tags.Where(t => t.OwnerId == ownerId).ToListAsync();
        }

        public async Task<TagEntity?> GetByIdAsync(int id, string ownerId)
        {
            return await _dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == ownerId);
        }

        public async Task<TagEntity?> GetByNameAsync(string name, string ownerId)
        {
            return await _dbContext.Tags.FirstOrDefaultAsync(t => t.Name == name && t.OwnerId == ownerId);
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
