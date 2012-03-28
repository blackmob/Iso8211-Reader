using System.Collections.Generic;
using System.Linq;

namespace GreaterHeights.ISO8211
{
    public class DdfField
    {
        public char[] Data { private get; set; }

        public int DataSize { private get; set; }
        public DdfFieldDefinition FieldDefinition { get; set; }

        public int Offset { private get; set; }

        private static int GetRepeatCount()
        {
            //the s-57 catalog.031 does not seem to have repeating fields in records so we just return 1
            return 1;
        }

        public Dictionary<string, SubFieldData> GetRecord()
        {
            var retVal = new Dictionary<string, SubFieldData>();

            var bytesRemaining = DataSize;

            for (var i = 0; i < GetRepeatCount(); i++)
            {
                var position = Offset;
                foreach (var subField in FieldDefinition.SubFieldDefinitions)
                {
                    int consumed;
                    var data = subField.GetData(Data.Skip(position).ToArray(), out consumed, bytesRemaining);
                    position += consumed;
                    bytesRemaining -= consumed;
                    retVal.Add(subField.Name,
                               new SubFieldData {DataType = subField.DataType, Name = subField.Name, Value = data});
                }
            }

            return retVal;
        }
    }
}