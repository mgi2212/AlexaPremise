using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Alexa.Startup))]
namespace Alexa
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
