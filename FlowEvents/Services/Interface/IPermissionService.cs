using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Services.Interface
{
    public interface IPermissionService
    {
        Task<List<Permission>> GetPermissionsAsync();
    }
}
