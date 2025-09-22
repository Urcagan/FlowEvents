using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Services.Interface
{
    public interface IDatabaseValidationService
    {
        Task<DatabaseValidationResult> ValidateDatabaseAsync(string databasePath, string expectedVersion);
        Task<DatabaseValidationResult> ValidateDatabaseAsync(DatabaseInfo databaseInfo);
        bool ValidateFile(string databasePath);
    }

    public class DatabaseValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public ValidationErrorType ErrorType { get; set; }
        public DatabaseInfo DatabaseInfo { get; set; }
    }
}
