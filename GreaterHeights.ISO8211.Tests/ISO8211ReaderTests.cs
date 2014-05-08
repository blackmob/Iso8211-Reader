// ***********************************************************************
// Assembly         : GreaterHeights.ISO8211.Tests
// Author           : Ben Blackmore
// Created          : 05-08-2014
//
// Last Modified By : Ben Blackmore
// Last Modified On : 05-08-2014
// ***********************************************************************
// <copyright file="ISO8211ReaderTests.cs" company="Greater Heights Ltd">
//     Copyright (c) Greater Heights Ltd. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace GreaterHeights.ISO8211.Tests
{
    using System.Diagnostics;

    using NUnit.Framework;

    /// <summary>
    /// Class Iso8211ReaderTests.
    /// </summary>
    [TestFixture]
    public class Iso8211ReaderTests 
    {
        /// <summary>
        /// Shoulds the open the catalog031 file.
        /// </summary>
        [Test]
        public void ShouldOpenTheCatalog031File()
        {
            using (var reader = new Iso8211Reader(@".\CATALOG.031"))
            {
                reader.Open();

                int i= 0;
                DdfRecord record;
                while ((record = reader.Read()) != null)
                {
                    i++;
                    foreach (var field in record.Fields)
                    {                         
                        Debug.WriteLine(field.FieldDefinition.FieldName);
                        var data = field.GetRecord();
                        foreach (var key in data.Keys)
                        {
                            Debug.WriteLine("{0}:{1}", key, data[key].Value);  
                        }
                    }                    
                }
                Debug.WriteLine(i);  
            }
        }
    }
}
