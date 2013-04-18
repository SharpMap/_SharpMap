using System.Data;
using System.IO;

namespace DelftTools.Utils.CSV
{
    
    public class CommonCsvWriter
    {
        public char Delimiter { get; set;}
        
        public static string WriteToString(DataTable table, bool header, bool quoteall)
        {
            StringWriter writer = new StringWriter();
            WriteToStream(writer, table, header, quoteall);
            return writer.ToString();
        }

        public static void WriteToStream(TextWriter stream, DataTable table, bool header, bool quoteall)
        {
            if (header)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    WriteItem(stream, table.Columns[i].Caption, quoteall);
                    if (i < table.Columns.Count - 1)
                        stream.Write(',');
                    else
                        stream.Write('\n');
                }
            }
            else
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    WriteItem(stream, table.Columns[i].ColumnName,quoteall);
                    if (i < table.Columns.Count - 1)
                        stream.Write(',');
                    else
                        stream.Write('\n');
                }
            }
            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    WriteItem(stream, row[i], quoteall);
                    if (i < table.Columns.Count - 1)
                        stream.Write(',');
                    else
                        stream.Write('\n');
                }
            }
        }

        private static void WriteItem(TextWriter stream, object item, bool quoteall)
        {
            if (item == null)
                return;
            string s = item.ToString();
            if (quoteall || s.IndexOfAny("\",\x0A\x0D".ToCharArray()) > -1)
                stream.Write("\"" + s.Replace("\"", "\"\"") + "\"");
            else
                stream.Write(s);
        }

        
    }
}
