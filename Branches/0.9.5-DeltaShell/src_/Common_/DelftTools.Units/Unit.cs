using System;

namespace DelftTools.Units
{
    [Serializable]
    public class Unit : IUnit
    {
        private IDimension dimension;
        private IUnitConvertor convertor;
        private string name;
        private string symbol;
        private long id;

        public Unit()
        {
            
        }

        public Unit(string name, string symbol, IDimension dimension)
        {
            this.name = name;
            this.symbol = symbol;
            this.dimension = dimension;
        }

        public Unit(string name)
        {
            this.name = name;   
        }

        public Unit(string name, string symbol)
        {
            this.name = name;
            this.symbol = symbol;
        }
        
        #region IUnit Members


        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// The unit dimension.
        /// </summary>
        /// <value>The dimension.</value>
        public IDimension Dimension
        {
            get { return dimension; }
        }

        /// <summary>
        /// The unit name.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        /// <summary>
        /// The unit symbol.
        /// </summary>
        public string Symbol
        {
            get { return symbol; }
            set { symbol = value; }
        }

        public IUnitConvertor GetConvertor(IUnit unit)
        {
            return convertor;
        }

        #endregion

        #region IUnit Members


       

        #endregion

        public object Clone()
        {
            return Activator.CreateInstance(GetType(), name, symbol);
        }

        public override string ToString()
        {
            return symbol;
        }
    }
}