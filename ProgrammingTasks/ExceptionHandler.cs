using System.Net;
using System.Net.Http;
using System.Web.Http;
using ProgrammingTasks.Models;
using System.Net.Http.Headers;
using Newtonsoft.Json;

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