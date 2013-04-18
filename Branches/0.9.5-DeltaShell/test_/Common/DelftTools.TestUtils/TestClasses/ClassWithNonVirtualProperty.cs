using DelftTools.Utils.Aop;

namespace DelftTools.Tests.Utils.Aop.TestClasses
{
    /// <summary>
    /// AOP attribute can only be applied to a virtual property. In this
    /// case the AOPFactory should raise an exception because the property Street to
    /// which the attribute is applied is not virtual.
    /// </summary>
    public class ClassWithNonVirtualProperty
    {
        private string street;

        [NotifyPropertyChanged]
        public string Street
        {
            get { return street; }set{ street = value;}
        }

    }
}