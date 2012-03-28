using System;
using System.Globalization;
using System.Linq;

namespace GreaterHeights.ISO8211
{
    public class DdfSubFieldDefinition
    {
        #region DdfDataType enum

        public enum DdfDataType
        {
            DdfInt,
            DdfFloat,
            DdfString,
            DdfBinaryString
        };

        #endregion

        public DdfSubFieldDefinition()
        {
            FormatWidth = 0;
            DataType = DdfDataType.DdfString;
            IsVariable = true;
            FormatDelimeter = Constants.DdfUnitTerminator;
            BinaryFormat = DdfBinaryFormat.NotBinary;
        }

        public string Name { get; set; }
        public int FormatWidth { get; private set; }
        private bool IsVariable { get; set; }
        public DdfDataType DataType { get; private set; }
        private int FormatDelimeter { get; set; }
        private DdfBinaryFormat BinaryFormat { get; set; }
        private string FormatString { get; set; }

        public void SetFormat(string format)
        {
            FormatString = format;

            if (format.Contains('('))
            {
                FormatWidth = Int32.Parse(new string(format.Where(char.IsDigit).ToArray()));
                IsVariable = FormatWidth == 0;
            }
            else
            {
                IsVariable = true;
            }

            /* -------------------------------------------------------------------- */
            /*      Interpret the format string.                                    */
            /* -------------------------------------------------------------------- */
            switch (format[0])
            {
                case 'A':
                case 'C': // It isn't clear to me how this is different than 'A'
                    DataType = DdfDataType.DdfString;
                    break;

                case 'R':
                    DataType = DdfDataType.DdfFloat;
                    break;

                case 'I':
                case 'S':
                    DataType = DdfDataType.DdfInt;
                    break;

                case 'B':
                case 'b':
                    // Is the width expressed in bits? (is it a bitstring)
                    IsVariable = false;
                    if (format[1] == '(')
                    {
                        FormatWidth = Int32.Parse(new string(format.Where(char.IsDigit).ToArray()))/8;
                        BinaryFormat = DdfBinaryFormat.SInt; // good default, works for SDTS.

                        DataType = FormatWidth < 5 ? DdfDataType.DdfInt : DdfDataType.DdfBinaryString;
                    }

                        // or do we have a binary type indicator? (is it binary)
                    else
                    {
                        BinaryFormat = (DdfBinaryFormat) (format[1] - '0');
                        FormatWidth = Int32.Parse(new string(format.Where(char.IsDigit).ToArray()));

                        if (BinaryFormat == DdfBinaryFormat.SInt || BinaryFormat == DdfBinaryFormat.UInt)
                            DataType = DdfDataType.DdfInt;
                        else
                            DataType = DdfDataType.DdfFloat;
                    }
                    break;

                case 'X':
                    // 'X' is extra space, and shouldn't be directly assigned to a
                    // subfield ... I haven't encountered it in use yet though.
                    throw new ApplicationException(
                        "Format type of " + format[0] + " not supported.");

                default:
                    throw new ApplicationException(
                        "Format type of " + format[0] + " not recognised.");
            }
        }

        public string GetData(char[] data, out int consumed, int bytesRemaining)
        {
            switch (DataType)
            {
                case DdfDataType.DdfInt:
                    return
                        Int32.Parse(ExtractStringData(data, bytesRemaining, out consumed)).ToString(
                            CultureInfo.InvariantCulture);
                case DdfDataType.DdfFloat:
                    float retVal;
                    float.TryParse(ExtractStringData(data, bytesRemaining, out consumed), out retVal);
                    return retVal.ToString(CultureInfo.InvariantCulture);
                case DdfDataType.DdfString:
                    return ExtractStringData(data, bytesRemaining, out consumed);
                case DdfDataType.DdfBinaryString:
                    return ExtractStringData(data, bytesRemaining, out consumed);
                default:
                    consumed = 0;
                    return "";
            }
        }

        private string ExtractStringData(char[] data, int maxBytes, out int consumed)
        {
            int length = GetDataLength(data, maxBytes, out consumed);

            char[] chunk = data.Take(length).ToArray().Any() ? data.Take(length).ToArray() : null;

            return new string(chunk);
        }

        private int GetDataLength(char[] data, int maxBytes, out int consumed)
        {
            if (!IsVariable)
            {
                if (FormatWidth > maxBytes)
                {
                    Console.Write(
                        "Only {0} bytes available for subfield {1} with format string {2} ... returning shortened data.",
                        maxBytes, Name, FormatString);

                    consumed = maxBytes;

                    return maxBytes;
                }
                return consumed = FormatWidth;
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
                && (data[maxBytes - 2] == FormatDelimeter
                    || data[maxBytes - 2] == Constants.DdfFieldTerminator)
                && data[maxBytes - 1] == 0x00)
                isAsciiField = false;

            while (length < maxBytes)
            {
                if (isAsciiField)
                {
                    if (data[length] == FormatDelimeter ||
                        data[length] == Constants.DdfFieldTerminator)
                        break;
                }
                else
                {
                    if (length > 0
                        && (data[length - 1] == FormatDelimeter
                            || data[length - 1] == Constants.DdfFieldTerminator)
                        && data[length] == 0)
                    {
                        // Suck up the field terminator if one follows
                        // or else it will be interpreted as a new subfield.
                        // This is a pretty ugly counter-intuitive hack!
                        if (length + 1 < maxBytes &&
                            data[length + 1] == Constants.DdfFieldTerminator)
                            extraConsumedBytes++;
                        break;
                    }
                }

                length++;
            }


            if (maxBytes == 0)
                consumed = length + extraConsumedBytes;
            else
                consumed = length + extraConsumedBytes + 1;

            return length;
        }

        #region Nested type: DdfBinaryFormat

        private enum DdfBinaryFormat
        {
            NotBinary = 0,
            UInt = 1,
            SInt = 2
        };

        #endregion
    }
}