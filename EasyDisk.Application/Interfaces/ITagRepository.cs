using EasyDisk.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces
{
    public interface ITagRepository
    {
        Task<TagEntity?> GetByIdAsync(int id, string ownerId);
        Task<List<TagEntity>> GetAllByOwnerAsync(string ownerId);
        Task<TagEntity?> GetByNameAsync(string name, string ownerId);
        Task AddAsync(TagEntity tag);
        Task SaveChangesAsync();
        void Delete(TagEntity tag);
    }
}
