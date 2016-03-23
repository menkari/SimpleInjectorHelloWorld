using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleInjectorInit.Models;

namespace SimpleInjectorInit.Interfaces
{
    interface IUserRepository
    {
        List<ApplicationUser> Get();

        ApplicationUser Get(string id);

        void Dispose();
    }
}
