
namespace DelftTools.Utils.Data
{
    //todo describe purpose of NameableEntity, it is not used.
    public abstract class NameableEntity: IUnique<long>, INameable
    {
        private long id;
        private string name;

        public virtual long Id
        {
            get { return id; }
            set { id = value; }
        }

        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
    }
}
