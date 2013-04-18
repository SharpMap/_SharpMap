using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using GeoAPI.Geometries;

namespace GeoAPI.Extensions.Coverages
{
    /// <summary>
    /// Each cell in the grid can be addressed by index (i, j) in two dimensions or (i, j, k) in three dimensions
    /// Each vertex has coordinates in 2D or in 3D for some real numbers dx, dy, and dz representing the grid spacing.
    /// 
    /// See http://en.wikipedia.org/wiki/Regular_grid for definition of a grids.
    /// See Web Coverage Service for a definition of different coverages: http://portal.opengeospatial.org/files/?artifact_id=1131
    /// </summary>
    public interface IRegularGridCoverage: ICoverage
    {
        /// <summary>
        /// X axis of the coverage. Every coverage is defined as a function with minimum 2 arguments: X, Y.
        /// This property is equal to 1st argument of the coverage function. <seealso cref="IVariable.Arguments"/>
        /// </summary>
        IVariable<double> X { get; }

        /// <summary>
        /// Y axis of the coverage. Every coverage is defined as a function with minimum 2 arguments: X, Y.
        /// This property is equal to 2nd argument of the coverage function. <seealso cref="IVariable.Arguments"/>
        /// </summary>
        IVariable<double> Y { get; }

        /// <summary>
        /// Angle of grid rotated around it's lower-left boundary.
        /// 
        /// TODO: probably should be part of CRS
        /// </summary>
        double Rotation { get; set; }

        /// <summary>
        /// Origin of the grid in world coordinates using current CRS.
        /// 
        /// TODO: probably should be part of CRS
        /// </summary>
        ICoordinate Origin { get; }

        /// <summary>
        /// Step of the grid along the X axis.
        /// </summary>
        double DeltaX { get; }

        /// <summary>
        /// Step of the grid along the Y axis.
        /// </summary>
        double DeltaY { get;  }

        /// <summary>
        /// Number of cells / values along the X axis. (Check if it is needed, there is already X.Values.Count)
        /// </summary>
        int SizeX { get; }

        /// <summary>
        /// Number of cells / values along the Y axis. (Check if it is needed, there is already X.Values.Count)
        /// </summary>
        int SizeY { get; }

        /// <summary>
        /// Updates grid size, stepsize, origin and geometry based on argument values (x and y)
        /// </summary>
        void UpdateGridGeometryAttributes();

        IRegularGridCoverage FilterAsRegularGridCoverage(params IVariableFilter[] filters);

        void Resize(int sizeX, int sizeY, double deltaX, double deltaY);

        void Resize(int sizeX, int sizeY, double deltaX, double deltaY,ICoordinate origin);

        void Resize(int sizeX, int sizeY, double deltaX, double deltaY, ICoordinate origin,bool setComponentValues);

        /// <summary>
        /// Returns coordinate of the cell x, y is in.
        /// eg.       x=0  x=1
        /// y=1
        /// y=0
        /// GetRegularGridCoverageCellAtPosition(0.0, 0.0) will return cell 0, 0
        /// GetRegularGridCoverageCellAtPosition(0.5, 0.0) will return cell 0, 0
        /// GetRegularGridCoverageCellAtPosition(0.0, 0.5) will return cell 0, 0
        /// GetRegularGridCoverageCellAtPosition(1.0, 1.0) will return cell 1, 1
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        IRegularGridCoverageCell GetRegularGridCoverageCellAtPosition(double x, double y);
    }
}