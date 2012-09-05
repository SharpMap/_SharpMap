using System;
using System.Globalization;
using DelftTools.Utils;
using NUnit.Framework;

namespace DelftTools.Tests.Utils
{
    [TestFixture]
    public class StringExtensionsTests
    {
        [Test]
        public void Parse()
        {
            
            "12.34".Parse<double>(CultureInfo.InvariantCulture).Should().Be.EqualTo(12.34);

            "1234".Parse<int>().Should().Be.EqualTo(1234);
            
            "asdf".Parse<int>().Should().Be.EqualTo(0); // i.e. default(int)
            
            "1234".Parse<int?>().Should().Be.EqualTo(1234);
            
            "asdf".Parse<int?>().Should().Be.EqualTo(null);
            
            "2001-02-03".Parse<DateTime?>().Should().Be.EqualTo(new DateTime(2001, 2, 3));
           

        }
    }
}