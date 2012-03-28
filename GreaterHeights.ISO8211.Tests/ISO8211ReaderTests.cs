using NUnit.Framework;
using System.Diagnostics;

namespace GreaterHeights.ISO8211.Tests
{
    [TestFixture]
    public class Iso8211ReaderTests 
    {
        [Test]
        public void ShouldOpenTheCatalog031File()
        {
            using (var reader = new Iso8211Reader(@".\CATALOG.031"))
            {
                reader.Open();

                int i= 0;
                DdfRecord record;
                while ((record = reader.Read()) != null)
                {
                    i++;
                    foreach (var field in record.Fields)
                    {                         
                        Debug.WriteLine(field.FieldDefinition.FieldName);
                        var data = field.GetRecord();
                        foreach (var key in data.Keys)
                        {
                            Debug.WriteLine("{0}:{1}", key, data[key].Value);  
                        }
                    }                    
                }
                Debug.WriteLine(i);  
            }
        }
    }
}
