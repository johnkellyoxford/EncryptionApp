﻿using FactaLogicaSoftware.CryptoTools.Exceptions;
using JetBrains.Annotations;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace FactaLogicaSoftware.CryptoTools.Algorithms.Symmetric
{
    /// <inheritdoc />
    public sealed class AesCryptoManager : SymmetricCryptoManager
    {
        private static readonly int[] KeySizes = { 128, 192, 256 };

        private byte[] _initializationVector;

        /// <summary>
        /// The initialization vector used for
        /// transformation
        /// </summary>
        /// <remarks>You can leave
        /// this empty and pass the IV as a function
        /// argument instead</remarks>
        public byte[] InitializationVector
        {
            get => this._initializationVector;
            set
            {
                if (value?.Length < this.SymmetricAlgorithm.BlockSize)
                    throw new ArgumentException("Length of IV must be at least as much as length of block size");

                this._initializationVector = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets or sets the KeySize for the AES algorithm
        /// Valid sizes are 128, 192, and 256
        /// </summary>
        /// <exception cref="T:System.ArgumentException"></exception>
        public override int? KeySize
        {
            get => this.SymmetricAlgorithm.KeySize;
            set
            {
                if (value.HasValue && !KeySizes.Contains(value.Value))
                {
                    throw new ArgumentException("Key is not a valid length");
                }

                this.SymmetricAlgorithm.KeySize = value ?? this.SymmetricAlgorithm.KeySize;
            }
        }

        /// <summary>
        /// Returns the default algorithm for this class.
        /// Used for chaining constructor calls
        /// </summary>
        private static SymmetricAlgorithm DefaultAlgorithm { get; } = new AesCng
        {
            BlockSize = 128,
            KeySize = 128,
            Mode = CipherMode.CBC,
            Padding = PaddingMode.PKCS7
        };

        /// <summary>
        /// Returns the default chunk size for this class.
        /// Used for chaining constructor calls
        /// </summary>
        private static int DefaultChunkSize => 1024 * 4;

        /// <inheritdoc />
        /// <summary>
        /// The default constructor which uses 4kb of memory and uses AesCng
        /// </summary>
        public AesCryptoManager() : this(DefaultChunkSize)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Defines the maximum size read through streams and uses AesCng
        /// </summary>
        /// <param name="memoryConst">The number of bytes to read and write</param>
        public AesCryptoManager(int memoryConst) : this(memoryConst, DefaultAlgorithm)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Uses 4kb read/write values and an AES algorithm of your choice
        /// </summary>
        /// <param name="algorithm">The algorithm to use</param>
        public AesCryptoManager([NotNull] SymmetricAlgorithm algorithm) : this(DefaultChunkSize, algorithm)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Uses custom read/write values and an AES algorithm of your choice
        /// </summary>
        /// <param name="memoryConst">The number of bytes to read and write</param>
        /// <param name="algorithm">The algorithm to use</param>
        public AesCryptoManager(int memoryConst, [NotNull] SymmetricAlgorithm algorithm) : base(memoryConst, algorithm)
        {
            this.KeySize = null;

            // Check if the algorithm is part of the 2 .NET algorithms currently FIPS compliant
            if (algorithm is AesCng || algorithm is AesCryptoServiceProvider || algorithm is TripleDESCng)
            {
                this.IsFipsCompliant = true;
            }
            else
            {
                this.IsFipsCompliant = false;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Encrypts data from one file to another using AES
        /// </summary>
        /// <param name="inputFile">The file path to the unencrypted data</param>
        /// <param name="outputFile">The file path to output the encrypted data to</param>
        /// <param name="key">The key bytes</param>
        /// <param name="iv">The initialization vector</param>
        /// <exception cref="ArgumentNullException"></exception>
        public override void EncryptFileBytes(string inputFile, string outputFile, byte[] key, byte[] iv = null)
        {
            #region CONTRACT

            if (inputFile == null) throw new ArgumentNullException(nameof(inputFile));
            if (outputFile == null) throw new ArgumentNullException(nameof(outputFile));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (iv == null)
                iv = this.InitializationVector ?? throw new ArgumentNullException(nameof(this.InitializationVector));

            if (KeySize.HasValue && key.Length * 8 != KeySize) throw new ArgumentException(nameof(key) + " must be the length of KeySize - " + KeySize + " bits");

            if (iv.Length * 8 < this.SymmetricAlgorithm.BlockSize)
                throw new ArgumentException("Initialization vector must be at least as many bits as the block size");

            if (!File.Exists(inputFile))
            {
                throw new ArgumentException("Input file does not exist");
            }

            Contract.EndContractBlock();

            #endregion CONTRACT

            // Set actual IV and key
            this.SymmetricAlgorithm.Key = key;
            this.SymmetricAlgorithm.IV = iv.Take(this.SymmetricAlgorithm.BlockSize / 8).ToArray();

            InternalTransformFile(inputFile, outputFile, this.SymmetricAlgorithm.CreateEncryptor());
        }

        /// <inheritdoc />
        /// <summary>
        /// Decrypts data from one file to another using AES
        /// </summary>
        /// <param name="inputFile">The file path to the encrypted data</param>
        /// <param name="outputFile">The file path to output the decrypted data to</param>
        /// <param name="key">The key bytes</param>
        /// <param name="iv">The initialization vector</param>
        /// <exception cref="ArgumentNullException"></exception>
        public override void DecryptFileBytes(string inputFile, string outputFile, byte[] key, byte[] iv = null)
        {
            #region CONTRACT

            if (inputFile == null) throw new ArgumentNullException(nameof(inputFile));
            if (outputFile == null) throw new ArgumentNullException(nameof(outputFile));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (iv == null)
                iv = this.InitializationVector ?? throw new ArgumentNullException(nameof(this.InitializationVector));

            if (KeySize.HasValue && key.Length * 8 != KeySize) throw new ArgumentOutOfRangeException(nameof(key) + " must be the length of KeySize - " + KeySize + " bits");

            if (this.InitializationVector.Length * 8 < this.SymmetricAlgorithm.BlockSize)
                throw new ArgumentException("Initialization vector set in class must be at least as many bits as the block size");

            if (!File.Exists(inputFile))
            {
                throw new ArgumentException("Input file does not exist");
            }

            Contract.EndContractBlock();

            #endregion CONTRACT

            // Set actual IV and key
            this.SymmetricAlgorithm.Key = key;
            this.SymmetricAlgorithm.IV = iv.Take(this.SymmetricAlgorithm.BlockSize / 8).ToArray();

            InternalTransformFile(inputFile, outputFile, this.SymmetricAlgorithm.CreateDecryptor());
        }

        /// <inheritdoc />
        /// <summary>
        /// Encrypts an array of bytes
        /// </summary>
        /// <param name="data">The data to encrypt</param>
        /// <param name="key">The key to encrypt with</param>
        /// <param name="iv">The initialization vector</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns>The encrypted byte array</returns>
        public override byte[] EncryptBytes(byte[] data, byte[] key, byte[] iv = null)
        {
            #region CONTRACT

            if (data == null) throw new ArgumentNullException(nameof(data));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (iv == null)
                iv = this.InitializationVector ?? throw new ArgumentNullException(nameof(this.InitializationVector));

            if (KeySize.HasValue && key.Length * 8 != KeySize) throw new ArgumentOutOfRangeException(nameof(key) + " must be the length of KeySize - " + KeySize + " bits");

            if (this.InitializationVector.Length * 8 < this.SymmetricAlgorithm.BlockSize)
                throw new ArgumentException("Initialization vector set in class must be at least as many bits as the block size");

            Contract.EndContractBlock();

            #endregion CONTRACT

            // AES values
            this.SymmetricAlgorithm.Key = key;
            this.SymmetricAlgorithm.IV = iv;

            // Put the plaintext byte array into memory, and read it through the crypto stream to encrypt it
            var memStream = new MemoryStream(data);
            var cryptoStream = new CryptoStream(memStream, this.SymmetricAlgorithm.CreateEncryptor(), CryptoStreamMode.Read);
            using (var binReader = new BinaryReader(cryptoStream))
            {
                try
                {
                    return binReader.ReadBytes((int)memStream.Length);
                }
                catch (OverflowException e)
                {
                    throw new OverflowException("Byte array too large to encrypt", e);
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Decrypts an array of bytes
        /// </summary>
        /// <param name="data">The data to decrypt</param>
        /// <param name="key">The key to decrypt with</param>
        /// <param name="iv">The initialization vector</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns>The decrypted byte array</returns>
        public override byte[] DecryptBytes(byte[] data, byte[] key, byte[] iv = null)
        {
            #region CONTRACT

            if (data == null) throw new ArgumentNullException(nameof(data));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (iv == null)
                iv = this.InitializationVector ?? throw new ArgumentNullException(nameof(this.InitializationVector));

            if (KeySize.HasValue && key.Length * 8 != KeySize) throw new ArgumentOutOfRangeException(nameof(key) + " must be the length of KeySize - " + KeySize + " bits");

            if (this.InitializationVector.Length * 8 < this.SymmetricAlgorithm.BlockSize)
                throw new ArgumentException("Initialization vector set in class must be at least as many bits as the block size");

            Contract.EndContractBlock();

            #endregion CONTRACT

            this.SymmetricAlgorithm.Key = key;
            this.SymmetricAlgorithm.IV = iv;

            // Put the cipher text byte array into memory, and read it through the crypto stream to decrypt it
            var memStream = new MemoryStream(data);
            var cryptoStream = new CryptoStream(memStream, this.SymmetricAlgorithm.CreateDecryptor(), CryptoStreamMode.Read);
            using (var binReader = new BinaryReader(cryptoStream))
            {
                try
                {
                    return binReader.ReadBytes((int)memStream.Length);
                }
                catch (OverflowException e)
                {
                    throw new OverflowException("Byte array to large to decrypt", e);
                }
            }
        }
    }
}
