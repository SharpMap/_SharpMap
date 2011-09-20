namespace SharpMap.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Data;
    using Geometries;

    internal static class JsonHelper
    {
        internal static IEnumerable<GeoJSON> GetData(string layer, FeatureDataSet data)
        {
            if (String.IsNullOrEmpty(layer))
                throw new ArgumentNullException("layer");
            if (data == null)
                throw new ArgumentNullException("data");

            using (data)
            {
                foreach (FeatureDataTable table in data.Tables)
                {
                    DataColumnCollection columns = table.Columns;
                    DataRowCollection rows = table.Rows;
                    int count = rows.Count;
                    for (int i = 0; i < count; i++)
                    {
                        FeatureDataRow row = (FeatureDataRow)rows[i];
                        Geometry geometry = row.Geometry;
                        Dictionary<string, object> values = new Dictionary<string, object>();
                        for (int j = 0; j < row.ItemArray.Length; j++)
                        {
                            string key = columns[j].ColumnName;
                            object value = row.ItemArray[j];
                            values.Add(key, value);
                        }
                        values.Add("layer", layer);
                        yield return new GeoJSON(geometry, values);
                    }
                }
            }
        }        
    }
}