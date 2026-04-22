using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces.EmailSender
{
    public interface IEmailSenderService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
}
