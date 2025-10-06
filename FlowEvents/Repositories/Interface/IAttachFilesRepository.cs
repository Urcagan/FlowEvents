using FlowEvents.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Interface
{
    public interface IAttachFilesRepository
    {
        Task<IEnumerable<AttachedFileModel>> GetByEventIdAsync(long eventId); // Получение списка вложенных файлов по EventId

        Task<AttachedFileModel> GetByIdAsync(int fileId); // Получение файла по его ID

        Task InsertEventAttachmentsAsync(long eventId, IEnumerable<AttachedFileModel> attachedFiles); // Сохранение информации о вложенных файлах в БД

        Task<bool> UpdateAsync(AttachedFileModel file); // Обновление информации о вложенном файле

        Task<bool> DeleteAsync(int fileId); // Существующий метод для удаления по FileId

        Task<bool> DeleteByEventIdAsync(long eventId); // Новый метод для удаления по EventId
    }
}
