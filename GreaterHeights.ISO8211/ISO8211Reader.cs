// ***********************************************************************
// Assembly         : GreaterHeights.ISO8211
// Author           : Ben Blackmore
// Created          : 05-08-2014
//
// Last Modified By : Ben Blackmore
// Last Modified On : 05-08-2014
// ***********************************************************************
// <copyright file="ISO8211Reader.cs" company="Greater Heights Ltd">
//     Copyright (c) Greater Heights Ltd. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace GreaterHeights.ISO8211
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// The iso 8211 reader.
    /// </summary>
    public class Iso8211Reader : IIso8211Reader
    {
        /// <summary>
        /// The _field definitions.
        /// </summary>
        private readonly List<DdfFieldDefinition> _fieldDefinitions;

        /// <summary>
        /// The _path.
        /// </summary>
        private readonly string _path;

        /// <summary>
        /// The _reader.
        /// </summary>
        private readonly BinaryReader _reader;

        /// <summary>
        /// Initializes a new instance of the <see cref="Iso8211Reader" /> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">File does not exist</exception>
        public Iso8211Reader(string path)
        {
            if (!File.Exists(path))
            {
                throw new ArgumentOutOfRangeException(path, "File does not exist");
            }

            this._reader = new BinaryReader(new StreamReader(path).BaseStream);
            this._path = path;

            this._fieldDefinitions = new List<DdfFieldDefinition>();
        }

        /// <summary>
        /// Gets the field definitions.
        /// </summary>
        /// <value>The field definitions.</value>
        public IEnumerable<DdfFieldDefinition> FieldDefinitions
        {
            get
            {
                return this._fieldDefinitions;
            }
        }

        /// <summary>
        /// Gets or sets the leader.
        /// </summary>
        /// <value>The leader.</value>
        public DdfLeader Leader { get; set; }

        /// <summary>
        /// Gets the reader.
        /// </summary>
        /// <value>The reader.</value>
        public BinaryReader Reader
        {
            get
            {
                return this._reader;
            }
        }

        /// <summary>
        /// The open.
        /// </summary>
        /// <exception cref="System.ApplicationException">File is invalid</exception>
        public void Open()
        {
            char[] leader = this._reader.ReadChars(24);

            // chek if the leader is valid
            if (!this.LeaderIsValid(leader))
            {
                throw new ApplicationException("File is invalid");
            }

            // populate the leader record 
            this.Leader = DdfUtils.CreateLeader(leader, true);

            // read the record
            char[] record = this.ReadRecord(leader, this.Leader);

            int fieldEntryWidth = this.Leader.GetFieldEntryWidth();

            int fieldCount = DdfUtils.CountDirectoryEntries(
                record, 
                Constants.LeaderSize, 
                this.Leader.RecordLength, 
                fieldEntryWidth);

            this.ReadFieldDefinitions(this.Leader, record, fieldEntryWidth, fieldCount);
        }

        /// <summary>
        /// The read.
        /// </summary>
        /// <returns>The <see cref="DdfRecord" />.</returns>
        public DdfRecord Read()
        {
            var record = new DdfRecord();

            return record.Read(this) ? record : null;
        }

        /// <summary>
        /// The read field definitions.
        /// </summary>
        /// <param name="ddfLeader">The ddf leader.</param>
        /// <param name="record">The record.</param>
        /// <param name="fieldEntryWidth">The field entry width.</param>
        /// <param name="fieldCount">The field count.</param>
        /// <exception cref="System.ApplicationException">Header record invalid on DDF file  + this._path</exception>
        public void ReadFieldDefinitions(DdfLeader ddfLeader, char[] record, int fieldEntryWidth, int fieldCount)
        {
            for (int i = 0; i < fieldCount; i++)
            {
                int entryOffset = Constants.LeaderSize + i * fieldEntryWidth;

                var tag = new String(record.Skip(entryOffset).Take(ddfLeader.SizeFieldTag).ToArray());

                entryOffset += ddfLeader.SizeFieldTag;
                int fieldLength = DdfUtils.ReadIntFromBuffer(record, entryOffset, ddfLeader.SizeFieldLength);

                entryOffset += ddfLeader.SizeFieldLength;
                int fieldPos = DdfUtils.ReadIntFromBuffer(record, entryOffset, ddfLeader.SizeFieldPosition);

                if (ddfLeader.FieldAreaStart + fieldPos < 0
                    || ddfLeader.RecordLength - (ddfLeader.FieldAreaStart + fieldPos) < fieldLength)
                {
                    throw new ApplicationException("Header record invalid on DDF file " + this._path);
                }

                var fieldDef = new DdfFieldDefinition();
                if (fieldDef.Initialize(ddfLeader, tag, fieldLength, record, ddfLeader.FieldAreaStart + fieldPos))
                {
                    this._fieldDefinitions.Add(fieldDef);
                }
            }
        }

        /// <summary>
        /// The read record.
        /// </summary>
        /// <param name="leader">The leader.</param>
        /// <param name="ddfLeader">The ddf leader.</param>
        /// <returns>The <see cref="char[]" />.</returns>
        public char[] ReadRecord(char[] leader, DdfLeader ddfLeader)
        {
            var record = new char[ddfLeader.RecordLength];
            leader.CopyTo(record, 0);

            for (int i = 0; i < (ddfLeader.RecordLength - Constants.LeaderSize); i++)
            {
                byte[] entry = this._reader.ReadBytes(1);

                char[] charEntry = Encoding.UTF8.GetString(entry).ToCharArray();

                charEntry.CopyTo(record, (charEntry.Length * i) + Constants.LeaderSize);
            }

            return record;
        }

        /// <summary>
        /// The leader is valid.
        /// </summary>
        /// <param name="leader">The leader.</param>
        /// <returns>The <see cref="bool" />.</returns>
        public bool LeaderIsValid(char[] leader)
        {
            // Check the leader is valid.
            for (int i = 0; i < Constants.LeaderSize; i++)
            {
                if (leader[i] < 32 || leader[i] > 126)
                {
                    return false;
                }
            }

            if (leader[5] != '1' && leader[5] != '2' && leader[5] != '3')
            {
                return false;
            }

            if (leader[6] != 'L')
            {
                return false;
            }

            if (leader[8] != '1' && leader[8] != ' ')
            {
                return false;
            }

            return true;
        }

        #region IDisposable Members

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            if (this._reader != null)
            {
                this._reader.Close();
                this._reader.Dispose();
            }
        }

        #endregion
    }
}