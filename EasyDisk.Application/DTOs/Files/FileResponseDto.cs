using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs.Files
{
    public class FileResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Extension { get; set; } = string.Empty;
        public int? FolderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TagResponseDto> Tags { get; set; } = new();
    }
}
