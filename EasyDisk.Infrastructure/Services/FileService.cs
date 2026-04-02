using EasyDisk.Application.DTOs;
using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Interfaces;
using EasyDisk.Domain.Entities;
using EasyDisk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
        private readonly ApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileStorageService _fileStorageService;

        public FileService(ApplicationDbContext dbContext, ICurrentUserService currentUserService, IFileStorageService fileStorageService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _fileStorageService = fileStorageService;
        }

        public async Task<FileResponseDto?> UploadChunkAsync(UploadChunkDto uploadChunkDto, Stream chunkStream)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to upload file");

            if (uploadChunkDto.FolderId.HasValue)
            {
                // 1
                var folderExists = await _dbContext.Folders.AnyAsync(f => f.Id == uploadChunkDto.FolderId && f.OwnerId == userId);
                if (!folderExists)
                {
                    throw new NotFoundException("Folder", uploadChunkDto.FolderId);
                }
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

            _dbContext.Files.Add(file);
            await _dbContext.SaveChangesAsync();

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
