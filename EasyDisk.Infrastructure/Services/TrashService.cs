using EasyDisk.Application.DTOs.Files;
using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Interfaces.Auth;
using EasyDisk.Application.Interfaces.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Services
{
    public class TrashService : ITrashService
    {
        private readonly IFileRepository _fileRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly IFileService _fileService;
        private readonly IFolderService _folderService;
        private readonly ICurrentUserService _currentUserService;

        public TrashService(
            IFileRepository fileRepository,
            IFolderRepository folderRepository,
            IFileService fileService,
            IFolderService folderService,
            ICurrentUserService currentUserService)
        {
            _fileRepository = fileRepository;
            _folderRepository = folderRepository;
            _fileService = fileService;
            _folderService = folderService;
            _currentUserService = currentUserService;
        }

        public async Task<IEnumerable<TrashItemDto>> GetTrashItemsAsync()
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User not found");

            var deletedFiles = await _fileRepository.GetDeletedFilesAsync(userId);
            var deletedFolders = await _folderRepository.GetDeletedFoldersAsync(userId);

            var trashItems = new List<TrashItemDto>();

            trashItems.AddRange(deletedFolders.Select(f => new TrashItemDto
            {
                Id = f.Id.ToString(),
                Name = f.Name,
                IsFolder = true,
                Size = 0,
                DeletedAt = f.DeletedAt
            }));

            trashItems.AddRange(deletedFiles.Select(f => new TrashItemDto
            {
                Id = f.Id.ToString(),
                Name = f.Name,
                IsFolder = false,
                Size = f.Size,
                DeletedAt = f.DeletedAt
            }));

            return trashItems.OrderByDescending(t => t.DeletedAt);
        }

        public async Task RestoreItemAsync(string id, bool isFolder)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User not found");

            if (isFolder)
            {
                if (!int.TryParse(id, out int folderId)) throw new ValidationException("Invalid folder ID");

                await _folderService.RestoreFolderAsync(folderId);
            }
            else
            {
                if (!Guid.TryParse(id, out Guid fileId)) throw new ValidationException("Invalid file ID");

                var file = await _fileRepository.GetDeletedFileByIdAsync(fileId, userId)
                    ?? throw new NotFoundException("File in trash", fileId);

                file.DeletedAt = null;
                await _fileRepository.UpdateAsync(file);
                await _fileRepository.SaveChangesAsync();
            }
        }

        public async Task EmptyTrashAsync()
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User not found");

            var deletedFiles = await _fileRepository.GetDeletedFilesAsync(userId);
            var deletedFolders = await _folderRepository.GetDeletedFoldersAsync(userId);

            foreach (var file in deletedFiles)
            {
                await _fileService.HardDeleteFileAsync(file.Id);
            }

            foreach (var folder in deletedFolders)
            {
                await _folderService.HardDeleteFolderAsync(folder.Id);
            }
        }

        public async Task HardDeleteItemAsync(string id, bool isFolder)
        {
            if (isFolder)
            {
                if (!int.TryParse(id, out int folderId))
                {
                    throw new ValidationException("Invalid folder ID");
                }

                await _folderService.HardDeleteFolderAsync(folderId);
            }
            else
            {
                if (!Guid.TryParse(id, out Guid fileId))
                {
                    throw new ValidationException("Invalid file ID");
                }

                await _fileService.HardDeleteFileAsync(fileId);
            }
        }
    }
}
