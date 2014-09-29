using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(MarkdownToHtml.Startup))]
namespace MarkdownToHtml
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
        }
    }
}
