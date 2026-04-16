using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Extensions
{
    public static class TaskExtensions
    {
        public static async Task<T> EnsureExistsAsync<T>(this Task<T?> task, Func<string> notFoundMessage) where T : class
        {
            var result = await task;
            if (result == null)
            {
                throw new NotFoundException(notFoundMessage());
            }
            return result;
        }

        public static async Task<T?> EnsureExistsNameAsync<T>(this Task<T?> task, Func<string> notFoundMessage) where T : class
        {
            var result = await task;
            if (result != null)
            {
                throw new ValidationException(notFoundMessage());
            }
            return result;
        }

        public static async Task<bool> ValidateCodeAsync(this Task<bool> task, Func<string> validateMessage)
        {
            var isTrue = await task;
            if (!isTrue)
            {
                throw new ValidationException(validateMessage());
            }
            return isTrue;
        }

        public static async Task ValidateExistsAsync(this Task<bool> task, Func<string> notFoundMessage)
        {
            var isTrue = await task;
            if (!isTrue)
            {
                throw new NotFoundException(notFoundMessage());
            }
        }

        public static async Task EnsureNameIsUniqueAsync(this Task<bool> task, Func<string> validateMessage)
        {
            var isTrue = await task;
            if (isTrue)
            {
                throw new ValidationException(validateMessage());
            }
        }


    }
}
