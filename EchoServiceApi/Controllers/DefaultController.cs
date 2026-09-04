using Microsoft.AspNetCore.Mvc;

namespace EchoServiceApi.Controllers
{
    public class DefaultController : ControllerBase
    {
        const string HomeContentHtmlKey = "HomeContentHtml";
        const string HomeContentKey = "HomeContent";

        public IActionResult Home([FromServices] IConfiguration configuration)
        {
            var content = "It works!";
            var contentType = "text/plain; charset=utf-8";
            var contentHtml = configuration[HomeContentHtmlKey];
            if (!string.IsNullOrWhiteSpace(contentHtml))
            {
                content = contentHtml;
                contentType = "text/html; charset=UTF-8";
            }
            else if (configuration[HomeContentKey] is { } contentText && !string.IsNullOrWhiteSpace(contentText))
            {
                content = contentText;
            }
            return Content(content, contentType);
        }

        public IActionResult Name([FromServices] AppOptions appOptions)
            => Content(appOptions.DiagName ?? "", "text/plain; charset=utf-8");
    }
}
