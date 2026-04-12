using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs
{
    public class FileSearchParametersDto
    {
        public string? SearchTerm { get; set; }
        public string? Extension { get; set; }
        public int? FolderId { get; set; }
        public List<int>? TagIds { get; set; }
    }
}
