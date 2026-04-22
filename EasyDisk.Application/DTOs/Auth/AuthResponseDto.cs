using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string? Token { get; set; }
        public string? Email { get; set; }
        public string? UserId { get; set; }
        public bool RequiresTwoFactor { get; set; }
    }
}
