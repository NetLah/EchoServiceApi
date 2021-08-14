using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace EchoServiceApi.Controllers
{
    [Route("e")]
    [ApiController]
    public class EchoController : ControllerBase
    {
        [Route("400")]
        public IActionResult OnError400() => CodeAndMessage(400, "Test bad request");

        [Route("401")]
        public IActionResult OnError401() => CodeAndMessage(401, "Test unauthorized");

        [Route("403")]
        public IActionResult OnError403() => CodeAndMessage(403, "Test forbidden");

        [Route("404")]
        public IActionResult OnError404() => CodeAndMessage(404, "Test not found");

        [Route("500")]
        public IActionResult OnError500() => CodeAndMessage(500, "Test internal server error");

        [Route("502")]
        public IActionResult OnError502() => CodeAndMessage(502, "Test bad gateway");

        [Route("503")]
        public IActionResult OnError503() => CodeAndMessage(503, "Test service unavailable");

        private IActionResult CodeAndMessage(int code, string message) => new ContentResult
        {
            Content = message,
            ContentType = "text/plain",
            StatusCode = code,
        };

        [Route("{*url}")]
        public async Task<ActionResult<MyResult>> Action(string url)
        {
            var request = HttpContext.Request;
            var result = new MyResult
            {
                Url = url,
                Method = request.Method,
                Scheme = request.Scheme,
                Host = request.Host.ToString(),
                Connection = new ConnInfo(HttpContext.Connection),
                PathBase = request.PathBase.ToString(),
                Path = request.Path.ToString(),
                ContentType = request.ContentType,
                QueryString = request.QueryString.ToString(),
                Headers = ToDict1(request.Headers),
                Form = ToDict1(request.HasFormContentType ? request.Form : null),
                Query = ToDict1(request.Query),
            };

            if (!string.IsNullOrEmpty(request.ContentType))
            {
                using (var reader = new System.IO.StreamReader(Request.Body, System.Text.Encoding.UTF8))
                {
                    result.Body = await reader.ReadToEndAsync();
                }
            }

            return result;
        }

        private IDictionary<string, string> ToDict1(IEnumerable<KeyValuePair<string, StringValues>> headers)
        {
            return headers?.ToDictionary(e => e.Key, e => e.Value.ToString());
        }

        public class MyResult
        {
            public string Url { get; set; }
            public string Body { get; set; }
            public string Method { get; set; }
            public string Scheme { get; set; }
            public string Host { get; set; }
            public ConnInfo Connection { get; set; }
            public string PathBase { get; set; }
            public string Path { get; set; }
            public string ContentType { get; set; }
            public string QueryString { get; set; }
            public IDictionary<string, string> Headers { get; set; }
            public IDictionary<string, string> Form { get; set; }
            public IDictionary<string, string> Query { get; set; }
        }

        public class ConnInfo
        {
            public ConnInfo(ConnectionInfo connectionInfo)
            {
                if (connectionInfo != null)
                {
                    Id = connectionInfo.Id;
                    LocalIpAddress = connectionInfo.LocalIpAddress?.ToString();
                    LocalPort = connectionInfo.LocalPort;
                    RemoteIpAddress = connectionInfo.RemoteIpAddress?.ToString();
                    RemotePort = connectionInfo.RemotePort;
                }
            }

            public string Id { get; }
            public string LocalIpAddress { get; }
            public int LocalPort { get; }
            public string RemoteIpAddress { get; }
            public int RemotePort { get; }
        }
    }
}
