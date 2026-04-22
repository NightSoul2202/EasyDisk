using EasyDisk.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces
{
    public interface ITrashService
    {
        Task<IEnumerable<TrashItemDto>> GetTrashItemsAsync();
        Task RestoreItemAsync(string id, bool isFolder);
        Task HardDeleteItemAsync(string id, bool isFolder);
        Task EmptyTrashAsync();
    }
}
