using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs
{
    public class ShareLinkInfoDto
    {
        public string FileName { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
        public long? Size { get; set; }
        public bool IsPasswordProtected { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }
}
