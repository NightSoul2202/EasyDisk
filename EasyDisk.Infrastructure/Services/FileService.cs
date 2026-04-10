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
using System.Text;
using System.Threading.Tasks;
using ValidationException = EasyDisk.Application.Exceptions.ValidationException;

namespace EasyDisk.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly IFileRepository _fileRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileStorageService _fileStorageService;

        public FileService(IFileRepository fileRepository, IFolderRepository folderRepository, ICurrentUserService currentUserService, IFileStorageService fileStorageService)
        {
            _fileRepository = fileRepository;
            _folderRepository = folderRepository;
            _currentUserService = currentUserService;
            _fileStorageService = fileStorageService;
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

        public async Task HardDeleteFileAsync(Guid fileId)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to delete file.");

            var file = await _fileRepository.GetByIdWithTagsAsync(fileId, userId).EnsureExistsAsync(() => $"File with id {fileId} not found.");

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

            var extension = Path.GetExtension(uploadChunkDto.FileName);

            var physicalPath = await _fileStorageService.FinalizeUploadAsync(uploadChunkDto.UploadId, uploadChunkDto.FileName);

            var fillPath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", physicalPath);
            var fileInfo = new FileInfo(fillPath);
            var size = fileInfo.Length;

            var file = new FileEntity
            {
                Id = Guid.NewGuid(),
                Name = uploadChunkDto.FileName,
                Size = size,
                Extension = extension,
                FolderId = uploadChunkDto.FolderId,
                OwnerId = userId,
                PhysicalPath = physicalPath,
                CreatedAt = DateTime.UtcNow
            };

            await _fileRepository.AddAsync(file);
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

        public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadFileAsync(Guid fileId)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to download file.");

            var file = await _fileRepository.GetByIdWithTagsAsync(fileId, userId).EnsureExistsAsync(() => $"File with id {fileId} not found.");

            var fileStream = await _fileStorageService.GetFileStreamAsync(file.PhysicalPath);

            var provider = new FileExtensionContentTypeProvider();
            if(!provider.TryGetContentType(file.Name, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return (fileStream, contentType, file.Name);
        }

        public async Task CancelUploadAsync(string uploadId)
        {
            await _fileStorageService.CancelUploadAsync(uploadId);
        }
    }
}
