using System.Reflection;
using DelftTools.Utils.Reflection;
using NUnit.Framework;

namespace DelftTools.Tests.Utils.Reflection
{
    [TestFixture]
    public class AssemblyUtilsTest
    {
        [Test]
        public void GetAssemblyInfo()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyUtils.AssemblyInfo assemblyInfo = AssemblyUtils.GetAssemblyInfo(assembly);
            Assert.AreEqual("DelftTools.Tests", assemblyInfo.Title);
        }
    }
}