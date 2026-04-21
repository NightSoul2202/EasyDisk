using EasyDisk.Application.DTOs;
using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Extensions;
using EasyDisk.Application.Interfaces;
using EasyDisk.Domain.Entities;
using EasyDisk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO.Compression;
using System.Threading.Tasks;

namespace EasyDisk.Application.Services
{
    public class FolderService : IFolderService
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IFolderRepository _folderRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFileStorageService _fileStorageService;

        public FolderService(ICurrentUserService currentUserService, IFolderRepository folderRepository, IFileRepository fileRepository, IFileStorageService fileStorageService, IUserRepository userRepository)
        {
            _currentUserService = currentUserService;
            _folderRepository = folderRepository;
            _fileRepository = fileRepository;
            _fileStorageService = fileStorageService;
            _userRepository = userRepository;
        }

        public async Task<FolderResponseDto> CreateFolderAsync(CreateFolderDto createFolderDto)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to create a folder.");

            if (createFolderDto.ParentFolderId.HasValue)
            {
                await _folderRepository.ExistsAsync(createFolderDto.ParentFolderId.Value, userId).ValidateExistsAsync(() => $"Parent folder with id {createFolderDto.ParentFolderId.Value} not found.");
            }
                
            await _folderRepository.IsNameTakenAsync(createFolderDto.Name, createFolderDto.ParentFolderId, userId, null).EnsureNameIsUniqueAsync(() => $"Folder with name {createFolderDto.Name} already exists in the parent folder.");

            var folder = new FolderEntity
            {
                Name = createFolderDto.Name,
                ParentFolderId = createFolderDto.ParentFolderId,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _folderRepository.AddAsync(folder);
            await _folderRepository.SaveChangesAsync();

            return new FolderResponseDto
            {
                Id = folder.Id,
                Name = folder.Name,
                ParentFolderId = folder.ParentFolderId,
                CreatedAt = folder.CreatedAt
            };
        }

        public async Task<(Stream ZipStream, string ZipName)> DownloadFolderAsync(int folderId, string userId)
        {
            var rootFolder = await _folderRepository.GetByIdAsync(folderId, userId).EnsureExistsAsync(() => $"Folder with id {folderId} not found.");

            var memoryStream = new MemoryStream();

            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                await AddFolderToArchive(archive, folderId, "", userId);
            }

            memoryStream.Position = 0;
            return (memoryStream, $"{rootFolder.Name}.zip");
        }

        public async Task<IEnumerable<FolderResponseDto>> GetFoldersAsync(int? parentFolderId = null)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to view folders.");

            var folders = await _folderRepository.GetByParentIdAsync(parentFolderId, userId);

            return folders.Select(f => new FolderResponseDto
            {
                Id = f.Id,
                Name = f.Name,
                ParentFolderId = f.ParentFolderId,
                CreatedAt = f.CreatedAt
            });
        }

        public async Task<IEnumerable<FolderResponseDto>> GetFolderPathAsync(int folderId)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated.");

            var path = new List<FolderResponseDto>();
            int? currentId = folderId;

            while (currentId.HasValue)
            {
                var folder = await _folderRepository.GetByIdAsync(currentId.Value, userId);
                if (folder == null)
                {
                    break;
                }

                path.Insert(0, new FolderResponseDto 
                { 
                    Id = folder.Id, 
                    Name = folder.Name, 
                    ParentFolderId = folder.ParentFolderId 
                });

                currentId = folder.ParentFolderId;
            }

            return path;
        }

        public async Task<FolderResponseDto> UpdateFolderAsync(int folderId, UpdateFolderDto updateFolderDto)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to update a folder.");

            var folder = await _folderRepository.GetByIdWithFilesAsync(folderId, userId).EnsureExistsAsync(() => $"Folder with id {folderId} not found.");

            if (updateFolderDto.ParentFolderId != folder.ParentFolderId && updateFolderDto.ParentFolderId.HasValue)
            {
                if (updateFolderDto.ParentFolderId == folder.Id)
                {
                    throw new ValidationException("A folder cannot be its own parent.");
                }

                await _folderRepository.ExistsAsync(updateFolderDto.ParentFolderId.Value, userId).ValidateExistsAsync(() => $"Parent folder with id {updateFolderDto.ParentFolderId.Value} not found.");
            }

            await _folderRepository.IsNameTakenAsync(updateFolderDto.Name, updateFolderDto.ParentFolderId, userId, folderId).EnsureNameIsUniqueAsync(() => $"Folder with name {updateFolderDto.Name} already exists in the parent folder.");

            folder.Name = updateFolderDto.Name;
            folder.ParentFolderId = updateFolderDto.ParentFolderId;

            await _folderRepository.SaveChangesAsync();

            return new FolderResponseDto
            {
                Id = folder.Id,
                Name = folder.Name,
                ParentFolderId = folder.ParentFolderId,
                CreatedAt = folder.CreatedAt
            };
        }

        public async Task SoftDeleteFolderAsync(int folderId)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to delete a folder.");

            var rootFolder = await _folderRepository.GetByIdWithFilesAsync(folderId, userId).EnsureExistsAsync(() => $"Folder with id {folderId} not found.");

            await MarkFolderAsDeletedRecursiveAsync(folderId, userId);

            await _folderRepository.SaveChangesAsync();
        }

        public async Task HardDeleteFolderAsync(int folderId)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to delete a folder.");

            var rootFolder = await _folderRepository.GetByIdWithFilesAsync(folderId, userId).EnsureExistsAsync(() => $"Folder with id {folderId} not found.");

            var totalDeletedSize = await ExecuteHardDeleteRecursiveAsync(rootFolder, userId);

            await _userRepository.UpdateUserQuotaAsync(userId, -totalDeletedSize);

            await _folderRepository.SaveChangesAsync();
        }

        private async Task<long> ExecuteHardDeleteRecursiveAsync(FolderEntity folder, string userId)
        {
            long totalDeletedSize = 0;
            foreach (var file in folder.Files)
            {
                totalDeletedSize += file.Size;
                await _fileStorageService.DeleteFileAsync(file.PhysicalPath);
                _fileRepository.Delete(file);
            }

            var subFolders = await _folderRepository.GetByParentIdAsync(folder.Id, userId);

            foreach (var subFolder in subFolders)
            {
                var detailedSubFolder = await _folderRepository.GetByIdWithFilesAsync(subFolder.Id, userId);
                if (detailedSubFolder != null)
                {
                    totalDeletedSize += await ExecuteHardDeleteRecursiveAsync(detailedSubFolder, userId);
                }
            }

            _folderRepository.Delete(folder);
            return totalDeletedSize;
        }

        private async Task MarkFolderAsDeletedRecursiveAsync(int folderId, string userId)
        {
            var folder = await _folderRepository.GetByIdWithFilesAsync(folderId, userId);

            if (folder == null || folder.DeletedAt != null)
            {
                return;
            }

            folder.DeletedAt = DateTime.UtcNow;

            foreach (var file in folder.Files)
            {
                file.DeletedAt = DateTime.UtcNow;
            }

            var subFolders = await _folderRepository.GetByParentIdAsync(folderId, userId);
            foreach (var subFolder in subFolders)
            {
                await MarkFolderAsDeletedRecursiveAsync(subFolder.Id, userId);
            }
        }

        private async Task AddFolderToArchive(ZipArchive archive, int folderId, string currentPath, string userId)
        {
            var folder = await _folderRepository.GetByIdWithFilesAsync(folderId, userId).EnsureExistsAsync(() => $"Folder with id {folderId} not found."); ;

            foreach (var file in folder.Files)
            {
                var entryName = Path.Combine(currentPath, file.Name);
                var entry = archive.CreateEntry(entryName);

                using (var entryStream = entry.Open())
                using (var fileStream = await _fileStorageService.GetFileStreamAsync(file.PhysicalPath))
                {
                    await fileStream.CopyToAsync(entryStream);
                }
            }

            var subFolders = await _folderRepository.GetByParentIdAsync(folderId, userId);

            foreach (var subFolder in subFolders)
            {
                var newPath = Path.Combine(currentPath, subFolder.Name);
                await AddFolderToArchive(archive, subFolder.Id, newPath, userId);
            }
        }
    }
}
