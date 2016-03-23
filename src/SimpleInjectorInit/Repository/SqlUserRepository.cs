using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleInjectorInit.Interfaces;
using SimpleInjectorInit.Models;

namespace SimpleInjectorInit.Repository
{
    public class SqlUserRepository: IUserRepository
    {
        ApplicationDbContext _db = new ApplicationDbContext();

        public List<ApplicationUser> Get()
        {
            return _db.Users.ToList();
        }

        public ApplicationUser Get(string id)
        {
            return _db.Users.Single(d => d.Id == id);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
