﻿using System.Text;
using Newtonsoft.Json;

namespace FactaLogicaSoftware.CryptoTools.Information.Representatives
{
    /// <summary>
    /// The abstract class that all CryptographicRepresentative objects derive from
    /// </summary>
    public abstract class CryptographicRepresentative
    {
        // padding used to find start and end of header object - human readable
        private protected Encoding Encoding;

        [JsonIgnore]
        public static readonly string StartChars;
        [JsonIgnore]
        public static readonly string EndChars;

        static CryptographicRepresentative()
        {
            StartChars = "BEGIN ENCRYPTION HEADER STRING";
            EndChars = "END ENCRYPTION HEADER STRING";
        }

        [JsonIgnore]
        public long HeaderLength { get; private protected set; }

        [JsonIgnore]
        public InfoType Type { get; private protected set; }

        public enum InfoType
        {
            Read,
            Write
        }

        // primary data object - see CryptoStructs.cs for documentation
        public string CryptoManager;

        /// <summary>
        /// If overriden in a derived class, writes a header representation of the object to a file as JSON
        /// </summary>
        /// <param name="path">The path to the file to be written to - will be overwritten</param>
        public abstract void WriteHeaderToFile(string path);

        /// <summary>
        /// If overriden in a derived class, reads a header from a file and creates a CryptographicRepresentative object from it
        /// </summary>
        /// <param name="path"></param>
        /// <returns>The object created from the header</returns>
        public abstract void ReadHeaderFromFile(string path);

        /// <summary>
        /// If overriden in a derived class, creates a header representation of the object as a string as JSON
        /// </summary>
        /// <returns>The JSON string representing the object</returns>
        public abstract string GenerateHeader();

        /// <summary>
        /// If overriden in a derived class, reads a header from a string and creates a CryptographicRepresentative object from it
        /// </summary>
        /// <param name="data"></param>
        public abstract CryptographicRepresentative ReadHeader(string data);
    }
}