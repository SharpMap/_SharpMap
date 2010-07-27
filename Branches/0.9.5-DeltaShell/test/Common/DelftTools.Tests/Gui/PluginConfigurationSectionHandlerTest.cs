using System.Configuration;
using DelftTools.Shell.Gui;
using NUnit.Framework;

namespace DelftTools.Tests.Gui
{
    [TestFixture]
    public class PluginConfigurationSectionHandlerTest
    {
        [Test]
        public void ReadConfig()
        {
            var pluginConfig = (plugin) ConfigurationManager.GetSection("plugin");
            Assert.IsNotNull(pluginConfig);
        }
    }
}