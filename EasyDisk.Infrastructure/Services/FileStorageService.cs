using EasyDisk.Application.Interfaces;
using EasyDisk.Infrastructure.Exceptions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _uploadDirectory;
        private readonly string _tempDirectory;

        public FileStorageService(IConfiguration configuration)
        {
            var basePath = configuration["Storage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage");
            
            _uploadDirectory = Path.Combine(basePath, "Uploads");
            _tempDirectory = Path.Combine(basePath, "Temp");

            if(!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }
            if(!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }

        public async Task AppendChunkAsync(string uploadId, Stream chunkStream)
        {
            var tempFilePath = Path.Combine(_tempDirectory, $"{uploadId}.tmp");

            using var fileStream = new FileStream(tempFilePath, FileMode.Append, FileAccess.Write, FileShare.None);
            await chunkStream.CopyToAsync(fileStream);
        }

        public async Task<string> FinalizeUploadAsync(string uploadId, string extension)
        {
            var tempFilePath = Path.Combine(_tempDirectory, $"{uploadId}.tmp");

            if (!File.Exists(tempFilePath))
            {
                throw new StorageFileNotFoundException($"Temp file not found {uploadId}");
            }

            var finalFileName = $"{Guid.NewGuid()}{extension}";
            var finalFilePath = Path.Combine(_uploadDirectory, finalFileName);

            await Task.Run(() => File.Move(tempFilePath, finalFilePath));

            return Path.Combine("Uploads", finalFileName);
        }

        public Task<Stream> GetFileStreamAsync(string physicalPath)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", physicalPath);

            if(!File.Exists(fullPath))
            {
                throw new StorageFileNotFoundException($"File not found at path: {physicalPath}");
            }

            Stream fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            return Task.FromResult(fileStream);
        }

        public Task CancelUploadAsync(string uploadId)
        {
            var tempFilePath = Path.Combine(_tempDirectory, $"{uploadId}.tmp");

            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            return Task.CompletedTask;
        }

        public Task DeleteFileAsync(string physicalPath)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", physicalPath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }
    }
}
