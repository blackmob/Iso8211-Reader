// ***********************************************************************
// Assembly         : GreaterHeights.ISO8211
// Author           : Ben Blackmore
// Created          : 05-08-2014
//
// Last Modified By : Ben Blackmore
// Last Modified On : 05-08-2014
// ***********************************************************************
// <copyright file="DDFSubFieldDefinition.cs" company="Greater Heights Ltd">
//     Copyright (c) Greater Heights Ltd. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace GreaterHeights.ISO8211
{
    using System;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// The ddf sub field definition.
    /// </summary>
    public class DdfSubFieldDefinition
    {
        #region DdfDataType enum

        /// <summary>
        /// The ddf data type.
        /// </summary>
        public enum DdfDataType
        {
            /// <summary>
            /// The ddf int.
            /// </summary>
            DdfInt,

            /// <summary>
            /// The ddf float.
            /// </summary>
            DdfFloat,

            /// <summary>
            /// The ddf string.
            /// </summary>
            DdfString,

            /// <summary>
            /// The ddf binary string.
            /// </summary>
            DdfBinaryString
        };

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="DdfSubFieldDefinition" /> class.
        /// </summary>
        public DdfSubFieldDefinition()
        {
            this.FormatWidth = 0;
            this.DataType = DdfDataType.DdfString;
            this.IsVariable = true;
            this.FormatDelimeter = Constants.DdfUnitTerminator;
            this.BinaryFormat = DdfBinaryFormat.NotBinary;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets the format width.
        /// </summary>
        /// <value>The width of the format.</value>
        public int FormatWidth { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether is variable.
        /// </summary>
        /// <value><c>true</c> if this instance is variable; otherwise, <c>false</c>.</value>
        private bool IsVariable { get; set; }

        /// <summary>
        /// Gets the data type.
        /// </summary>
        /// <value>The type of the data.</value>
        public DdfDataType DataType { get; private set; }

        /// <summary>
        /// Gets or sets the format delimeter.
        /// </summary>
        /// <value>The format delimeter.</value>
        private int FormatDelimeter { get; set; }

        /// <summary>
        /// Gets or sets the binary format.
        /// </summary>
        /// <value>The binary format.</value>
        private DdfBinaryFormat BinaryFormat { get; set; }

        /// <summary>
        /// Gets or sets the format string.
        /// </summary>
        /// <value>The format string.</value>
        private string FormatString { get; set; }

        /// <summary>
        /// The set format.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <exception cref="System.ApplicationException">
        /// Format type of  + format[0] +  not supported.
        /// or
        /// Format type of  + format[0] +  not recognised.
        /// </exception>
        public void SetFormat(string format)
        {
            this.FormatString = format;

            if (format.Contains('('))
            {
                this.FormatWidth = int.Parse(new string(format.Where(char.IsDigit).ToArray()));
                this.IsVariable = this.FormatWidth == 0;
            }
            else
            {
                this.IsVariable = true;
            }

            /* -------------------------------------------------------------------- */
            /*      Interpret the format string.                                    */
            /* -------------------------------------------------------------------- */
            switch (format[0])
            {
                case 'A':
                case 'C': // It isn't clear to me how this is different than 'A'
                    this.DataType = DdfDataType.DdfString;
                    break;

                case 'R':
                    this.DataType = DdfDataType.DdfFloat;
                    break;

                case 'I':
                case 'S':
                    this.DataType = DdfDataType.DdfInt;
                    break;

                case 'B':
                case 'b':

                    // Is the width expressed in bits? (is it a bitstring)
                    this.IsVariable = false;
                    if (format[1] == '(')
                    {
                        this.FormatWidth = int.Parse(new string(format.Where(char.IsDigit).ToArray())) / 8;
                        this.BinaryFormat = DdfBinaryFormat.SInt; // good default, works for SDTS.

                        this.DataType = this.FormatWidth < 5 ? DdfDataType.DdfInt : DdfDataType.DdfBinaryString;
                    }

                        // or do we have a binary type indicator? (is it binary)
                    else
                    {
                        this.BinaryFormat = (DdfBinaryFormat)(format[1] - '0');
                        this.FormatWidth = int.Parse(new string(format.Where(char.IsDigit).ToArray()));

                        if (this.BinaryFormat == DdfBinaryFormat.SInt || this.BinaryFormat == DdfBinaryFormat.UInt)
                        {
                            this.DataType = DdfDataType.DdfInt;
                        }
                        else
                        {
                            this.DataType = DdfDataType.DdfFloat;
                        }
                    }

                    break;

                case 'X':

                    // 'X' is extra space, and shouldn't be directly assigned to a
                    // subfield ... I haven't encountered it in use yet though.
                    throw new ApplicationException("Format type of " + format[0] + " not supported.");

                default:
                    throw new ApplicationException("Format type of " + format[0] + " not recognised.");
            }
        }

        /// <summary>
        /// The get data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="consumed">The consumed.</param>
        /// <param name="bytesRemaining">The bytes remaining.</param>
        /// <returns>The <see cref="string" />.</returns>
        public string GetData(char[] data, out int consumed, int bytesRemaining)
        {
            switch (this.DataType)
            {
                case DdfDataType.DdfInt:
                    return
                        int.Parse(this.ExtractStringData(data, bytesRemaining, out consumed))
                            .ToString(CultureInfo.InvariantCulture);
                case DdfDataType.DdfFloat:
                    float retVal;
                    float.TryParse(this.ExtractStringData(data, bytesRemaining, out consumed), out retVal);
                    return retVal.ToString(CultureInfo.InvariantCulture);
                case DdfDataType.DdfString:
                    return this.ExtractStringData(data, bytesRemaining, out consumed);
                case DdfDataType.DdfBinaryString:
                    return this.ExtractStringData(data, bytesRemaining, out consumed);
                default:
                    consumed = 0;
                    return string.Empty;
            }
        }

        /// <summary>
        /// The extract string data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="maxBytes">The max bytes.</param>
        /// <param name="consumed">The consumed.</param>
        /// <returns>The <see cref="string" />.</returns>
        private string ExtractStringData(char[] data, int maxBytes, out int consumed)
        {
            int length = this.GetDataLength(data, maxBytes, out consumed);

            char[] chunk = data.Take(length).ToArray().Any() ? data.Take(length).ToArray() : null;

            return new string(chunk);
        }

        /// <summary>
        /// The get data length.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="maxBytes">The max bytes.</param>
        /// <param name="consumed">The consumed.</param>
        /// <returns>The <see cref="int" />.</returns>
        private int GetDataLength(char[] data, int maxBytes, out int consumed)
        {
            if (!this.IsVariable)
            {
                if (this.FormatWidth > maxBytes)
                {
                    Console.Write(
                        "Only {0} bytes available for subfield {1} with format string {2} ... returning shortened data.", 
                        maxBytes, 
                        this.Name, 
                        this.FormatString);

                    consumed = maxBytes;

                    return maxBytes;
                }

                return consumed = this.FormatWidth;
            }

            int length = 0;
            bool isAsciiField = true;
            int extraConsumedBytes = 0;

            /* We only check for the field terminator because of some buggy 
                 * datasets with missing format terminators.  However, we have found
                 * the field terminator and unit terminators are legal characters 
                 * within the fields of some extended datasets (such as JP34NC94.000).
                 * So we don't check for the field terminator and unit terminators as 
                 * a single byte if the field appears to be multi-byte which we 
                 * establish by checking for the buffer ending with 0x1e 0x00 (a
                 * two byte field terminator). 
                 *
                 * In the case of S57, the subfield ATVL of the NATF field can be 
                 * encoded in lexical level 2 (see S57 specification, Edition 3.1, 
                 * paragraph 2.4 and 2.5). In that case the Unit Terminator and Field 
                 * Terminator are followed by the NULL character.
                 * A better fix would be to read the NALL tag in the DSSI to check 
                 * that the lexical level is 2, instead of relying on the value of 
                 * the first byte as we are doing - but that is not information
                 * that is available at the libiso8211 level (bug #1526)
                 */

            // If the whole field ends with 0x1e 0x00 then we assume this
            // field is a double byte character set.
            if (maxBytes > 1
                && (data[maxBytes - 2] == this.FormatDelimeter || data[maxBytes - 2] == Constants.DdfFieldTerminator)
                && data[maxBytes - 1] == 0x00)
            {
                isAsciiField = false;
            }

            while (length < maxBytes)
            {
                if (isAsciiField)
                {
                    if (data[length] == this.FormatDelimeter || data[length] == Constants.DdfFieldTerminator)
                    {
                        break;
                    }
                }
                else
                {
                    if (length > 0
                        && (data[length - 1] == this.FormatDelimeter || data[length - 1] == Constants.DdfFieldTerminator)
                        && data[length] == 0)
                    {
                        // Suck up the field terminator if one follows
                        // or else it will be interpreted as a new subfield.
                        // This is a pretty ugly counter-intuitive hack!
                        if (length + 1 < maxBytes && data[length + 1] == Constants.DdfFieldTerminator)
                        {
                            extraConsumedBytes++;
                        }

                        break;
                    }
                }

                length++;
            }

            if (maxBytes == 0)
            {
                consumed = length + extraConsumedBytes;
            }
            else
            {
                consumed = length + extraConsumedBytes + 1;
            }

            return length;
        }

        #region Nested type: DdfBinaryFormat

        /// <summary>
        /// The ddf binary format.
        /// </summary>
        private enum DdfBinaryFormat
        {
            /// <summary>
            /// The not binary.
            /// </summary>
            NotBinary = 0,

            /// <summary>
            /// The u int.
            /// </summary>
            UInt = 1,

            /// <summary>
            /// The s int.
            /// </summary>
            SInt = 2
        };

        #endregion
    }
}