// ***********************************************************************
// Assembly         : GreaterHeights.ISO8211
// Author           : Ben Blackmore
// Created          : 05-08-2014
//
// Last Modified By : Ben Blackmore
// Last Modified On : 05-08-2014
// ***********************************************************************
// <copyright file="DDFField.cs" company="Greater Heights Ltd">
//     Copyright (c) Greater Heights Ltd. All rights reserved.
// </copyright>
// <summary>The ddf field.</summary>
// ***********************************************************************

namespace GreaterHeights.ISO8211
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The ddf field.
    /// </summary>
    public class DdfField
    {
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public char[] Data { private get; set; }

        /// <summary>
        /// Gets or sets the data size.
        /// </summary>
        /// <value>The size of the data.</value>
        public int DataSize { private get; set; }

        /// <summary>
        /// Gets or sets the field definition.
        /// </summary>
        /// <value>The field definition.</value>
        public DdfFieldDefinition FieldDefinition { get; set; }

        /// <summary>
        /// Gets or sets the offset.
        /// </summary>
        /// <value>The offset.</value>
        public int Offset { private get; set; }

        /// <summary>
        /// The get repeat count.
        /// </summary>
        /// <returns>The <see cref="int" />.</returns>
        private static int GetRepeatCount()
        {
            // the s-57 catalog.031 does not seem to have repeating fields in records so we just return 1
            return 1;
        }

        /// <summary>
        /// The get record.
        /// </summary>
        /// <returns>The <see cref="Dictionary" />.</returns>
        public Dictionary<string, SubFieldData> GetRecord()
        {
            var retVal = new Dictionary<string, SubFieldData>();

            int bytesRemaining = this.DataSize;

            for (int i = 0; i < GetRepeatCount(); i++)
            {
                int position = this.Offset;
                foreach (DdfSubFieldDefinition subField in this.FieldDefinition.SubFieldDefinitions)
                {
                    int consumed;
                    string data = subField.GetData(this.Data.Skip(position).ToArray(), out consumed, bytesRemaining);
                    position += consumed;
                    bytesRemaining -= consumed;
                    retVal.Add(
                        subField.Name, 
                        new SubFieldData { DataType = subField.DataType, Name = subField.Name, Value = data });
                }
            }

            return retVal;
        }
    }
}