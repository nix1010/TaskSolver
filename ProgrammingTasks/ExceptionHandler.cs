using Newtonsoft.Json;
using ProgrammingTasks.Models;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace ProgrammingTasks
{
    public class ExceptionHandler
    {
        public static void ThrowException(HttpStatusCode statusCode, string message)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new ErrorInfo()
                {
                    StatusCode = statusCode,
                    Message = message
                }))
            };

            responseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            throw new HttpResponseException(responseMessage);
        }
    }
}