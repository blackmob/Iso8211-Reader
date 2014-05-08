// ***********************************************************************
// Assembly         : GreaterHeights.ISO8211
// Author           : Ben Blackmore
// Created          : 05-08-2014
//
// Last Modified By : Ben Blackmore
// Last Modified On : 05-08-2014
// ***********************************************************************
// <copyright file="DDFLeader.cs" company="Greater Heights Ltd">
//     Copyright (c) Greater Heights Ltd. All rights reserved.
// </copyright>
// <summary>The ddf leader.</summary>
// ***********************************************************************

namespace GreaterHeights.ISO8211
{
    using System.Collections.Generic;

    /// <summary>
    /// The ddf leader.
    /// </summary>
    public class DdfLeader
    {
        /// <summary>
        /// Gets or sets the record length.
        /// </summary>
        /// <value>The length of the record.</value>
        public int RecordLength { get; set; }

        /// <summary>
        /// Gets or sets the interchange level.
        /// </summary>
        /// <value>The interchange level.</value>
        public string InterchangeLevel { internal get; set; }

        /// <summary>
        /// Gets or sets the leader identifier.
        /// </summary>
        /// <value>The leader identifier.</value>
        public string LeaderIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the inline code extension indicator.
        /// </summary>
        /// <value>The inline code extension indicator.</value>
        public string InlineCodeExtensionIndicator { internal get; set; }

        /// <summary>
        /// Gets or sets the version number.
        /// </summary>
        /// <value>The version number.</value>
        public string VersionNumber { internal get; set; }

        /// <summary>
        /// Gets or sets the app indicator.
        /// </summary>
        /// <value>The application indicator.</value>
        public string AppIndicator { internal get; set; }

        /// <summary>
        /// Gets or sets the field control length.
        /// </summary>
        /// <value>The length of the field control.</value>
        public int FieldControlLength { get; set; }

        /// <summary>
        /// Gets or sets the field area start.
        /// </summary>
        /// <value>The field area start.</value>
        public int FieldAreaStart { get; set; }

        /// <summary>
        /// Gets or sets the extended char set.
        /// </summary>
        /// <value>The extended character set.</value>
        public IEnumerable<char> ExtendedCharSet { internal get; set; }

        /// <summary>
        /// Gets or sets the size field length.
        /// </summary>
        /// <value>The length of the size field.</value>
        public int SizeFieldLength { get; set; }

        /// <summary>
        /// Gets or sets the size field position.
        /// </summary>
        /// <value>The size field position.</value>
        public int SizeFieldPosition { get; set; }

        /// <summary>
        /// Gets or sets the size field tag.
        /// </summary>
        /// <value>The size field tag.</value>
        public int SizeFieldTag { get; set; }

        /// <summary>
        /// The get field entry width.
        /// </summary>
        /// <returns>The <see cref="int" />.</returns>
        public int GetFieldEntryWidth()
        {
            return this.SizeFieldLength + this.SizeFieldPosition + this.SizeFieldTag;
        }
    }
}