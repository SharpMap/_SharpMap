using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DelftTools.Utils;
using DelftTools.Utils.Data;
using GeoAPI.CoordinateSystems;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Features;
using SharpMap.Api;
using SharpMap.Data.Providers;
using log4net;

namespace SharpMap.Extensions.Data.Providers
{
    /// <summary>
    /// Represents a (readonly) land boundary file (file format used at Deltares).
    /// </summary>
    public class LdbFile : Unique<long>, IFileBasedFeatureProvider
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LdbFile));

        private IList<IFeature> features;
        private IEnvelope extend;

        public LdbFile()
        {
        }

        public LdbFile(string path)
        {
            Open(path);
        }

        public virtual void Close()
        {
            IsOpen = false;
            features = null;
            extend = null;
        }

        public virtual void Open(string path)
        {
            Close();
            Path = path;

            if (!File.Exists(Path))
                log.ErrorFormat("Unable to open ldb file: {0}", Path);

            IsOpen = true;
            ReadFeatures();
        }

        private void ReadFeatures()
        {
            features = new List<IFeature>();
            extend = new Envelope();

            var lineNumber = 1;

            string line = "";
            try
            {
                using (var fileStream = File.OpenRead(Path))
                using (var streamReader = new StreamReader(fileStream))
                using (CultureUtils.SwitchToInvariantCulture())
                {
                    do
                    {
                        do // skip empty, comment & name lines
                        {
                            line = streamReader.ReadLine();
                            lineNumber++;
                        } while (string.IsNullOrEmpty(line) || line.StartsWith("*"));

                        var sizeLine = streamReader.ReadLine();
                        lineNumber++;
                        var sizeSplit = Split(sizeLine);
                        var numCoordinates = int.Parse(sizeSplit[0]);

                        var coordinates = new List<ICoordinate>();
                        for (var i = 0; i < numCoordinates; i++)
                        {
                            var coordinateLine = streamReader.ReadLine();
                            lineNumber++;
                            var coordinateSplit = Split(coordinateLine);

                            var x = double.Parse(coordinateSplit[0]);
                            var y = double.Parse(coordinateSplit[1]);

                            if (Math.Abs(x - 999.999) < 0.00001 && Math.Abs(y - 999.999) < 0.00001) //coordinate split; new feature
                            {
                                AddFeature(features, coordinates, extend);
                                coordinates.Clear();
                            }
                            else
                            {
                                coordinates.Add(new Coordinate(x, y));
                            }
                        }
                        AddFeature(features, coordinates, extend);

                        line = streamReader.ReadLine();
                        lineNumber++; // read (skip) feature name
                    } while (line != null);
                }
            }
            catch (Exception e)
            {
                log.ErrorFormat("Error parsing ldb file, line {0}: {1}. Error: {2}", lineNumber, line, e.Message);
            }
        }

        private static void AddFeature(ICollection<IFeature> features, List<ICoordinate> coordinates, IEnvelope envelope)
        {
            IGeometry geometry = null;

            if (coordinates.Count == 1)
            {
                var coordinate = coordinates.First();
                geometry = new LineString(new[] {coordinate, coordinate});
            }
            else if (coordinates.Count > 1)
            {
                geometry = new LineString(coordinates.ToArray());
            }

            if (geometry == null) 
                return;

            envelope.ExpandToInclude(geometry.EnvelopeInternal);
            features.Add(new Feature { Geometry = geometry });
        }

        private static string[] Split(string str)
        {
            return str.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
        }

        public virtual void CopyTo(string newPath)
        {
            File.Copy(Path, newPath, true);
        }

        public virtual void SwitchTo(string newPath)
        {
            Close();
            Open(newPath);
        }

        public virtual void Delete()
        {
            var path = Path;
            Close();
            File.Delete(path);
        }

        public virtual void Dispose()
        {
            Close();
        }

        public virtual Type FeatureType
        {
            get { return typeof(Feature); }
        }

        public virtual IList Features 
        {
            get { return (IList)features; }
        }

        public virtual bool IsReadOnly { get { return true; } }

        public virtual string Path { get; set; }

        public virtual bool IsOpen { get; protected set; }

        public virtual IEnumerable<string> Paths
        {
            get { yield return Path; }
        }

        public virtual void CreateNew(string path)
        {
            throw new NotImplementedException();
        }

        public virtual IFeature Add(IGeometry geometry)
        {
            throw new NotSupportedException("Provider is readonly");
        }

        public virtual Func<IFeatureProvider, IGeometry, IFeature> AddNewFeatureFromGeometryDelegate { get; set; }

        public virtual event EventHandler FeaturesChanged;

        public virtual IGeometry GetGeometryByID(int oid)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<IFeature> GetFeatures(IEnvelope box)
        {
            return features.Where(f => box.Intersects(f.Geometry.Envelope.EnvelopeInternal));
        }

        public virtual int GetFeatureCount()
        {
            return features.Count;
        }

        public virtual IFeature GetFeature(int index)
        {
            return features[index];
        }

        public virtual bool Contains(IFeature feature)
        {
            return features.Contains(feature);
        }

        public virtual int IndexOf(IFeature feature)
        {
            return features.IndexOf(feature);
        }

        public virtual IEnvelope GetExtents()
        {
            return extend;
        }

        public virtual string SrsWkt { get; set; }

        public virtual ICoordinateSystem CoordinateSystem { get; set; }

        public virtual IEnvelope GetBounds(int recordIndex)
        {
            return GetFeature(recordIndex).Geometry.EnvelopeInternal;
        }

        public virtual string FileFilter { get { return "Land boundary file (*.ldb)|*.ldb"; } }

        public virtual bool IsRelationalDataBase { get { return false; } }
    }
}