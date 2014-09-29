using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace MarkdownToHtml.Controllers
{
    [RoutePrefix("")]
    public class HomeController : Controller
    {
        [Route("")]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Route("")]
        public async Task<ActionResult> Index(string type, string url, string owner, string repo, string path, string token)
        {
            if (String.IsNullOrWhiteSpace(type))
                return Redirect("/");

            var result = type.Equals("public", StringComparison.InvariantCultureIgnoreCase) ? await getPublicAsync(url) : await getPrivateAsync(owner, repo, path, token);

            if (String.IsNullOrWhiteSpace(result))
                return View();

            var css = await getPublicAsync("https://raw.githubusercontent.com/sindresorhus/github-markdown-css/gh-pages/github-markdown.css");
            var layout = getLayout();
            var body = await getHtmlFromGitHubMarkdownApi(result);

            var content = layout
                .Replace("{{github-css}}", css)
                .Replace("{{body}}", body);

            return Content(content, "text/html");
        }

        async Task<string> getHtmlFromGitHubMarkdownApi(string markdown)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitHubMarkdownToHtml", "1.0"));

            var request = new
            {
                text = markdown,
                mode = "gfm",
                context = ""
            };

            var response = await client.PostAsync("https://api.github.com/markdown", new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();

            return content;
        }

        async Task<string> getPublicAsync(string url)
        {
            var client = new HttpClient();

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync();
        }

        async Task<string> getPrivateAsync(string owner, string repo, string path, string token)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ConvertToMarkdown", "1.0"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", token, "x-oauth-basic"))));

            var response = await client.GetAsync(String.Format("https://api.github.com/repos/{0}/{1}/contents/{2}", owner, repo, path));

            if (!response.IsSuccessStatusCode)
                return null;

            var json = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

            if (json == null || String.IsNullOrWhiteSpace((string)json.content))
                return null;

            var content = Encoding.UTF8.GetString(Convert.FromBase64String((string)json.content));

            return content;
        }

        string getLayout()
        {
            return
@"<html>
<head>
    <title></title>
    <link rel=""stylesheet"" href=""~/content/github-markdown.css"">
    <style>
        {{github-css}}
        .markdown-body {
            min-width: 200px;
            max-width: 790px;
            margin: 0 auto;
            padding: 30px;
        }
    </style>
</head>
<body>
    <article class=""markdown-body"">
        {{body}}
    </article>
</body>
</html>";
        }
    }
}