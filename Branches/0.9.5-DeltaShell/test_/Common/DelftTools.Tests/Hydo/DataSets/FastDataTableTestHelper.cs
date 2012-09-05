using System;
using System.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using DelftTools.Hydro.CrossSections.DataSets;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo.DataSets
{
    public class FastDataTableTestHelper
    {
        /// <summary>
        /// Creates a table. Adds rows using the addRowAction. Serializes and deserializes and test if this is correct and fast.
        /// </summary>
        /// <typeparam name="T">Type of table to test</typeparam>
        /// <param name="maxMilliSeconds">Maximum allowed time for a serialization roundtrip</param>
        /// <param name="rowCount">Number of rows to add to the table</param>
        /// <param name="addRowAction">Action to add a new row to the table</param>
        public static void TestSerializationIsFastAndCorrect<T>(int maxMilliSeconds,int rowCount, Action<T> addRowAction) where T : DataTable, new()
        {
            var table = new T();//create the table
            for (int i = 0; i < rowCount; i++)
            {
                addRowAction(table);
            }

            T retrievedTable = null;
            TestHelper.AssertIsFasterThan(maxMilliSeconds, () =>
                                              retrievedTable = SerializeAndDeserialize(table));

            Assert.IsTrue(table.ContentEquals(retrievedTable));
        }
        
        public static T SerializeAndDeserialize<T>(T obj) 
        {
            var memoryStream = new MemoryStream();
            var binaryFormatter = new BinaryFormatter();
            
            binaryFormatter.Serialize(memoryStream, obj);
            
            memoryStream.Seek(0,0);
            return (T) binaryFormatter.Deserialize(memoryStream);
        }
    }
}