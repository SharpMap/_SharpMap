using DelftTools.Utils.Aop.NotifyPropertyChanged;

namespace DelftTools.TestUtils.TestClasses
{
    /// <summary>
    /// Adding NotifyPropertyChanged attribute to the class declaration will ensure that INotifyPropertyChanged will be 
    /// injected on compilation time as additional aspect. During runtime it can be accessed in the following way:
    /// 
    /// INotifyPropertyChanged observable = Post.Cast<T, INotifyPropertyChanged>(o); 
    /// 
    /// It makes things a bit more implicit but also more clean (hopefully not too clean :)).
    /// </summary>
    [NotifyPropertyChanged]
    public class NotifiableTestClass
    {
        private string name = "some name";

        public virtual string Name
        {
            set { name = value; }
            get { return name; }
        }

        public virtual string AutoProperty { get; set; }

        public void SetNameUsingPrivateMethod(string name)
        {
            this.name = name;
        }
    }
}