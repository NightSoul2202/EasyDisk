using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs.Auth
{
    public class GoogleLoginDto
    {
        [Required(ErrorMessage = "Token Google is required")]
        public string AccessToken { get; set; } = string.Empty;
        public string? Code { get; set; }
    }
}
