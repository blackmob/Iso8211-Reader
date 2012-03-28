using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace GreaterHeights.ISO8211
{
    public static class DdfUtils
    {
        public static int ReadIntFromBuffer(char[] buffer, int offset, int maxChars)
        {
            var stringBuffer = new string(buffer);

            string stringToConvert = stringBuffer.Substring(offset, maxChars);

            return Int32.Parse(stringToConvert);
        }

        public static string ReadStringFromBufffer(char[] record, int offset, int maxChars,
                                                   int firstDelimiter, int secondDelimiter,
                                                   out int consumedChars)
        {
            int i;

            for (i = 0;
                 i < maxChars - 1 && record[offset + i] != firstDelimiter
                 && record[offset + i] != secondDelimiter;
                 i++)
            {
            }

            consumedChars = i;
            if (i < maxChars
                && (record[offset + i] == firstDelimiter || record[offset + i] == secondDelimiter))
                consumedChars++;

            if (i == 0)
                return "";

            return new String(record.Skip(offset).Take(i).Select(c => c).ToArray());
        }


        public static int ReadObjectsFromBuffer(BinaryReader reader, char[] buffer, int bufferOffset,
                                                int sizeOfObjectInBytes, int numberToread)
        {
            int i;

            for (i = 0; i < numberToread; i++)
            {
                var entry = reader.ReadBytes(sizeOfObjectInBytes);

                var charEntry = Encoding.UTF8.GetString(entry).ToCharArray();

                charEntry.CopyTo(buffer, (charEntry.Length*i) + bufferOffset);
            }

            return i*sizeOfObjectInBytes;
        }

        public static int CountDirectoryEntries(char[] record, int offset, int dataSize, int fieldEntryWidth)
        {
            var fieldCount = 0;

            for (var i = offset; i < dataSize; i += fieldEntryWidth)
            {
                if (record[i] == Constants.DdfFieldTerminator)
                    break;

                fieldCount++;
            }
            return fieldCount;
        }

        public static DdfLeader CreateLeader(char[] leader, bool moduleLeader)
        {
            DdfLeader ddfLeader;

            if (moduleLeader)
            {
                ddfLeader = new DdfLeader
                                {
                                    RecordLength = ReadIntFromBuffer(leader, 0, 5),
                                    InterchangeLevel = leader[5].ToString(CultureInfo.InvariantCulture),
                                    LeaderIdentifier = leader[6].ToString(CultureInfo.InvariantCulture),
                                    InlineCodeExtensionIndicator = leader[7].ToString(CultureInfo.InvariantCulture),
                                    VersionNumber = leader[8].ToString(CultureInfo.InvariantCulture),
                                    AppIndicator = leader[9].ToString(CultureInfo.InvariantCulture),
                                    FieldControlLength = ReadIntFromBuffer(leader, 10, 2),
                                    FieldAreaStart = ReadIntFromBuffer(leader, 12, 5),
                                    ExtendedCharSet = new List<char> {leader[17], leader[18], leader[19], '\0'},
                                    SizeFieldLength = ReadIntFromBuffer(leader, 20, 1),
                                    SizeFieldPosition = ReadIntFromBuffer(leader, 21, 1),
                                    SizeFieldTag = ReadIntFromBuffer(leader, 23, 1)
                                };

                if (ddfLeader.RecordLength < 12 || ddfLeader.FieldControlLength == 0
                    || ddfLeader.FieldAreaStart < 24 || ddfLeader.SizeFieldLength == 0
                    || ddfLeader.SizeFieldPosition == 0 || ddfLeader.SizeFieldTag == 0)
                {
                    throw new ApplicationException("File is invalid");
                }
            }
            else
            {
                ddfLeader = new DdfLeader
                                {
                                    RecordLength = ReadIntFromBuffer(leader, 0, 5),
                                    LeaderIdentifier = leader[6].ToString(CultureInfo.InvariantCulture),
                                    FieldAreaStart = ReadIntFromBuffer(leader, 12, 5),
                                    SizeFieldLength = ReadIntFromBuffer(leader, 20, 1),
                                    SizeFieldPosition = ReadIntFromBuffer(leader, 21, 1),
                                    SizeFieldTag = ReadIntFromBuffer(leader, 23, 1)
                                };

                if (ddfLeader.SizeFieldLength < 0 || ddfLeader.SizeFieldLength > 9
                    || ddfLeader.SizeFieldPosition < 0 || ddfLeader.SizeFieldPosition > 9
                    || ddfLeader.SizeFieldTag < 0 || ddfLeader.SizeFieldTag > 9)
                {
                    throw new ApplicationException("ISO8211 record leader appears to be corrupt.");
                }
            }

            return ddfLeader;
        }
    }
}