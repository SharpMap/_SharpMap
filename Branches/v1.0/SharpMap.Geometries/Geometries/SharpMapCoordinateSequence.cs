using System.Collections.Generic;
using System.Diagnostics;
using GeoAPI.Geometries;

namespace SharpMap.Geometries
{
    /// <summary>
    /// 
    /// </summary>
    public class SharpMapCoordinateSequenceFactory : ICoordinateSequenceFactory
    {
        private SharpMapCoordinateSequenceFactory()
        {}
        
        /// <summary>
        /// 
        /// </summary>
        public static readonly SharpMapCoordinateSequenceFactory Instance = new SharpMapCoordinateSequenceFactory();
        
        #region Implementation of ICoordinateSequenceFactory

        public SharpMapCoordinateSequence Create(IList<Coordinate> coordinates)
        {
            return new SharpMapCoordinateSequence(coordinates);
        }
        public ICoordinateSequence Create(Coordinate[] coordinates)
        {
            return new SharpMapCoordinateSequence(coordinates);
        }

        public ICoordinateSequence Create(ICoordinateSequence coordSeq)
        {
            return new SharpMapCoordinateSequence(coordSeq);
        }

        public ICoordinateSequence Create(int size, int dimension)
        {
            return new SharpMapCoordinateSequence(size, dimension);
        }

        #endregion
    }
    
    /// <summary>
    /// 
    /// </summary>
    public class SharpMapCoordinateSequence : ICoordinateSequence
    {
        private readonly int _dimension;
        private readonly List<Coordinate> _coordinates;

        internal SharpMapCoordinateSequence(IEnumerable<Coordinate> coordinates)
        {
            _coordinates = new List<Coordinate>(coordinates);
        }

        internal SharpMapCoordinateSequence(ICoordinateSequence coordSeq)
        {
            _dimension = coordSeq.Dimension;
            _coordinates = new List<Coordinate>(coordSeq.Count);
            for (var i = 0; i < coordSeq.Count; i++)
            {
                _coordinates.Add(new Coordinate(
                                     coordSeq.GetOrdinate(i, 0),
                                     coordSeq.GetOrdinate(i, (Ordinate) 1),
                                     _dimension > 2
                                         ? coordSeq.GetOrdinate(i, (Ordinate) 2)
                                         : Coordinate.NullOrdinate)
                    );
            }
        }

        internal SharpMapCoordinateSequence(int size, int dimension)
        {
            _dimension = dimension;
            _coordinates = new List<Coordinate>(size);
        }


        #region Implementation of ICloneable

        public object Clone()
        {
            var coordinates = new Coordinate[_coordinates.Count];
            for (var i = 0; i < coordinates.Length; i++)
                coordinates[i] = (Coordinate) _coordinates[i].Clone();
            return new SharpMapCoordinateSequence((IList<Coordinate>)_coordinates);
        }

        #endregion

        #region Implementation of ICoordinateSequence

        public Coordinate GetCoordinate(int i)
        {
            return _coordinates[i];
        }

        public Coordinate GetCoordinateCopy(int i)
        {
            return new Coordinate(_coordinates[i]);
        }

        public void GetCoordinate(int index, Coordinate coord)
        {
            coord.CoordinateValue = _coordinates[index];
        }

        public double GetX(int index)
        {
            return _coordinates[index].X;
        }

        public double GetY(int index)
        {
            return _coordinates[index].Y;
        }

        public double GetOrdinate(int index, Ordinate ordinate)
        {
            return _coordinates[index][ordinate];
        }

        public void SetOrdinate(int index, Ordinate ordinate, double value)
        {
            Debug.Assert((int) ordinate < Dimension, "(int)ordinate < Dimension");
            _coordinates[index][ordinate] = value;
        }

        public Coordinate[] ToCoordinateArray()
        {
            return _coordinates.ToArray();
        }

        public Envelope ExpandEnvelope(Envelope env)
        {
            foreach (var coordinate in _coordinates)
            {
                env.ExpandToInclude(coordinate);
            }
            return env;
        }

        public int Dimension
        {
            get { return _dimension; }
        }

        public int Count
        {
            get { return _coordinates.Count; }
        }

        #endregion
    }
}