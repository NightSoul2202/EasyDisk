using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Domain.Entities
{
    public class FileVersionEntity
    {
        public Guid Id { get; set; }
        public int VersionNumber { get; set; }
        public long Size { get; set; }
        public string PhysicalPath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public Guid FileId { get; set; }

        public FileEntity? File { get; set; }
    }
}
