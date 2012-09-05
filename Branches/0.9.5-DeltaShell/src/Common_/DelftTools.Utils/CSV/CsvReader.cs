using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace DelftTools.Utils.CSV
{
    /// <summary>
    /// Wrapper class around LumenWorks csv reader from codeproject.
    /// </summary>
    public class CSVReader:IDisposable
    {
        private readonly LumenWorks.Framework.IO.Csv.CsvReader csvReader;

        public CSVReader(TextReader reader,bool hasHeaders):this(reader,hasHeaders,',')
        {
            
        }
        public CSVReader(TextReader reader,bool hasHeaders,char delimeter)
        {
            csvReader = new LumenWorks.Framework.IO.Csv.CsvReader(reader,hasHeaders,delimeter);
        }
        ///Do we really want char??? is string not nicers?
        public char Delimeter
        {
            get { return csvReader.Delimiter; }
        }
        public IDataReader ToDataReader()
        {
            return csvReader;
        }

        public void Dispose()
        {
            csvReader.Dispose();
        }
    }
}