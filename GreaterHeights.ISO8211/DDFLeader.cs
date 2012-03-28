using System.Collections.Generic;

namespace GreaterHeights.ISO8211
{
    public class DdfLeader
    {
        public int RecordLength { get; set; }
        public string InterchangeLevel { internal get; set; }
        public string LeaderIdentifier { get; set; }
        public string InlineCodeExtensionIndicator { internal get; set; }
        public string VersionNumber { internal get; set; }
        public string AppIndicator { internal get; set; }
        public int FieldControlLength { get; set; }
        public int FieldAreaStart { get; set; }
        public IEnumerable<char> ExtendedCharSet { internal get; set; }
        public int SizeFieldLength { get; set; }
        public int SizeFieldPosition { get; set; }
        public int SizeFieldTag { get; set; }

        public int GetFieldEntryWidth()
        {
            return SizeFieldLength + SizeFieldPosition + SizeFieldTag;
        }
    }
}