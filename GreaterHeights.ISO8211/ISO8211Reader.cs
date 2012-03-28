using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GreaterHeights.ISO8211
{
    public class Iso8211Reader : IIso8211Reader
    {
        private readonly List<DdfFieldDefinition> _fieldDefinitions;
        private readonly string _path;
        private readonly BinaryReader _reader;

        public Iso8211Reader(string path)
        {
            if (!File.Exists(path))
                throw new ArgumentOutOfRangeException(path, "File does not exist");

            _reader = new BinaryReader(new StreamReader(path).BaseStream);
            _path = path;

            _fieldDefinitions = new List<DdfFieldDefinition>();
        }

        public IEnumerable<DdfFieldDefinition> FieldDefinitions
        {
            get { return _fieldDefinitions; }
        }

        public DdfLeader Leader { get; set; }

        public BinaryReader Reader
        {
            get { return _reader; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_reader != null)
            {
                _reader.Close();
                _reader.Dispose();
            }
        }

        #endregion

        public void Open()
        {
            char[] leader = _reader.ReadChars(24);

            //chek if the leader is valid
            if (!LeaderIsValid(leader))
                throw new ApplicationException("File is invalid");

            //populate the leader record 
            Leader = DdfUtils.CreateLeader(leader, true);

            //read the record
            var record = ReadRecord(leader, Leader);

            var fieldEntryWidth = Leader.GetFieldEntryWidth();

            var fieldCount = DdfUtils.CountDirectoryEntries(record, Constants.LeaderSize, Leader.RecordLength,
                                                            fieldEntryWidth);

            ReadFieldDefinitions(Leader, record, fieldEntryWidth, fieldCount);
        }

        public DdfRecord Read()
        {
            var record = new DdfRecord();

            return record.Read(this) ? record : null;
        }

        public void ReadFieldDefinitions(DdfLeader ddfLeader, char[] record, int fieldEntryWidth, int fieldCount)
        {
            for (var i = 0; i < fieldCount; i++)
            {
                var entryOffset = Constants.LeaderSize + i*fieldEntryWidth;

                var tag = new String(record.Skip(entryOffset).Take(ddfLeader.SizeFieldTag).ToArray());

                entryOffset += ddfLeader.SizeFieldTag;
                var fieldLength = DdfUtils.ReadIntFromBuffer(record, entryOffset, ddfLeader.SizeFieldLength);

                entryOffset += ddfLeader.SizeFieldLength;
                var fieldPos = DdfUtils.ReadIntFromBuffer(record, entryOffset, ddfLeader.SizeFieldPosition);

                if (ddfLeader.FieldAreaStart + fieldPos < 0 ||
                    ddfLeader.RecordLength - (ddfLeader.FieldAreaStart + fieldPos) < fieldLength)
                {
                    throw new ApplicationException("Header record invalid on DDF file " + _path);
                }

                var fieldDef = new DdfFieldDefinition();
                if (fieldDef.Initialize(ddfLeader, tag, fieldLength, record, ddfLeader.FieldAreaStart + fieldPos))
                    _fieldDefinitions.Add(fieldDef);
            }
        }

        public char[] ReadRecord(char[] leader, DdfLeader ddfLeader)
        {
            var record = new char[ddfLeader.RecordLength];
            leader.CopyTo(record, 0);

            for (var i = 0; i < (ddfLeader.RecordLength - Constants.LeaderSize); i++)
            {
                var entry = _reader.ReadBytes(1);

                var charEntry = Encoding.UTF8.GetString(entry).ToCharArray();

                charEntry.CopyTo(record, (charEntry.Length*i) + Constants.LeaderSize);
            }
            return record;
        }

        public bool LeaderIsValid(char[] leader)
        {
            //Check the leader is valid.
            for (var i = 0; i < Constants.LeaderSize; i++)
            {
                if (leader[i] < 32 || leader[i] > 126)
                    return false;
            }
            if (leader[5] != '1' && leader[5] != '2' && leader[5] != '3')
                return false;
            if (leader[6] != 'L')
                return false;
            if (leader[8] != '1' && leader[8] != ' ')
                return false;

            return true;
        }
    }
}