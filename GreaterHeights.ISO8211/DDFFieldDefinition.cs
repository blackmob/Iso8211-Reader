// ***********************************************************************
// Assembly         : GreaterHeights.ISO8211
// Author           : Ben Blackmore
// Created          : 05-08-2014
//
// Last Modified By : Ben Blackmore
// Last Modified On : 05-08-2014
// ***********************************************************************
// <copyright file="DDFFieldDefinition.cs" company="Greater Heights Ltd">
//     Copyright (c) Greater Heights Ltd. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace GreaterHeights.ISO8211
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// The ddf field definition.
    /// </summary>
    public class DdfFieldDefinition
    {
        /// <summary>
        /// The _array description.
        /// </summary>
        private string _arrayDescription;

        /// <summary>
        /// The _data structure code.
        /// </summary>
        private DdfDataStructCode _dataStructureCode;

        /// <summary>
        /// The _format controls.
        /// </summary>
        private string _formatControls;

        /// <summary>
        /// Initializes a new instance of the <see cref="DdfFieldDefinition" /> class.
        /// </summary>
        public DdfFieldDefinition()
        {
            this.SubFieldDefinitions = new List<DdfSubFieldDefinition>();
            this.RepeatingSubfields = false;
        }

        /// <summary>
        /// Gets the sub field definitions.
        /// </summary>
        /// <value>The sub field definitions.</value>
        public List<DdfSubFieldDefinition> SubFieldDefinitions { get; private set; }

        /// <summary>
        /// Gets or sets the fixed width.
        /// </summary>
        /// <value>The width of the fixed.</value>
        private int FixedWidth { get; set; }

        /// <summary>
        /// Gets the field name.
        /// </summary>
        /// <value>The name of the field.</value>
        public string FieldName { get; private set; }

        /// <summary>
        /// Gets the tag.
        /// </summary>
        /// <value>The tag.</value>
        public string Tag { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether repeating subfields.
        /// </summary>
        /// <value><c>true</c> if [repeating subfields]; otherwise, <c>false</c>.</value>
        private bool RepeatingSubfields { get; set; }

        /// <summary>
        /// The initialize.
        /// </summary>
        /// <param name="leader">The leader.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="size">The size.</param>
        /// <param name="record">The record.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>The <see cref="bool" />.</returns>
        /// <exception cref="System.ArgumentNullException">leader</exception>
        /// <exception cref="System.ApplicationException">
        /// Unrecognised data_struct_code value  + record[offset] +  Field  + tag
        ///                         +  initialization incorrect.
        /// or
        /// Unrecognised data_type_code value  + record[offset + 1] +  Field  + tag
        ///                         + initialization incorrect.
        /// </exception>
        public bool Initialize(DdfLeader leader, string tag, int size, char[] record, int offset)
        {
            if (leader == null)
            {
                throw new ArgumentNullException("leader");
            }

            this.Tag = tag;

            int charsConsumed;

            /* -------------------------------------------------------------------- */
            /*      Set the data struct and type codes.                             */
            /* -------------------------------------------------------------------- */
            switch (record[offset])
            {
                case ' ': /* for ADRG, DIGEST USRP, DIGEST ASRP files */
                case '0':
                    this._dataStructureCode = DdfDataStructCode.DscElementary;
                    break;
                case '1':
                    this._dataStructureCode = DdfDataStructCode.DscVector;
                    break;
                case '2':
                    this._dataStructureCode = DdfDataStructCode.DscArray;
                    break;
                case '3':
                    this._dataStructureCode = DdfDataStructCode.DscConcatenated;
                    break;
                default:
                    throw new ApplicationException(
                        "Unrecognised data_struct_code value " + record[offset] + " Field " + tag
                        + " initialization incorrect.");

                    // dataStructureCode = DDF_data_struct_code.dsc_elementary;
            }

            switch (record[offset + 1])
            {
                case ' ': /* for ADRG, DIGEST USRP, DIGEST ASRP files */
                case '0':
                    break;

                case '1':
                    break;

                case '2':
                    break;

                case '3':
                    break;

                case '4':
                    break;

                case '5':
                    break;

                case '6':
                    break;

                default:
                    throw new ApplicationException(
                        "Unrecognised data_type_code value " + record[offset + 1] + " Field " + tag
                        + "initialization incorrect.");

                    // dataTypeCode = dtc_char_string;
            }

            /* -------------------------------------------------------------------- */
            /*      Capture the field name, description (sub field names), and      */
            /*      format statements.                                              */
            /* -------------------------------------------------------------------- */
            int fieldDataOffset = leader.FieldControlLength;

            this.FieldName = DdfUtils.ReadStringFromBufffer(
                record, 
                offset + fieldDataOffset, 
                size - fieldDataOffset, 
                Constants.DdfUnitTerminator, 
                Constants.DdfFieldTerminator, 
                out charsConsumed);

            fieldDataOffset += charsConsumed;

            this._arrayDescription = DdfUtils.ReadStringFromBufffer(
                record, 
                offset + fieldDataOffset, 
                size - fieldDataOffset, 
                Constants.DdfUnitTerminator, 
                Constants.DdfFieldTerminator, 
                out charsConsumed);

            fieldDataOffset += charsConsumed;

            this._formatControls = DdfUtils.ReadStringFromBufffer(
                record, 
                offset + fieldDataOffset, 
                size - fieldDataOffset, 
                Constants.DdfUnitTerminator, 
                Constants.DdfFieldTerminator, 
                out charsConsumed);

            if (this._dataStructureCode != DdfDataStructCode.DscElementary)
            {
                if (!this.BuildSubfields())
                {
                    return false;
                }

                if (!this.ApplyFormats())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The apply formats.
        /// </summary>
        /// <returns>The <see cref="bool" />.</returns>
        /// <exception cref="System.ApplicationException">
        /// Format controls missing brackets
        /// or
        /// or
        /// got less formats than subfields for field  + this.FieldName
        /// </exception>
        private bool ApplyFormats()
        {
            /* -------------------------------------------------------------------- */
            /*      Verify that the format string is contained within brackets.     */
            /* -------------------------------------------------------------------- */
            if (this._formatControls.Length < 2 || this._formatControls.First() != '('
                || this._formatControls.Last() != ')')
            {
                throw new ApplicationException("Format controls missing brackets");
            }

            // remove brackets and expand format
            List<string> formatList = this.ExpandFormat(this._formatControls).Split(',').ToList();

            if (formatList.Count > this.SubFieldDefinitions.Count)
            {
                throw new ApplicationException(
                    string.Format("Got more formats than subfields for field {0}", this.FieldName));
            }

            int index = 0;

            foreach (DdfSubFieldDefinition subFieldDefintion in this.SubFieldDefinitions)
            {
                subFieldDefintion.SetFormat(formatList[index]);
                index++;
            }

            if (index < this.SubFieldDefinitions.Count)
            {
                throw new ApplicationException("got less formats than subfields for field " + this.FieldName);
            }

            foreach (DdfSubFieldDefinition subField in this.SubFieldDefinitions)
            {
                if (subField.FormatWidth == 0)
                {
                    this.FixedWidth = 0;
                    break;
                }

                this.FixedWidth += subField.FormatWidth;
            }

            return true;
        }

        /// <summary>
        /// The expand format.
        /// </summary>
        /// <param name="formatControls">The format controls.</param>
        /// <returns>The <see cref="string" />.</returns>
        private string ExpandFormat(string formatControls)
        {
            var destination = new StringBuilder();

            for (int src = 0; src < formatControls.Length; src++)
            {
                if ((src == 0 || formatControls[src] == ',') && formatControls[src] == '(')
                {
                    string contents = ExtractSubString(formatControls.Substring(src));
                    string expandedContents = this.ExpandFormat(contents);

                    destination.Append(expandedContents);
                    src += contents.Length + 2;
                }
                    
                    /*this is a repeated subclause */
                else if ((src == 0 || formatControls[src - 1] == ',') && char.IsDigit(formatControls[src]))
                {
                    int repeat = int.Parse(formatControls[src].ToString(CultureInfo.InvariantCulture));

                    // skip over the repeat count
                    while (char.IsDigit(formatControls[src]))
                    {
                        src++;
                    }

                    string next = formatControls.Substring(src);

                    string contents = ExtractSubString(next);
                    string expandedContents = this.ExpandFormat(contents);

                    for (int i = 0; i < repeat; i++)
                    {
                        destination.Append(expandedContents);
                        if (i < repeat - 1)
                        {
                            destination.Append(",");
                        }
                    }

                    if (next[0] == '(')
                    {
                        src += contents.Length + 2;
                    }
                    else
                    {
                        src += contents.Length - 1;
                    }
                }
                else
                {
                    // we need to ensure that we move the index back one to ensure we pick up closing
                    // brackets and commas
                    destination.Append(formatControls[src]);
                }
            }

            return destination.ToString();
        }

        /// <summary>
        /// The extract sub string.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The <see cref="string" />.</returns>
        private static string ExtractSubString(string input)
        {
            int bracket = 0, i;

            for (i = 0; i < input.Length && (bracket > 0 || input[i] != ','); i++)
            {
                switch (input[i])
                {
                    case '(':
                        bracket++;
                        break;
                    case ')':
                        bracket--;
                        break;
                }
            }

            return input[0] == '(' ? input.Substring(1, i - 2) : input.Substring(0, i);
        }

        /// <summary>
        /// The build subfields.
        /// </summary>
        /// <returns>The <see cref="bool" />.</returns>
        private bool BuildSubfields()
        {
            var subFields = new String(this._arrayDescription.Skip(this._arrayDescription.LastIndexOf('*')).ToArray());

            if (subFields.First() == '*')
            {
                this.RepeatingSubfields = true;
            }

            List<string> subfieldNames = subFields.Split('!').ToList();

            foreach (string subField in subfieldNames)
            {
                this.SubFieldDefinitions.Add(new DdfSubFieldDefinition { Name = subField });
            }

            return true;
        }

        #region Nested type: DdfDataStructCode

        /// <summary>
        /// The ddf data struct code.
        /// </summary>
        private enum DdfDataStructCode
        {
            /// <summary>
            /// The dsc elementary.
            /// </summary>
            DscElementary,

            /// <summary>
            /// The dsc vector.
            /// </summary>
            DscVector,

            /// <summary>
            /// The dsc array.
            /// </summary>
            DscArray,

            /// <summary>
            /// The dsc concatenated.
            /// </summary>
            DscConcatenated
        };

        #endregion
    }
}