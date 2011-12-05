using DelftTools.Utils.RegularExpressions;
using NUnit.Framework;

namespace DelftTools.Tests.Utils.RegularExpressions
{
    [TestFixture]
    public class RegularExpressionTest
    {
        [Test]
        public void GetFloat()
        {
            string source = "a 1.8 b 2.2 c 4.5";
            string pattern = RegularExpression.GetFloat("a")
                             + RegularExpression.GetFloat("b")
                             + RegularExpression.GetFloat("c");
            var match = RegularExpression.GetFirstMatch(pattern, source);
            Assert.AreEqual("1.8",match.Groups["a"].Value);
            Assert.AreEqual("2.2", match.Groups["b"].Value);
            Assert.AreEqual("4.5", match.Groups["c"].Value);
        }
        
        [Test]
        public void GetFloatOptionalIsReadWhenFound()
        {
            string pattern = RegularExpression.GetFloat("a")
                             + RegularExpression.GetFloatOptional("b")
                             + RegularExpression.GetFloat("c");
            
            string source = "a 1.8 b 2.2 c 4.5";
            
            var match = RegularExpression.GetFirstMatch(pattern, source);
            Assert.AreEqual("1.8", match.Groups["a"].Value);
            Assert.AreEqual("2.2", match.Groups["b"].Value);
            Assert.AreEqual("4.5", match.Groups["c"].Value);

        }
        [Test]
        public void GetFloatOptionalIsOptional()
        {
            string source = "a 1.8 c 4.5";
            string pattern = RegularExpression.GetFloat("a")
                             + RegularExpression.GetFloatOptional("b")
                             + RegularExpression.GetFloat("c");
            var match = RegularExpression.GetFirstMatch(pattern, source);
            Assert.AreEqual("1.8", match.Groups["a"].Value);
            Assert.AreEqual("4.5", match.Groups["c"].Value);
        }

        [Test]
        public void GetFloatPattern()
        {
            string pattern = RegularExpression.GetFloat("a");
            Assert.AreEqual("a\\s(?<a>[0-9\\.-]*)\\s?",pattern);
         
        }
    }
}