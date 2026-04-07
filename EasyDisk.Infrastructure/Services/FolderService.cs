using EasyDisk.Application.DTOs;
using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Interfaces;
using EasyDisk.Domain.Entities;
using EasyDisk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
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

            await ValidateParentFolderAsync(createFolderDto.ParentFolderId, userId);
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

        public async Task<IEnumerable<FolderResponseDto>> GetFoldersAsync(int? parentFolderId = null)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to view folders.");

            return await _dbContext.Folders
                .Where(f => f.OwnerId == userId && f.ParentFolderId == parentFolderId)
                .Select(f => new FolderResponseDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    ParentFolderId = f.ParentFolderId,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();
        }

        public async Task SoftDeleteFolderAsync(int folderId)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to delete a folder.");

            var rootFolder = await _dbContext.Folders.FirstOrDefaultAsync(f => f.Id == folderId && f.OwnerId == userId && f.DeletedAt == null);
            if(rootFolder == null)
            {
                throw new NotFoundException("Folder", folderId);
            }

            await MarkFolderAsDeletedRecursiveAsync(folderId, userId);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<FolderResponseDto> UpdateFolderAsync(int folderId, UpdateFolderDto updateFolderDto)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to update a folder.");

            var folder = await _dbContext.Folders.FirstOrDefaultAsync(f => f.Id == folderId && f.OwnerId == userId);
            if (folder == null)
            {
                throw new NotFoundException("Folder", folderId);
            }

            if (updateFolderDto.ParentFolderId != folder.ParentFolderId)
            {
                if(updateFolderDto.ParentFolderId == folder.Id)
                {
                    throw new ValidationException("A folder cannot be its own parent.");
                }

                await ValidateParentFolderAsync(updateFolderDto.ParentFolderId, userId);
            }

            await EnsureNameIsNotDublicateExists(updateFolderDto, folderId, userId);

            folder.Name = updateFolderDto.Name;
            folder.ParentFolderId = updateFolderDto.ParentFolderId;

            await _dbContext.SaveChangesAsync();

            return new FolderResponseDto
            {
                Id = folder.Id,
                Name = folder.Name,
                ParentFolderId = folder.ParentFolderId,
                CreatedAt = folder.CreatedAt
            };
        }

        private async Task MarkFolderAsDeletedRecursiveAsync(int folderId, string userId)
        {
            var folder = await _dbContext.Folders
                .Include(f => f.Files)
                .FirstOrDefaultAsync(f => f.Id == folderId && f.OwnerId == userId);

            if (folder == null || folder.DeletedAt != null)
            {
                return;
            }

            folder.DeletedAt = DateTime.UtcNow;

            foreach (var file in folder.Files)
            {
                file.DeletedAt = DateTime.UtcNow;
            }

            var subFolders = await _dbContext.Folders.Where(f => f.ParentFolderId == folderId && f.OwnerId == userId && f.DeletedAt == null).ToListAsync();
            foreach (var subFolder in subFolders)
            {
                await MarkFolderAsDeletedRecursiveAsync(subFolder.Id, userId);
            }
        }

        private async Task EnsureNameIsNotDublicateExists(UpdateFolderDto updateFolderDto, int folderId, string userId)
        {
            var dublicateExists = await _dbContext.Folders
                .AnyAsync(f => f.Id != folderId
                            && f.Name == updateFolderDto.Name
                            && f.ParentFolderId == updateFolderDto.ParentFolderId
                            && f.OwnerId == userId);

            if (dublicateExists)
            {
                throw new ValidationException($"A folder with name {updateFolderDto.Name} already exists in the specified location.");
            }
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

        private async Task ValidateParentFolderAsync(int? parentFolderId, string userId)
        {
            if (parentFolderId.HasValue)
            {
                var exists = await _dbContext.Folders
                    .AnyAsync(f => f.Id == parentFolderId && f.OwnerId == userId);

                if (!exists)
                {
                    throw new NotFoundException("Parent folder", parentFolderId);
                }
            }
        }
    }
}
