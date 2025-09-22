using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Models
{
    public class DatabaseInfo
    {
        public string Path { get; set; }
        public string Version { get; set; }
        public bool Exists { get; set; }
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }
}
