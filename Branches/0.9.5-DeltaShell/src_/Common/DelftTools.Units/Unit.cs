using System;
using DelftTools.Utils.Data;

namespace DelftTools.Units
{
    [Serializable]
    public class Unit : Unique<long>, IUnit
    {
        private IDimension dimension;
        private IUnitConvertor convertor;
        private string name;
        private string symbol;

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