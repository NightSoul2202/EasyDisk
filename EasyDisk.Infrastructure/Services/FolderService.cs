using EasyDisk.Application.DTOs;
using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Interfaces;
using EasyDisk.Domain.Entities;
using EasyDisk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace EasyDisk.Application.Services
{
    public class FolderService : IFolderService
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ApplicationDbContext _dbContext;

        public FolderService(ICurrentUserService currentUserService, ApplicationDbContext dbContext)
        {
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<FolderResponseDto> CreateFolderAsync(CreateFolderDto createFolderDto)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to create a folder.");

            await ValidateParentFolderAsync(createFolderDto, userId);
            await EnsureNameIsUniqueAsync(createFolderDto, userId);

            var folder = new FolderEntity
            {
                Name = createFolderDto.Name,
                ParentFolderId = createFolderDto.ParentFolderId,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Folders.Add(folder);
            await _dbContext.SaveChangesAsync();

            return new FolderResponseDto
            {
                Id = folder.Id,
                Name = folder.Name,
                ParentFolderId = folder.ParentFolderId,
                CreatedAt = folder.CreatedAt
            };
        }

        private async Task EnsureNameIsUniqueAsync(CreateFolderDto createFolderDto, string userId)
        {
            var dublicateExists = await _dbContext.Folders
                .FirstOrDefaultAsync(f => f.Name == createFolderDto.Name
                            && f.ParentFolderId == createFolderDto.ParentFolderId
                            && f.OwnerId == userId);
            if (dublicateExists != null)
            {
                throw new ValidationException($"A folder with name {createFolderDto.Name} already exists in the specified location.");
            }
        }

        private async Task ValidateParentFolderAsync(CreateFolderDto createFolderDto, string userId)
        {
            if (createFolderDto.ParentFolderId.HasValue)
            {
                var exists = await _dbContext.Folders
                    .AnyAsync(f => f.Id == createFolderDto.ParentFolderId && f.OwnerId == userId);

                if (!exists) throw new NotFoundException("Parent folder", createFolderDto.ParentFolderId);
            }
        }
    }
}
