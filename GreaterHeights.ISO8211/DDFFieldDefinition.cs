using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace GreaterHeights.ISO8211
{
    public class DdfFieldDefinition
    {
        private string _arrayDescription;
        private DdfDataStructCode _dataStructureCode;
        private string _formatControls;

        public DdfFieldDefinition()
        {
            SubFieldDefinitions = new List<DdfSubFieldDefinition>();
            RepeatingSubfields = false;
        }

        public List<DdfSubFieldDefinition> SubFieldDefinitions { get; private set; }

        private int FixedWidth { get; set; }

        public string FieldName { get; private set; }
        public string Tag { get; private set; }
        private bool RepeatingSubfields { get; set; }

        public bool Initialize(DdfLeader leader, string tag, int size, char[] record, int offset)
        {
            if (leader == null) throw new ArgumentNullException("leader");

            Tag = tag;

            int charsConsumed;

            /* -------------------------------------------------------------------- */
            /*      Set the data struct and type codes.                             */
            /* -------------------------------------------------------------------- */
            switch (record[offset])
            {
                case ' ': /* for ADRG, DIGEST USRP, DIGEST ASRP files */
                case '0':
                    _dataStructureCode = DdfDataStructCode.DscElementary;
                    break;
                case '1':
                    _dataStructureCode = DdfDataStructCode.DscVector;
                    break;
                case '2':
                    _dataStructureCode = DdfDataStructCode.DscArray;
                    break;
                case '3':
                    _dataStructureCode = DdfDataStructCode.DscConcatenated;
                    break;
                default:
                    throw new ApplicationException("Unrecognised data_struct_code value " + record[offset] +
                                                   " Field " + tag + " initialization incorrect.");
                    //dataStructureCode = DDF_data_struct_code.dsc_elementary;
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
                        "Unrecognised data_type_code value " + record[offset + 1] +
                        " Field " + tag + "initialization incorrect.");
                    //dataTypeCode = dtc_char_string;
            }

            /* -------------------------------------------------------------------- */
            /*      Capture the field name, description (sub field names), and      */
            /*      format statements.                                              */
            /* -------------------------------------------------------------------- */
            var fieldDataOffset = leader.FieldControlLength;


            FieldName = DdfUtils.ReadStringFromBufffer(
                record, offset + fieldDataOffset, size - fieldDataOffset,
                Constants.DdfUnitTerminator, Constants.DdfFieldTerminator,
                out charsConsumed);

            fieldDataOffset += charsConsumed;

            _arrayDescription =
                DdfUtils.ReadStringFromBufffer(record, offset + fieldDataOffset, size - fieldDataOffset,
                                               Constants.DdfUnitTerminator, Constants.DdfFieldTerminator,
                                               out charsConsumed);

            fieldDataOffset += charsConsumed;

            _formatControls =
                DdfUtils.ReadStringFromBufffer(record, offset + fieldDataOffset, size - fieldDataOffset,
                                               Constants.DdfUnitTerminator, Constants.DdfFieldTerminator,
                                               out charsConsumed);


            if (_dataStructureCode != DdfDataStructCode.DscElementary)
            {
                if (!BuildSubfields())
                    return false;

                if (!ApplyFormats())
                    return false;
            }

            return true;
        }

        private bool ApplyFormats()
        {
            /* -------------------------------------------------------------------- */
            /*      Verify that the format string is contained within brackets.     */
            /* -------------------------------------------------------------------- */
            if (_formatControls.Length < 2
                || _formatControls.First() != '('
                || _formatControls.Last() != ')')
            {
                throw new ApplicationException(
                    "Format controls missing brackets");
            }

            //remove brackets and expand format
            var formatList = ExpandFormat(_formatControls).Split(',').ToList();

            if (formatList.Count > SubFieldDefinitions.Count)
                throw new ApplicationException(string.Format("Got more formats than subfields for field {0}", FieldName));

            var index = 0;

            foreach (var subFieldDefintion in SubFieldDefinitions)
            {
                subFieldDefintion.SetFormat(formatList[index]);
                index++;
            }

            if (index < SubFieldDefinitions.Count)
                throw new ApplicationException("got less formats than subfields for field " + FieldName); 

            foreach (var subField in SubFieldDefinitions)
            {
                if (subField.FormatWidth == 0)
                {
                    FixedWidth = 0;
                    break;
                }
                FixedWidth += subField.FormatWidth;
            }
            return true;
        }

        private string ExpandFormat(string formatControls)
        {
            var destination = new StringBuilder();

            for (var src = 0; src < formatControls.Length; src++)
            {
                if ((src == 0 || formatControls[src] == ',') && formatControls[src] == '(')
                {
                    var contents = ExtractSubString(formatControls.Substring(src));
                    var expandedContents = ExpandFormat(contents);

                    destination.Append(expandedContents);
                    src += contents.Length + 2;
                }
                    /*this is a repeated subclause */
                else if ((src == 0 || formatControls[src - 1] == ',') && Char.IsDigit(formatControls[src]))
                {
                    var repeat = Int32.Parse(formatControls[src].ToString(CultureInfo.InvariantCulture));

                    //skip over the repeat count
                    while (char.IsDigit(formatControls[src]))
                    {
                        src++;
                    }

                    var next = formatControls.Substring(src);

                    var contents = ExtractSubString(next);
                    var expandedContents = ExpandFormat(contents);

                    for (var i = 0; i < repeat; i++)
                    {
                        destination.Append(expandedContents);
                        if (i < repeat - 1)
                            destination.Append(",");
                    }

                    if (next[0] == '(')
                        src += contents.Length + 2;
                    else
                        src += contents.Length - 1;
                }
                else
                {
                    //we need to ensure that we move the index back one to ensure we pick up closing
                    //brackets and commas
                    destination.Append(formatControls[src]);
                }
            }

            return destination.ToString();
        }

        private static string ExtractSubString(string input)
        {
            int bracket = 0, i;

            for (i = 0;
                 i < input.Length && (bracket > 0 || input[i] != ',');
                 i++)
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

        private bool BuildSubfields()
        {
            var subFields = new String(_arrayDescription.Skip(_arrayDescription.LastIndexOf('*')).ToArray());

            if (subFields.First() == '*')
                RepeatingSubfields = true;

            var subfieldNames = subFields.Split('!').ToList();

            foreach (var subField in subfieldNames)
                SubFieldDefinitions.Add(new DdfSubFieldDefinition {Name = subField});

            return true;
        }

        #region Nested type: DdfDataStructCode

        private enum DdfDataStructCode
        {
            DscElementary,
            DscVector,
            DscArray,
            DscConcatenated
        };

        #endregion
    }
}