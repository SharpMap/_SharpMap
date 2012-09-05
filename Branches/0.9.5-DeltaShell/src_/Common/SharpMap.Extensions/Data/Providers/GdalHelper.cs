using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Aop;
using GeoAPI.Geometries;
using log4net;
using OSGeo.GDAL;
using SharpMap.Converters.Geometries;
using SharpMap.Utilities;
using ValidationAspects;

namespace SharpMap.Extensions.Data.Providers
{
    /// <summary>
    /// Contains utility functions shared by GdalFeatureProvider and GdalRenderer
    /// </summary>
    [ParameterValidationAspect]
    public class GdalHelper
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (GdalHelper));

        /// <summary>
        /// BandIndex 1 based index
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataset"></param>
        /// <param name="bandIndex"></param>
        /// <param name="xOff">X position</param>
        /// <param name="yOff">Y position</param>
        /// <param name="selectionSizeX">n colomns</param>
        /// <param name="selectionSizeY">n rows</param>
        /// <returns></returns>
        public static T[] GetValuesForBand<T>(Dataset dataset, int bandIndex, int xOff, int yOff, int selectionSizeX,
                                              int selectionSizeY)
        {
            return GetValuesForBand<T>(dataset, bandIndex, xOff, yOff, selectionSizeX, selectionSizeY, selectionSizeX,
                                       selectionSizeY);
        }


        public static void SetValuesForBand<T>(Dataset dataset, int bandIndex, int xOff, int yOff, int selectionSizeX,
                                               int selectionSizeY, T[] values)
        {
            SetValuesForBand(dataset, bandIndex, xOff, yOff, selectionSizeX, selectionSizeY, selectionSizeX,
                             selectionSizeY, values);
        }


        public static void SetValuesForBand<T>(Dataset dataset, int bandIndex, int xOff, int yOff, int width, int height,
                                               int imageWidth, int imageHeight, T[] values)
        {
            if (bandIndex > dataset.RasterCount)
            {
                throw new ArgumentOutOfRangeException("bandIndex", "Band index is out of dataset rasterband ranges");
            }

            using (Band band = dataset.GetRasterBand(bandIndex))
            {
                //flip upside down
                FlipUpsideDown(imageWidth, imageHeight, values);


                CPLErr err;
                if (typeof (T) == typeof (double))
                {
                    err = band.WriteRaster(xOff, yOff, width, height, values as double[], imageWidth, imageHeight,
                                           sizeof (double), 0);
                }
                else if (typeof (T) == typeof (float))
                {
                    err = band.WriteRaster(xOff, yOff, width, height, values as float[], imageWidth, imageHeight,
                                           sizeof (float), 0);
                }
                else if (typeof (T) == typeof (int))
                {
                    err = band.WriteRaster(xOff, yOff, width, height, values as int[], imageWidth, imageHeight,
                                           sizeof (int), 0);
                }
                else if (typeof (T) == typeof (double))
                {
                    err = band.WriteRaster(xOff, yOff, width, height, values as double[], imageWidth, imageHeight,
                                           sizeof (double), 0);
                }
                else if (typeof (T) == typeof (byte))
                {
                    err = band.WriteRaster(xOff, yOff, width, height, values as byte[], imageWidth, imageHeight,
                                           sizeof (byte), 0);
                }

                else throw new NotImplementedException(String.Format("Cannot write data of type {0}", typeof (T)));

                if (CPLErr.CE_None != err)
                {
                    throw new IOException("Could not write raster data");
                }

                band.FlushCache();
            }
        }

        /// <summary>
        /// flips data of 2d grid stored in 1-d array along x-axis
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="imageWidth"></param>
        /// <param name="imageHeight"></param>
        /// <param name="values"></param>
        private static void FlipUpsideDown<T>(int imageWidth, int imageHeight, T[] values)
        {

            // data is stored in 1-d array

            //----row1---->----row2---->----row3---->
            //and we wish to obtain
            //----row3---->----row2---->----row1---->

            for (int k = 0; k < imageWidth; k++)
            {
                // implicit rounding of result imageheight/2 ensures that it will work with
                // an uneven number of rows (middle row is skipped.
                for (int j = 0; j < imageHeight/2; j++) 
                {
                    T swap1 = values[k + j*imageWidth];
                    T swap2 = values[(imageHeight - 1 - j)*imageWidth + k];
                    values[k + j*imageWidth] = swap2;
                    values[(imageHeight - 1 - j)*imageWidth + k] = swap1;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataset"></param>
        /// <param name="bandIndex"></param>
        /// <param name="xOff"></param>
        /// <param name="yOff"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="imageWidth"></param>
        /// <param name="imageHeight"></param>
        /// <returns></returns>
        public static T[] GetValuesForBand<T>(Dataset dataset, int bandIndex, int xOff, int yOff, int width, int height,
                                              int imageWidth, int imageHeight)
        {
            if (bandIndex > dataset.RasterCount)
            {
                throw new Exception("Band index is out of dataset rasterband ranges");
            }

            using (Band band = dataset.GetRasterBand(bandIndex))
            {
                var values = new T[imageWidth*imageHeight];


                if (typeof (T) == typeof (double))
                    band.ReadRaster(xOff, yOff, width, height, values as double[], imageWidth, imageHeight,
                                    sizeof (double),
                                    0);
                else if (typeof (T) == typeof (float))
                    band.ReadRaster(xOff, yOff, width, height, values as float[], imageWidth, imageHeight,
                                    sizeof (float), 0);
                else if (typeof(T) == typeof(int))
                    band.ReadRaster(xOff, yOff, width, height, values as int[], imageWidth, imageHeight, sizeof(int), 0);
                else if (typeof(T) == typeof(uint))
                    band.ReadRaster(xOff, yOff, width, height, values as int[], imageWidth, imageHeight, sizeof(int), 0);
                else if (typeof(T) == typeof(Int16))
                    band.ReadRaster(xOff, yOff, width, height, values as short[], imageWidth, imageHeight, sizeof(short), 0);

                else if (typeof (T) == typeof (double))
                    band.ReadRaster(xOff, yOff, width, height, values as double[], imageWidth, imageHeight,
                                    sizeof (double),
                                    0);
                else if (typeof (T) == typeof (byte))
                    band.ReadRaster(xOff, yOff, width, height, values as byte[], imageWidth, imageHeight, sizeof (byte),
                                    0);
                

                FlipUpsideDown(imageWidth, imageHeight, values);


                return values;
            }
        }


        /// <summary>
        /// Get Variable for Gdal.DataType, name = name of variable
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IVariable GetVariableForDataType(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.GDT_Float32:
                case DataType.GDT_CFloat32:
                    return new Variable<float>();
                case DataType.GDT_Float64:
                case DataType.GDT_CFloat64:
                    return new Variable<double>();
                case DataType.GDT_Int16:
                case DataType.GDT_CInt16:
                    return new Variable<int>(); //hack: read 16 bit integer as int type.
                case DataType.GDT_Int32:
                case DataType.GDT_CInt32:
                    return new Variable<Int32>();
                case DataType.GDT_UInt16:
                    return new Variable<UInt16>();
                case DataType.GDT_UInt32:
                    return new Variable<UInt32>();
                case DataType.GDT_Byte:
                    return new Variable<Byte>();
            }
            return null;
        }

        public static IEnvelope GetExtents([NotNull] Dataset dataset)
        {
            if (dataset == null) return null;
            var geoTrans = new double[6];
            dataset.GetGeoTransform(geoTrans);
            CPLErr err = Gdal.GetLastErrorType();
            if (CPLErr.CE_None != err)
            {
                throw new ApplicationException("GetGeoTransform() failed.");
            }

            var regularGridGeoTransform = new RegularGridGeoTransform(geoTrans);

            return GeometryFactory.CreateEnvelope(regularGridGeoTransform.Left,
                                                  regularGridGeoTransform.Left +
                                                  (regularGridGeoTransform.HorizontalPixelResolution*
                                                   dataset.RasterXSize),
                                                  regularGridGeoTransform.Top -
                                                  (regularGridGeoTransform.VerticalPixelResolution*
                                                   dataset.RasterYSize),
                                                  regularGridGeoTransform.Top);
        }

        public static Dictionary<Color, double> ReadEsriGridLegend(string filename)
        {
            var colorMap = new Dictionary<Color, double>();

            // read legend
            if (File.Exists(filename))
            {
                string legend = File.ReadAllText(filename);
                if (Regex.IsMatch(legend, "NoDataValueEnabled=-1")) // NoDataValues are enabled (-1)
                {
                    if (Regex.IsMatch(legend, "NoDataValueTransparent=-1")) // NoDataValues are transparent
                    {
                        //Match noDataValueMinMatch = Regex.Match(legend, "NoDataValueMin=(-?[0-9]+(\\.[0-9]+)?)");
                        Match noDataValueMaxMatch = Regex.Match(legend, "NoDataValueMax=(-?[0-9]+(\\.[0-9]+)?)");
                        //double noDataValueMin = Double.Parse(noDataValueMinMatch.Groups[1].Value);
                        double noDataValueMax = Double.Parse(noDataValueMaxMatch.Groups[1].Value);

                        colorMap.Add(Color.Transparent, noDataValueMax);
                    }
                }
                foreach (Match match in Regex.Matches(legend, "Range([1-9]+)=([0-9]+.?[0-9]*),([0-9A-Fa-f]{1,6})"))
                {
                    var stringBuilder = new StringBuilder();
                    while ((match.Groups[3].Value.Length + stringBuilder.Length) < 6)
                    {
                        stringBuilder.Append("0");
                    }
                    stringBuilder.Append(match.Groups[3].Value);

                    colorMap.Add(ParseHexStringAsColor(stringBuilder.ToString()),
                                 Double.Parse(match.Groups[2].Value.Replace('.', ',')));
                }
            }
            else
            {
                //TODO: add default legend
            }

            return colorMap;
        }

        public static string[] SupportedDriverNames
        {
            get
            {
                int driverCount = Gdal.GetDriverCount();
                var driverNames = new string[driverCount];
                for (int i = 0; i < driverCount; i++)
                {
                    Driver driver = Gdal.GetDriver(i);
                    driverNames[i] = driver.ShortName + "(" + driver.LongName + ")";
                }

                return driverNames;
            }
        }

        public static string GetDriverName(string path)
        {
            string extension = Path.GetExtension(path);

            if (extension.Equals(".asc", StringComparison.OrdinalIgnoreCase))
            {
                return "AAIGrid";
            }
            if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            {
                return "AAIGrid";
            }
            if (extension.Equals(".bil", StringComparison.OrdinalIgnoreCase))
            {
                return "EHdr";
            }
            if (extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".tif", StringComparison.OrdinalIgnoreCase))
            {
                return "GTiff";
            }
            if(extension.Equals(".map",StringComparison.OrdinalIgnoreCase))
            {
                return "PCRaster";
            }
            if (extension.Equals(".pcr", StringComparison.OrdinalIgnoreCase))
            {
                return "PCRaster";
            }
            throw new NotImplementedException(String.Format("No suitable driver for the following file type: {0}",
                                                            Path.GetExtension(path)));
        }


        private static Color ParseHexStringAsColor(string colorString)
        {
            Color color = Color.FromArgb(
                ParseHexStringAsInt(colorString.Substring(4, 2)),
                ParseHexStringAsInt(colorString.Substring(2, 2)),
                ParseHexStringAsInt(colorString.Substring(0, 2))
                );

            return color;
        }

        private static int ParseHexStringAsInt(string hexNumber)
        {
            var hexDigits =
                new List<string>(new[]
                                     {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F"});

            int upperDigit = hexDigits.IndexOf(hexNumber.Substring(0, 1).ToUpper());
            int lowerDigit = hexDigits.IndexOf(hexNumber.Substring(1, 1).ToUpper());

            return upperDigit*16 + lowerDigit;
        }

        /// <summary>
        /// Rewrites the HDR to include the pixeltype field.. (Remember to make a backup!)
        /// Perhaps use float as default if nBits is 32 pixels.. (Historically only floats
        /// were used in combo with 32 bits)
        /// </summary>
        /// <param name="path"></param>
        public static void CheckBilHdrFileForPixelTypeField(string path)
        {
            string headerFile = Path.ChangeExtension(path, ".hdr");
            if (!File.Exists(headerFile))
            {
                throw new IOException(String.Format(
                                          "There is no matching header file for the following bil file: '{0}", path));
            }

            StreamReader sr = File.OpenText(headerFile);
            var buffer = new StringBuilder();
            buffer.Append(sr.ReadToEnd());
            sr.Close();

            //only fix header in case of 32 bit data
            const string pattern = @"NBITS[\s\t]+32";
            const string pattern1 = @"NBITS[\s\t]+16";
            if (Regex.Match(buffer.ToString(), pattern,RegexOptions.IgnoreCase).Success) 
            {
                if (buffer.ToString().ToLower().IndexOf("pixeltype") == -1)
                {
                    log.Warn("PixelType is missing in HDR file, switching to float and fixing header.");
                    buffer.AppendLine("PixelType Float");
                    using (StreamWriter sw = File.CreateText(headerFile))
                    {
                        sw.Write(buffer);
                        sw.Close();
                    }
                }
            }


            else if (Regex.Match(buffer.ToString(), pattern1, RegexOptions.IgnoreCase).Success)
            {
                if (buffer.ToString().ToLower().IndexOf("pixeltype") == -1)
                {
                    log.Warn("PixelType is missing in HDR file, switching to signed int and fixing header.");
                    buffer.AppendLine("PixelType SIGNEDINT");
                    using (StreamWriter sw = File.CreateText(headerFile))
                    {
                        sw.Write(buffer);
                        sw.Close();
                    }
                }
            }

        }


        public static string GetFileFilters()
        {
            string fileFilter = "";
            fileFilter += "All supported raster formats|*.asc;*.bil;*.tif;*.tiff;*.map";
            fileFilter += "|" + "Arc/Info ASCII Grid (*.asc)|*.asc";
            fileFilter += "|" + "ESRI .hdr Labelled (*.bil)|*.bil";
            fileFilter += "|" + "TIF Tagget Image File Format (*.tif)|*.tif;*.tiff";
            fileFilter += "|" + "PCRaster raster file format (*.map)|*.map";
            return fileFilter;
/*            Gdal.AllRegister();
            for (int i=0;i<Gdal.GetDriverCount();i++)
            {
                if(i>0)
                {
                    fileFilter += "|";
                }
                Driver driver = Gdal.GetDriver(i);
                
                string fileExtension = driver.GetMetadataItem("DMD_EXTENSION", null);
                if (string.IsNullOrEmpty(fileExtension))
                {
                    continue;
                }
                fileFilter += driver.GetDescription() + " (*." + fileExtension + ") *." + fileExtension;
            }
            return fileFilter;
 */
        }

        /// <summary>
        /// Extra line may have been added by Sobek to asc file. If found make a backup and fix header by
        /// overwriting original
        /// </summary>
        /// <param name="path"></param>
        public static void CheckAndFixAscHeader(string path)
        {
            bool hasCommentedLine;
            //string commentedLinePattern = @"(/\*([^*]|[\r\n]|(\*+([^*/]|[\r\n])))*\*+/)|(//.*)";

            using (TextReader reader = new StreamReader(path))
            {
                string lineString = reader.ReadLine();
                hasCommentedLine = lineString != null && lineString.StartsWith("/*");
            }
            if (hasCommentedLine)
            {
                string path1 = Path.GetTempFileName();
                using (var reader = new StreamReader(path))
                {
                    using (TextWriter writer = new StreamWriter(path1))
                    {
                        while (!reader.EndOfStream)
                        {
                            string lineString = reader.ReadLine();

                            if (!lineString.StartsWith("/*"))
                            {
                                writer.WriteLine(lineString);
                            }
                        }
                    }
                }

                if (File.Exists(path1))
                {
                    //create backup of existing file 
                    File.Copy(path, path + ".bak", true);
                    File.Delete(path);
                    File.Move(path1, path);
                }
            }
        }

        public static Driver GetInMemoryDriver()
        {
            return Gdal.GetDriverByName("MEM");
        }

        public static bool IsInMemoryDriver(Driver driver)
        {
            return driver.ShortName.Equals("MEM");
        }

        public static bool IsCreateSupported(Driver gdalDriver)
        {
            return (gdalDriver.GetMetadataItem("DCAP_CREATE", null) == "YES");
        }

        public static bool IsCreateCopySupported(Driver gdalDriver)
        {
            return (gdalDriver.GetMetadataItem("DCAP_CREATECOPY", null) == "YES");
        }

        public static IEnumerable<DataType> GetSupportedValueTypes(Driver gdalDriver)
        {
            var metadataItem = gdalDriver.GetMetadataItem("DMD_CREATIONDATATYPES", null);
            if(metadataItem == null)
            {
                return Enumerable.Empty<DataType>();
            }

            var destinationTypeList = metadataItem.Split(new char[] {' '});
            var types = new HashSet<DataType>();

            //log.DebugFormat("Supported GDAL value types:");
            for (var i = 0; i < destinationTypeList.Length; i++)
            {
                types.Add(ToGDalDataType(destinationTypeList[i]));

                //log.DebugFormat("  {0}", ToGDalDataType(destinationTypeList[i]));
            }

            return types;
        }

        private static DataType ToGDalDataType(string type)
        {
            var typeEnumMemberName = "GDT_" + type;
            return (DataType) Enum.Parse(typeof (DataType), typeEnumMemberName);
        }

        public static DataType GetGdalDataType(Driver gdalDriver, Type type)
        {
            var dataTypes = GetSupportedValueTypes(gdalDriver);

            var types = dataTypes.ToDictionary(k => k);

            if (type == typeof(long) || type == typeof(ulong))
                return DataType.GDT_Unknown;

            if (type == typeof(int))
                goto TInt;

            if (type == typeof(uint))
                goto TUInt;

            if (type == typeof(short))
                goto TShort;

            if (type == typeof(ushort))
                goto TUShort;

            if (type == typeof(float))
                goto TFloat;

            if (type == typeof(byte))
                goto TByte;

            if (types.ContainsKey(DataType.GDT_CFloat64))
                return DataType.GDT_CFloat64;

            if (types.ContainsKey(DataType.GDT_Float64))
                return DataType.GDT_Float64;

        TFloat:
            if (types.ContainsKey(DataType.GDT_CFloat32))
                return DataType.GDT_CFloat32;

            if (types.ContainsKey(DataType.GDT_Float32))
                return DataType.GDT_Float32;

        TInt:
            if (types.ContainsKey(DataType.GDT_CInt32))
                return DataType.GDT_CInt32;

            if (types.ContainsKey(DataType.GDT_Int32))
                return DataType.GDT_Int32;

        TUInt:
            if (types.ContainsKey(DataType.GDT_UInt32))
                return DataType.GDT_UInt32;

        TShort:
            if (types.ContainsKey(DataType.GDT_CInt16))
                return DataType.GDT_CInt16;

            if (types.ContainsKey(DataType.GDT_Int16))
                return DataType.GDT_Int16;

        TUShort:
            if (types.ContainsKey(DataType.GDT_UInt16))
                return DataType.GDT_UInt16;

        TByte:
            if (types.ContainsKey(DataType.GDT_Byte))
                return DataType.GDT_Byte;

            return DataType.GDT_Unknown;
        }
    }
}