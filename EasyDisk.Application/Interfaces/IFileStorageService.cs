using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces
{
    public interface IFileStorageService
    {
        Task AppendChunkAsync(string uploadId, Stream chunkStream);
        Task<string> FinalizeUploadAsync(string uploadId, string extension);
        Task<Stream> GetFileStreamAsync(string physicalPath);
        Task CancelUploadAsync(string uploadId);
        Task DeleteFileAsync(string physicalPath);
    }
}
