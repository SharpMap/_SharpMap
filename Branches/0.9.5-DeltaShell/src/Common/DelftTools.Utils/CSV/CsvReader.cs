using System;
using System.Data;
using System.IO;
using LumenWorks.Framework.IO.Csv;

namespace DelftTools.Utils.CSV
{
    /// <summary>
    /// Wrapper class around LumenWorks csv reader from codeproject.
    /// </summary>
    public class CSVReader : IDisposable
    {
        private readonly CsvReader csvReader;

        public CSVReader(TextReader reader, bool hasHeaders) : this(reader, hasHeaders, ',')
        {
        }

        public CSVReader(TextReader reader, bool hasHeaders, char delimeter)
        {
            csvReader = new CsvReader(reader, hasHeaders, delimeter);
        }

        ///Do we really want char??? is string not nicers?
        public char Delimeter
        {
            get { return csvReader.Delimiter; }
        }

        /// <summary>
        /// Default behaviour fopr missing field is to throw a MissingFieldCsvException.
        /// If set to false the result will be set to null when a field is missing.
        /// </summary>
        public bool ErrorOnMissingField
        {
            get { return csvReader.MissingFieldAction == MissingFieldAction.ParseError; }
            set { csvReader.MissingFieldAction = value ? MissingFieldAction.ParseError : MissingFieldAction.ReplaceByNull; }
        }

        public int CurrentLineIndex
        {
            get { return (int)csvReader.CurrentRecordIndex; }
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