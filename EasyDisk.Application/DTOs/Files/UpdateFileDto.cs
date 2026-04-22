using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs.Files
{
    public class UpdateFileDto
    {
        public string Name { get; set; } = string.Empty;
        public int? FolderId { get; set; }
    }
}
