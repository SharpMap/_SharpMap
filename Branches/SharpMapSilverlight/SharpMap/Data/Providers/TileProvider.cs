using System;
using System.Collections.Generic;
using System.IO;
using BruTile;
using SharpMap.Geometries;
using SharpMap.Layers;
using SharpMap;
using System.Threading;
using BruTile.Cache;
using SharpMap.Data.Providers;
using System.Net;
using SharpMap.Data;
using SharpMap.Rasters;

namespace SharpMap.Providers
{
    public class TileProvider : IRasterProvider
    {
        #region Fields

        double minVisible = Double.MinValue;
        double maxVisible = Double.MaxValue;
        bool enabled = true;
        string layerName;
        int srid;
        ITileSource source;
        MemoryCache<byte[]> bitmaps = new MemoryCache<byte[]>(100, 200);

        #endregion

        #region Properties

        public double MinVisible
        {
            get { return minVisible; }
            set { minVisible = value; }
        }

        public double MaxVisible
        {
            get { return maxVisible; }
            set { maxVisible = value; }
        }

        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        public string LayerName
        {
            get { return layerName; }
            set { layerName = value; }
        }

        public BoundingBox GetExtents()
        {
            return this.source.Schema.Extent.ToBoundingBox();
        }

        public int SRID
        {
            get { return srid; }
            set { srid = value; }
        }

        #endregion

        public TileProvider(ITileSource tileSource, string layerName)
        {
            this.source = tileSource;
            this.layerName = layerName;
        }

        //class FetchTile
        //{
        //    ITileProvider requestBuilder;
        //    TileInfo tileInfo;
        //    AutoResetEvent autoResetEvent;
        //    MemoryCache<byte[]> bitmaps;

        //    public FetchTile(ITileProvider requestBuilder, TileInfo tileInfo, MemoryCache<byte[]> bitmaps, AutoResetEvent autoResetEvent)
        //    {
        //        this.requestBuilder = requestBuilder;
        //        this.tileInfo = tileInfo;
        //        this.bitmaps = bitmaps;
        //        this.autoResetEvent = autoResetEvent;
        //    }

        //    public void GetTileOnThread()
        //    {
        //        byte[] bytes;
        //        try
        //        {
        //            bitmaps.Add(tileInfo.Key, requestBuilder.GetTile(tileInfo));
        //        }
        //        catch
        //        {
        //            //!!!
        //        }
        //        finally
        //        {
        //            autoResetEvent.Set();
        //        }
        //    }
        //}

        public IList<IRaster> FetchTiles(BoundingBox boundingBox, double resolution)
        {
            Extent extent = new Extent(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Max.X, boundingBox.Max.Y);
            int level = BruTile.Utilities.GetNearestLevel(source.Schema.Resolutions, resolution);
            IList<TileInfo> tiles = source.Schema.GetTilesInView(extent, level);

            IList<WaitHandle> waitHandles = new List<WaitHandle>();

            foreach (TileInfo info in tiles)    
            {
                if (bitmaps.Find(info.Key) != null) continue;
                AutoResetEvent waitHandle = new AutoResetEvent(false);
                waitHandles.Add(waitHandle);
                ThreadPool.QueueUserWorkItem(GetTileOnThread, new object[] { source.Provider, info, bitmaps, waitHandle });
            }

            foreach (WaitHandle handle in waitHandles)
                handle.WaitOne();


            IList<IRaster> elements = new List<IRaster>(); 
            foreach (TileInfo info in tiles)
            {
                byte[] bitmap = bitmaps.Find(info.Key);
                if (bitmap == null) continue;
                elements.Add(new Raster(bitmap, new BoundingBox(info.Extent.MinX, info.Extent.MinY, info.Extent.MaxX, info.Extent.MaxY)));
            }
            return elements;
        }
        
        private void GetTileOnThread(object parameter)
        {
            object[] parameters = (object[])parameter;
            if (parameters.Length != 4) throw new ArgumentException("Four parameters expected");
            ITileProvider tileProvider = (ITileProvider)parameters[0];
            TileInfo tileInfo = (TileInfo)parameters[1];
            MemoryCache<byte[]> bitmaps = (MemoryCache<byte[]>)parameters[2];
            AutoResetEvent autoResetEvent = (AutoResetEvent)parameters[3];

            byte[] bytes;
            try
            {
                bitmaps.Add(tileInfo.Key, tileProvider.GetTile(tileInfo));
            }
            catch (Exception ex)
            {
                //todo: log and use other ways to report to user.
            }
            finally
            {
                autoResetEvent.Set();
            }
        }


        #region IRasterProvider Members

        public IList<IRaster> GetRastersInView(BoundingBox bbox, double resolution)
        {
            return FetchTiles(bbox, resolution);
        }

        #endregion
    }
}


namespace BruTile
{
    static class BruTileExtensions
    {
        public static BoundingBox ToBoundingBox(this Extent extent)
        {
            return new BoundingBox(
                extent.MinX,
                extent.MinY,
                extent.MaxX,
                extent.MaxY);
        }

    }
}
