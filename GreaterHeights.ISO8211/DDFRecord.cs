// ***********************************************************************
// Assembly         : GreaterHeights.ISO8211
// Author           : Ben Blackmore
// Created          : 05-08-2014
//
// Last Modified By : Ben Blackmore
// Last Modified On : 05-08-2014
// ***********************************************************************
// <copyright file="DDFRecord.cs" company="Greater Heights Ltd">
//     Copyright (c) Greater Heights Ltd. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************


namespace GreaterHeights.ISO8211
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The ddf record.
    /// </summary>
    public class DdfRecord
    {
        /// <summary>
        /// The _data.
        /// </summary>
        private char[] _data;

        /// <summary>
        /// The _reuse header.
        /// </summary>
        private bool _reuseHeader;

        /// <summary>
        /// Initializes a new instance of the <see cref="DdfRecord" /> class.
        /// </summary>
        public DdfRecord()
        {
            this.Fields = new List<DdfField>();
        }

        /// <summary>
        /// Gets the fields.
        /// </summary>
        /// <value>The fields.</value>
        public List<DdfField> Fields { get; private set; }

        /// <summary>
        /// The read.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns>The <see cref="bool" />.</returns>
        public bool Read(Iso8211Reader module)
        {
            if (!this._reuseHeader)
            {
                return this.ReadHeader(module);
            }

            return true;
        }

        /// <summary>
        /// The read header.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns>The <see cref="bool" />.</returns>
        /// <exception cref="System.ArgumentException">
        /// Leader is short on file
        /// or
        /// Data record is short on DDF file.
        /// or
        /// Data record is short on DDF file.
        /// </exception>
        /// <exception cref="System.ApplicationException">
        /// Data record appears to be corrupt on DDF file.\n ensure that the files were uncompressed without modifying\n carriage return/linefeeds (by default WINZIP does this).
        /// or
        /// or
        /// </exception>
        private bool ReadHeader(Iso8211Reader module)
        {
            if (module.Reader.BaseStream.Length == module.Reader.BaseStream.Position)
            {
                return false;
            }

            var leader = new char[Constants.LeaderSize];

            if (DdfUtils.ReadObjectsFromBuffer(module.Reader, leader, 0, Constants.LeaderSize, 1)
                != Constants.LeaderSize)
            {
                throw new ArgumentException("Leader is short on file");
            }

            DdfLeader ddfLeader = DdfUtils.CreateLeader(leader, false);

            this._reuseHeader = ddfLeader.LeaderIdentifier == "R";

            /* -------------------------------------------------------------------- */
            /*      Is there anything seemly screwy about this record?              */
            /* -------------------------------------------------------------------- */
            if ((ddfLeader.RecordLength < 24 || ddfLeader.RecordLength > 100000000 || ddfLeader.FieldAreaStart < 24
                 || ddfLeader.FieldAreaStart > 100000) && (ddfLeader.RecordLength != 0))
            {
                throw new ApplicationException(
                    "Data record appears to be corrupt on DDF file.\n ensure that the files were uncompressed without modifying\n carriage return/linefeeds (by default WINZIP does this).");
            }

            if (ddfLeader.RecordLength != 0)
            {
                int dataSize = ddfLeader.RecordLength - Constants.LeaderSize;
                this._data = new char[dataSize];

                if (DdfUtils.ReadObjectsFromBuffer(module.Reader, this._data, 0, dataSize, 1) != dataSize)
                {
                    throw new ArgumentException("Data record is short on DDF file.");
                }

                /* -------------------------------------------------------------------- */
                /*      If we don't find a field terminator at the end of the record    */
                /*      we will read extra bytes till we get to it.                     */
                /* -------------------------------------------------------------------- */
                while (this._data[dataSize - 1] != Constants.DdfFieldTerminator
                       && (dataSize == 0 || this._data[dataSize - 2] != Constants.DdfFieldTerminator))
                {
                    dataSize++;
                    var moreData = new char[dataSize];
                    this._data.CopyTo(moreData, 0);

                    if (DdfUtils.ReadObjectsFromBuffer(module.Reader, moreData, dataSize, 1, 1) != 1)
                    {
                        throw new ArgumentException("Data record is short on DDF file.");
                    }

                    this._data = moreData;
                }

                int fieldEntryWidth = ddfLeader.GetFieldEntryWidth();

                /* -------------------------------------------------------------------- */
                /*      Loop over the directory entries, making a pass counting them.   */
                /* -------------------------------------------------------------------- */
                int fieldCount = DdfUtils.CountDirectoryEntries(this._data, 0, dataSize, fieldEntryWidth);

                /* -------------------------------------------------------------------- */
                /*      Allocate, and read field definitions.                           */
                /* -------------------------------------------------------------------- */
                for (int i = 0; i < fieldCount; i++)
                {
                    int entryOffset = i * fieldEntryWidth;

                    /* -------------------------------------------------------------------- */
                    /*      Read the position information and tag.                          */
                    /* -------------------------------------------------------------------- */
                    var tag = new String(this._data.Skip(entryOffset).Take(ddfLeader.SizeFieldTag).ToArray());

                    entryOffset += ddfLeader.SizeFieldTag;
                    int fieldLength = DdfUtils.ReadIntFromBuffer(this._data, entryOffset, ddfLeader.SizeFieldLength);
                    entryOffset += ddfLeader.SizeFieldLength;
                    int fieldPos = DdfUtils.ReadIntFromBuffer(this._data, entryOffset, ddfLeader.SizeFieldPosition);

                    DdfFieldDefinition fieldDefn = module.FieldDefinitions.FirstOrDefault(f => f.Tag == tag);

                    if (fieldDefn == null)
                    {
                        throw new ApplicationException(
                            string.Format("Undefined field {0} encountered in data record.", tag));
                    }

                    if (ddfLeader.FieldAreaStart + fieldPos - Constants.LeaderSize < 0
                        || dataSize - (ddfLeader.FieldAreaStart + fieldPos - Constants.LeaderSize) < fieldLength)
                    {
                        throw new ApplicationException(string.Format("Not enough byte to initialize field {0}.", tag));
                    }

                    /*      Assign info the DDFField.                                       */
                    /* -------------------------------------------------------------------- */
                    this.Fields.Add(
                        new DdfField
                            {
                                FieldDefinition = fieldDefn, 
                                Data = this._data, 
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