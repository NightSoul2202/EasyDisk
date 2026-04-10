using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs
{
    public class ShareLinkResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public DateTime? ExpirationDate { get; set; }
        public bool IsPasswordProtected { get; set; }
    }
}
