using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace DelftTools.Utils.CSV
{
    public class CommonCsvWriter
    {
        public static string WriteToString(DataTable table, bool header, bool quoteall, char delimiter = ',')
        {
            var writer = new StringWriter();
            WriteToStream(writer, table, header, quoteall, delimiter);
            return writer.ToString();
        }

        public static void WriteToStream(TextWriter stream, DataTable table, bool header, bool quoteall, char delimiter = ',')
        {
            //store culture
            var storedCulture = Thread.CurrentThread.CurrentCulture;
            var storedUICulture = Thread.CurrentThread.CurrentUICulture;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            try
            {
                var headerLine = table.Columns.Cast<DataColumn>().Aggregate("", (current, column) =>
                                                                            current + ConvertToFileString((header ? column.Caption : column.ColumnName), quoteall) + delimiter)
                                 .TrimEnd(delimiter) + '\n';

                stream.Write(headerLine);

                foreach (DataRow row in table.Rows)
                {
                    var line = row.ItemArray.Aggregate("", (lineData, item) => lineData + ConvertToFileString(item, quoteall) + delimiter);
                    stream.Write(line.Remove(line.Length -1) + '\n');
                }
            }
            finally
            {
                //reset culture
                Thread.CurrentThread.CurrentCulture = storedCulture;
                Thread.CurrentThread.CurrentUICulture = storedUICulture;
            }
        }

        private static string ConvertToFileString(object item, bool quoteall)
        {
            if (item == null) return "";

            var itemAsString = item.ToString();

            return (quoteall || itemAsString.IndexOfAny("\",\x0A\x0D".ToCharArray()) > -1)
                       ? "\"" + itemAsString.Replace("\"", "\"\"") + "\""
                       : itemAsString;
        }
    }
}
