using EasyDisk.Application.DTOs;
using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Extensions;
using EasyDisk.Application.Interfaces;
using EasyDisk.Domain.Entities;
using EasyDisk.Infrastructure.Data;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using ValidationException = EasyDisk.Application.Exceptions.ValidationException;

namespace EasyDisk.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly IFileRepository _fileRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileStorageService _fileStorageService;

        public FileService(IFileRepository fileRepository, IFolderRepository folderRepository, ICurrentUserService currentUserService, IFileStorageService fileStorageService, IUserRepository userRepository)
        {
            _fileRepository = fileRepository;
            _folderRepository = folderRepository;
            _currentUserService = currentUserService;
            _fileStorageService = fileStorageService;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<FileResponseDto>> GetFilesAsync(int? folderId = null)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to view files.");

            var files = await _fileRepository.GetByFolderIdAsync(folderId, userId);

            return files.Select(f => new FileResponseDto
            {
                Id = f.Id,
                Name = f.Name,
                Size = f.Size,
                Extension = f.Extension,
                FolderId = f.FolderId,
                CreatedAt = f.CreatedAt
            });
        }

        public async Task<IEnumerable<FileResponseDto>> SearchFilesAsync(FileSearchParametersDto dto)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to search files.");

            var filesQuery = await _fileRepository.SearchFilesAsync(dto, userId);

            return filesQuery.Select(f => new FileResponseDto
            {
                Id = f.Id,
                Name = f.Name,
                Size = f.Size,
                Extension = f.Extension,
                FolderId = f.FolderId,
                CreatedAt = f.CreatedAt,
                Tags = f.Tags.Select(t => new TagResponseDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Color = t.Color,
                    CreatedAt = t.CreatedAt
                }).ToList()
            });
        }

        public async Task HardDeleteFileAsync(Guid fileId)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to delete file.");

            var file = await _fileRepository.GetByIdWithTagsAsync(fileId, userId).EnsureExistsAsync(() => $"File with id {fileId} not found.");

            await _userRepository.UpdateUserQuotaAsync(userId, -file.Size);

            await _fileStorageService.DeleteFileAsync(file.PhysicalPath);

            _fileRepository.Delete(file);

            await _fileRepository.SaveChangesAsync();
        }

        public async Task SoftDeleteFileAsync(Guid fileId)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to delete file.");

            var file = await _fileRepository.GetByIdWithTagsAsync(fileId, userId).EnsureExistsAsync(() => $"File with id {fileId} not found.");

            file.DeletedAt = DateTime.UtcNow;

            await _fileRepository.SaveChangesAsync();
        }

        public async Task<FileResponseDto?> UpdateFileAsync(Guid fileId, UpdateFileDto updateFileDto)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to update file.");

            var file = await _fileRepository.GetByIdWithTagsAsync(fileId, userId).EnsureExistsAsync(() => $"File with id {fileId} not found.");

            if (updateFileDto.FolderId != file.FolderId && updateFileDto.FolderId.HasValue)
            {
                await _folderRepository.ExistsAsync(updateFileDto.FolderId.Value, userId).ValidateExistsAsync(() => $"Folder with id {updateFileDto.FolderId.Value} not found.");
            }

            await _fileRepository.IsNameTakenAsync(updateFileDto.Name, file.Extension, updateFileDto.FolderId, userId, fileId).EnsureNameIsUniqueAsync(() => $"File with name {updateFileDto.Name} already exists in the folder.");

            file.Name = updateFileDto.Name;
            file.FolderId = updateFileDto.FolderId;

            await _fileRepository.SaveChangesAsync();

            return new FileResponseDto
            {
                Id = file.Id,
                Name = file.Name,
                Size = file.Size,
                Extension = file.Extension,
                FolderId = file.FolderId,
                CreatedAt = file.CreatedAt
            };
        }

        public async Task<FileResponseDto?> UploadChunkAsync(UploadChunkDto uploadChunkDto, Stream chunkStream)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to upload file");

            if (uploadChunkDto.FolderId.HasValue)
            {
                await _folderRepository.ExistsAsync(uploadChunkDto.FolderId.Value, userId).ValidateExistsAsync(() => $"Folder with id {uploadChunkDto.FolderId.Value} not found.");
            }

            await _fileStorageService.AppendChunkAsync(uploadChunkDto.UploadId, chunkStream);

            if (uploadChunkDto.ChunkIndex < uploadChunkDto.TotalChunks - 1)
            {
                return null;
            }

            return await FinalizeUploadProcessAsync(uploadChunkDto, userId);
        }

        public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadFileAsync(Guid fileId, string userId)
        {
            var file = await _fileRepository.GetByIdWithTagsAsync(fileId, userId).EnsureExistsAsync(() => $"File with id {fileId} not found.");

            var fileStream = await _fileStorageService.GetFileStreamAsync(file.PhysicalPath);

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(file.Name, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return (fileStream, contentType, file.Name);
        }

        public async Task MoveFileAsync(Guid fileId, int? targetFolderId)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User not found");

            var file = await _fileRepository.GetByIdAsync(fileId, userId)
                ?? throw new NotFoundException("File", fileId);

            if (targetFolderId.HasValue)
            {
                var targetFolder = await _folderRepository.GetByIdAsync(targetFolderId.Value, userId)
                    ?? throw new NotFoundException("Target folder", targetFolderId.Value);
            }

            file.FolderId = targetFolderId;
            await _fileRepository.UpdateAsync(file);
            await _fileRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<FileVersionResponseDto>> GetFileVersionsAsync(Guid fileId)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to view file versions.");

            var file = await _fileRepository.GetByIdWithVersionsAsync(fileId, userId).EnsureExistsAsync(() => $"File with id {fileId} not found.");

            return file.Versions
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => new FileVersionResponseDto
                {
                    Id = v.Id,
                    VersionNumber = v.VersionNumber,
                    Size = v.Size,
                    UploadedAt = v.UploadedAt,
                    IsCurrent = v.PhysicalPath == file.PhysicalPath
                });
        }

        public async Task RestoreFileVersionAsync(Guid fileId, int versionNumber)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User not found.");

            var file = await _fileRepository.GetByIdWithVersionsAsync(fileId, userId)
                ?? throw new NotFoundException("File", fileId);

            var targetVersion = file.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber)
                ?? throw new NotFoundException("Version", versionNumber);

            file.PhysicalPath = targetVersion.PhysicalPath;
            file.Size = targetVersion.Size;

            await _fileRepository.UpdateAsync(file);
            await _fileRepository.SaveChangesAsync();
        }

        public async Task CancelUploadAsync(string uploadId)
        {
            await _fileStorageService.CancelUploadAsync(uploadId);
        }

        private async Task<FileResponseDto> FinalizeUploadProcessAsync(UploadChunkDto uploadChunkDto, string userId)
        {
            var extension = Path.GetExtension(uploadChunkDto.FileName);

            var physicalPath = await _fileStorageService.FinalizeUploadAsync(uploadChunkDto.UploadId, uploadChunkDto.FileName);

            var fillPath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", physicalPath);
            var fileInfo = new FileInfo(fillPath);
            var size = fileInfo.Length;

            var existingFile = await _fileRepository.GetByNameWithVersionsAsync(uploadChunkDto.FileName, extension, uploadChunkDto.FolderId, userId);

            if (existingFile != null)
            {
                return await AppendNewVersionAsync(existingFile, physicalPath, size);
            }

            return await CreateBrandNewFileAsync(uploadChunkDto, extension, physicalPath, size, userId);
        }

        private async Task<FileResponseDto> CreateBrandNewFileAsync(UploadChunkDto uploadChunkDto, string extension, string physicalPath, long size, string userId)
        {
            var file = new FileEntity
            {
                Id = Guid.NewGuid(),
                Name = uploadChunkDto.FileName,
                Extension = extension,
                Size = size,
                PhysicalPath = physicalPath,
                OwnerId = userId,
                FolderId = uploadChunkDto.FolderId,
                CreatedAt = DateTime.UtcNow
            };

            var version = new FileVersionEntity
            {
                Id = Guid.NewGuid(),
                VersionNumber = 1,
                Size = size,
                PhysicalPath = physicalPath
            };

            file.Versions.Add(version);

            await _fileRepository.AddAsync(file);

            await _userRepository.UpdateUserQuotaAsync(userId, size);

            await _fileRepository.SaveChangesAsync();

            return MapToFileResponseDto(file);
        }

        private async Task<FileResponseDto> AppendNewVersionAsync(FileEntity existingFile, string newPhysicalPath, long newSize)
        {
            int newVersionNumber = existingFile.Versions.Any() ? existingFile.Versions.Max(v => v.VersionNumber) + 1 : 2;

            var newVersion = new FileVersionEntity
            {
                Id = Guid.NewGuid(),
                FileId = existingFile.Id,
                VersionNumber = newVersionNumber,
                Size = newSize,
                PhysicalPath = newPhysicalPath
            };

            await _fileRepository.AddFileVersionAsync(newVersion);

            existingFile.Size = newSize;
            existingFile.PhysicalPath = newPhysicalPath;

            await _userRepository.UpdateUserQuotaAsync(existingFile.OwnerId, newSize);

            await _fileRepository.SaveChangesAsync();

            return MapToFileResponseDto(existingFile);
        }

        private FileResponseDto MapToFileResponseDto(FileEntity file)
        {
            return new FileResponseDto
            {
                Id = file.Id,
                Name = file.Name,
                Size = file.Size,
                Extension = file.Extension,
                FolderId = file.FolderId,
                CreatedAt = file.CreatedAt
            };
        }
    }
}
