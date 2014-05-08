// ***********************************************************************
// Assembly         : GreaterHeights.ISO8211
// Author           : Ben Blackmore
// Created          : 05-08-2014
//
// Last Modified By : Ben Blackmore
// Last Modified On : 05-08-2014
// ***********************************************************************
// <copyright file="IIso8211Reader.cs" company="Greater Heights Ltd">
//     Copyright (c) Greater Heights Ltd. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace GreaterHeights.ISO8211
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// The Iso8211Reader interface.
    /// </summary>
    public interface IIso8211Reader : IDisposable
    {
        /// <summary>
        /// Gets the field definitions.
        /// </summary>
        /// <value>The field definitions.</value>
        IEnumerable<DdfFieldDefinition> FieldDefinitions { get; }

        /// <summary>
        /// Gets or sets the leader.
        /// </summary>
        /// <value>The leader.</value>
        DdfLeader Leader { get; set; }

        /// <summary>
        /// Gets the reader.
        /// </summary>
        /// <value>The reader.</value>
        BinaryReader Reader { get; }

        /// <summary>
        /// The open.
        /// </summary>
        void Open();

        /// <summary>
        /// The read.
        /// </summary>
        /// <returns>The <see cref="DdfRecord" />.</returns>
        DdfRecord Read();

        /// <summary>
        /// The read field definitions.
        /// </summary>
        /// <param name="ddfLeader">The ddf leader.</param>
        /// <param name="record">The record.</param>
        /// <param name="fieldEntryWidth">The field entry width.</param>
        /// <param name="fieldCount">The field count.</param>
        void ReadFieldDefinitions(DdfLeader ddfLeader, char[] record, int fieldEntryWidth, int fieldCount);

        /// <summary>
        /// The read record.
        /// </summary>
        /// <param name="leader">The leader.</param>
        /// <param name="ddfLeader">The ddf leader.</param>
        /// <returns>The <see cref="char[]" />.</returns>
        char[] ReadRecord(char[] leader, DdfLeader ddfLeader);

        /// <summary>
        /// The leader is valid.
        /// </summary>
        /// <param name="leader">The leader.</param>
        /// <returns>The <see cref="bool" />.</returns>
        bool LeaderIsValid(char[] leader);
    }
}