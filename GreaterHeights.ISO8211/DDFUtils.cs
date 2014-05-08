// ***********************************************************************
// Assembly         : GreaterHeights.ISO8211
// Author           : Ben Blackmore
// Created          : 05-08-2014
//
// Last Modified By : Ben Blackmore
// Last Modified On : 05-08-2014
// ***********************************************************************
// <copyright file="DDFUtils.cs" company="Greater Heights Ltd">
//     Copyright (c) Greater Heights Ltd. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace GreaterHeights.ISO8211
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// The ddf utils.
    /// </summary>
    public static class DdfUtils
    {
        /// <summary>
        /// The read int from buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="maxChars">The max chars.</param>
        /// <returns>The <see cref="int" />.</returns>
        public static int ReadIntFromBuffer(char[] buffer, int offset, int maxChars)
        {
            var stringBuffer = new string(buffer);

            string stringToConvert = stringBuffer.Substring(offset, maxChars);

            return int.Parse(stringToConvert);
        }

        /// <summary>
        /// The read string from bufffer.
        /// </summary>
        /// <param name="record">The record.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="maxChars">The max chars.</param>
        /// <param name="firstDelimiter">The first delimiter.</param>
        /// <param name="secondDelimiter">The second delimiter.</param>
        /// <param name="consumedChars">The consumed chars.</param>
        /// <returns>The <see cref="string" />.</returns>
        public static string ReadStringFromBufffer(
            char[] record, 
            int offset, 
            int maxChars, 
            int firstDelimiter, 
            int secondDelimiter, 
            out int consumedChars)
        {
            int i;

            for (i = 0;
                 i < maxChars - 1 && record[offset + i] != firstDelimiter && record[offset + i] != secondDelimiter;
                 i++)
            {
            }

            consumedChars = i;
            if (i < maxChars && (record[offset + i] == firstDelimiter || record[offset + i] == secondDelimiter))
            {
                consumedChars++;
            }

            if (i == 0)
            {
                return string.Empty;
            }

            return new String(record.Skip(offset).Take(i).Select(c => c).ToArray());
        }

        /// <summary>
        /// The read objects from buffer.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="bufferOffset">The buffer offset.</param>
        /// <param name="sizeOfObjectInBytes">The size of object in bytes.</param>
        /// <param name="numberToread">The number toread.</param>
        /// <returns>The <see cref="int" />.</returns>
        public static int ReadObjectsFromBuffer(
            BinaryReader reader, 
            char[] buffer, 
            int bufferOffset, 
            int sizeOfObjectInBytes, 
            int numberToread)
        {
            int i;

            for (i = 0; i < numberToread; i++)
            {
                byte[] entry = reader.ReadBytes(sizeOfObjectInBytes);

                char[] charEntry = Encoding.UTF8.GetString(entry).ToCharArray();

                charEntry.CopyTo(buffer, (charEntry.Length * i) + bufferOffset);
            }

            return i * sizeOfObjectInBytes;
        }

        /// <summary>
        /// The count directory entries.
        /// </summary>
        /// <param name="record">The record.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="dataSize">The data size.</param>
        /// <param name="fieldEntryWidth">The field entry width.</param>
        /// <returns>The <see cref="int" />.</returns>
        public static int CountDirectoryEntries(char[] record, int offset, int dataSize, int fieldEntryWidth)
        {
            int fieldCount = 0;

            for (int i = offset; i < dataSize; i += fieldEntryWidth)
            {
                if (record[i] == Constants.DdfFieldTerminator)
                {
                    break;
                }

                fieldCount++;
            }

            return fieldCount;
        }

        /// <summary>
        /// The create leader.
        /// </summary>
        /// <param name="leader">The leader.</param>
        /// <param name="moduleLeader">The module leader.</param>
        /// <returns>The <see cref="DdfLeader" />.</returns>
        /// <exception cref="System.ApplicationException">
        /// File is invalid
        /// or
        /// ISO8211 record leader appears to be corrupt.
        /// </exception>
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
                                    InlineCodeExtensionIndicator =
                                        leader[7].ToString(CultureInfo.InvariantCulture), 
                                    VersionNumber = leader[8].ToString(CultureInfo.InvariantCulture), 
                                    AppIndicator = leader[9].ToString(CultureInfo.InvariantCulture), 
                                    FieldControlLength = ReadIntFromBuffer(leader, 10, 2), 
                                    FieldAreaStart = ReadIntFromBuffer(leader, 12, 5), 
                                    ExtendedCharSet =
                                        new List<char> { leader[17], leader[18], leader[19], '\0' }, 
                                    SizeFieldLength = ReadIntFromBuffer(leader, 20, 1), 
                                    SizeFieldPosition = ReadIntFromBuffer(leader, 21, 1), 
                                    SizeFieldTag = ReadIntFromBuffer(leader, 23, 1)
                                };

                if (ddfLeader.RecordLength < 12 || ddfLeader.FieldControlLength == 0 || ddfLeader.FieldAreaStart < 24
                    || ddfLeader.SizeFieldLength == 0 || ddfLeader.SizeFieldPosition == 0 || ddfLeader.SizeFieldTag == 0)
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

                if (ddfLeader.SizeFieldLength < 0 || ddfLeader.SizeFieldLength > 9 || ddfLeader.SizeFieldPosition < 0
                    || ddfLeader.SizeFieldPosition > 9 || ddfLeader.SizeFieldTag < 0 || ddfLeader.SizeFieldTag > 9)
                {
                    throw new ApplicationException("ISO8211 record leader appears to be corrupt.");
                }
            }

            return ddfLeader;
        }
    }
}