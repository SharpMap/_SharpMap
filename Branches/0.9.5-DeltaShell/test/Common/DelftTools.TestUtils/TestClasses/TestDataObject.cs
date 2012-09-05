using DelftTools.Utils;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using DelftTools.Utils.Data;

namespace DelftTools.TestUtils.TestClasses
{
    /// <summary>
    /// Test data object which can be contained in the model / project (wrapperd by IDataItem implementation).
    /// </summary>
    [NotifyPropertyChanged]
    public class TestDataObject: IUnique<long>, INameable
    {
        private string field1;
        private long id;
        private string name;
        private static string defaultName = "test";

        public TestDataObject():this(defaultName)
        {
        }

        public TestDataObject(string name)
        {
            this.name = name;
        }

        public virtual string Field1
        {
            get { return field1; }
            set { field1 = value; }
        }

        #region IUnique<long> Members

        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        #endregion

        public string Name
        {
            get { return name; }
            set {name = value;}
        }
    }
}