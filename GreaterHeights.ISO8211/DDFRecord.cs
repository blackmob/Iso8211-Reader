using System;
using System.Collections.Generic;
using System.Linq;

namespace GreaterHeights.ISO8211
{
    public class DdfRecord
    {
        private char[] _data;
        private bool _reuseHeader;

        public DdfRecord()
        {
            Fields = new List<DdfField>();
        }

        public List<DdfField> Fields { get; private set; }

        public bool Read(Iso8211Reader module)
        {
            if (!_reuseHeader)
                return ReadHeader(module);

            return true;
        }

        private bool ReadHeader(Iso8211Reader module)
        {
            if (module.Reader.BaseStream.Length == module.Reader.BaseStream.Position)
                return false;

            var leader = new char[Constants.LeaderSize];

            if (DdfUtils.ReadObjectsFromBuffer(module.Reader, leader, 0, Constants.LeaderSize, 1) !=
                Constants.LeaderSize)
            {
                throw new ArgumentException("Leader is short on file");
            }

            var ddfLeader = DdfUtils.CreateLeader(leader, false);

            _reuseHeader = ddfLeader.LeaderIdentifier == "R";

            /* -------------------------------------------------------------------- */
            /*      Is there anything seemly screwy about this record?              */
            /* -------------------------------------------------------------------- */
            if ((ddfLeader.RecordLength < 24 || ddfLeader.RecordLength > 100000000
                 || ddfLeader.FieldAreaStart < 24 || ddfLeader.FieldAreaStart > 100000)
                && (ddfLeader.RecordLength != 0))
            {
                throw new ApplicationException(
                    "Data record appears to be corrupt on DDF file.\n ensure that the files were uncompressed without modifying\n carriage return/linefeeds (by default WINZIP does this).");
            }

            if (ddfLeader.RecordLength != 0)
            {
                var dataSize = ddfLeader.RecordLength - Constants.LeaderSize;
                _data = new char[dataSize];

                if (DdfUtils.ReadObjectsFromBuffer(module.Reader, _data, 0, dataSize, 1) != dataSize)
                {
                    throw new ArgumentException("Data record is short on DDF file.");
                }

                /* -------------------------------------------------------------------- */
                /*      If we don't find a field terminator at the end of the record    */
                /*      we will read extra bytes till we get to it.                     */
                /* -------------------------------------------------------------------- */
                while (_data[dataSize - 1] != Constants.DdfFieldTerminator
                       && (dataSize == 0 || _data[dataSize - 2] != Constants.DdfFieldTerminator))
                {
                    dataSize++;
                    var moreData = new char[dataSize];
                    _data.CopyTo(moreData, 0);

                    if (DdfUtils.ReadObjectsFromBuffer(module.Reader, moreData, dataSize, 1, 1) != 1)
                    {
                        throw new ArgumentException("Data record is short on DDF file.");
                    }

                    _data = moreData;
                }

                var fieldEntryWidth = ddfLeader.GetFieldEntryWidth();
                /* -------------------------------------------------------------------- */
                /*      Loop over the directory entries, making a pass counting them.   */
                /* -------------------------------------------------------------------- */
                var fieldCount = DdfUtils.CountDirectoryEntries(_data, 0, dataSize, fieldEntryWidth);
                /* -------------------------------------------------------------------- */
                /*      Allocate, and read field definitions.                           */
                /* -------------------------------------------------------------------- */
                for (var i = 0; i < fieldCount; i++)
                {
                    var entryOffset = i*fieldEntryWidth;

                    /* -------------------------------------------------------------------- */
                    /*      Read the position information and tag.                          */
                    /* -------------------------------------------------------------------- */
                    var tag = new String(_data.Skip(entryOffset).Take(ddfLeader.SizeFieldTag).ToArray());

                    entryOffset += ddfLeader.SizeFieldTag;
                    var fieldLength = DdfUtils.ReadIntFromBuffer(_data, entryOffset, ddfLeader.SizeFieldLength);
                    entryOffset += ddfLeader.SizeFieldLength;
                    var fieldPos = DdfUtils.ReadIntFromBuffer(_data, entryOffset, ddfLeader.SizeFieldPosition);

                    var fieldDefn = module.FieldDefinitions.FirstOrDefault(f => f.Tag == tag);

                    if (fieldDefn == null)
                    {
                        throw new ApplicationException(String.Format("Undefined field {0} encountered in data record.",
                                                                     tag));
                    }

                    if (ddfLeader.FieldAreaStart + fieldPos - Constants.LeaderSize < 0 ||
                        dataSize - (ddfLeader.FieldAreaStart + fieldPos - Constants.LeaderSize) < fieldLength)
                    {
                        throw new ApplicationException(String.Format("Not enough byte to initialize field {0}.", tag));
                    }

                    /*      Assign info the DDFField.                                       */
                    /* -------------------------------------------------------------------- */
                    Fields.Add(new DdfField
                                   {
                                       FieldDefinition = fieldDefn,
                                       Data = _data,
                                       Offset = ddfLeader.FieldAreaStart + fieldPos - Constants.LeaderSize,
                                       DataSize = fieldLength
                                   });
                }
                return true;
            }

            return true;
        }
    }
}