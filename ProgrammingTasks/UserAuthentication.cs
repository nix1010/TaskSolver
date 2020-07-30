using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Filters;
using System.Web.Http.Controllers;
using System.Net;
using System.Net.Http;
using System.Text;
using ProgrammingTasks.Models.Entity;
using System.Threading;
using System.Security.Principal;
using System.Security.Cryptography;

namespace ProgrammingTasks
{
    public class UserAuthentication : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (actionContext.Request.Headers.Authorization == null)
            {
                //if response is set, server immediately returns it
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized, "Credentials not provided");
            }
            else
            {
                string authenticationToken = actionContext.Request.Headers.Authorization.Parameter;
                string decodedAuthToken = 
                       Encoding.UTF8.GetString(
                        Convert.FromBase64CharArray(authenticationToken.ToCharArray(), 0, authenticationToken.Length)
                        );
                 
                string [] credentials = decodedAuthToken.Split(':');
                string username = credentials[0];
                byte[] password = Sha256(credentials[1]);
                
                using (DBEntities entities = new DBEntities())
                {
                    user userResult = null;

                    try
                    {
                        userResult = entities.users.Single(user => user.username == username
                                                               && user.password.Equals(password));

                        Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(username), null);
                    }
                    catch (Exception)
                    {
                        actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized, 
                                                                                                            "Authorization failed");
                    }
                }
            }
        }

        private byte[] Sha256(string text)
        {
            SHA256 sha256 = SHA256.Create();

            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));

            return hash;
        }
    }
}