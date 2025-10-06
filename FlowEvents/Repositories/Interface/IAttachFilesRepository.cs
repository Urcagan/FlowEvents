using FlowEvents.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Interface
{
    public interface IAttachFilesRepository
    {
        Task InsertEventAttachmentsAsync(long eventId, IEnumerable<AttachedFileModel> attachedFiles);

        Task<bool> DeleteAsync(int fileId);
    }
}
