//using Microsoft.AspNet.Identity;
//using Microsoft.AspNet.Identity.EntityFramework;
using ProgrammingTasks.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ProgrammingTasks.Identity
{
    public class AuthRepository : IDisposable
    {
        /*
        private programming_tasksEntities _ctx;
        
        private UserManager<IdentityUser> _userManager;

        public AuthRepository()
        {
            _ctx = new programming_tasksEntities();
            _userManager = new UserManager<IdentityUser>(new UserStore<IdentityUser>(_ctx));
        }

        public async Task<IdentityResult> RegisterUser(user user)
        {
            IdentityUser identityUser = new IdentityUser
            {
                UserName = user.username
            };

            var result = await _userManager.CreateAsync(identityUser, "pass");

            return result;
        }

        public async Task<IdentityUser> FindUser(string userName, string password)
        {
            IdentityUser user = await _userManager.FindAsync(userName, password);

            return user;
        }
        */
        public void Dispose()
        {
         /*   _ctx.Dispose();
            _userManager.Dispose();
          * */
        }
         
    }
}