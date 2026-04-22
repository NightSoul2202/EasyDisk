using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs.Files
{
    public class FileVersionResponseDto
    {
        public Guid Id { get; set; }
        public int VersionNumber { get; set; }
        public long Size { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsCurrent { get; set; }
    }
}
