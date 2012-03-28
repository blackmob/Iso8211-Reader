using System;
using System.Collections.Generic;
using System.IO;

namespace GreaterHeights.ISO8211
{
    public interface IIso8211Reader : IDisposable
    {
        IEnumerable<DdfFieldDefinition> FieldDefinitions { get; }
        DdfLeader Leader { get; set; }
        BinaryReader Reader { get; }
        void Open();
        DdfRecord Read();
        void ReadFieldDefinitions(DdfLeader ddfLeader, char[] record, int fieldEntryWidth, int fieldCount);
        char[] ReadRecord(char[] leader, DdfLeader ddfLeader);
        bool LeaderIsValid(char[] leader);
    }
}