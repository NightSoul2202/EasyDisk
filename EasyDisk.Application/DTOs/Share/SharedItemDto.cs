using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs
{
    public class SharedItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string OriginalId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
