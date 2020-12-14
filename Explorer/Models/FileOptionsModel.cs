using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Explorer.Models
{
    internal class FileOptionsModel
    {
        public static void Compress(string sourceFile, string compressedFile)
        {
            var length = compressedFile.Length - 4;
            compressedFile.Remove(length);
            sourceFile.Remove(length);
            using (var sourceStream = new FileStream(sourceFile, FileMode.OpenOrCreate))
            {
                using (var targetStream = File.Create(compressedFile))
                {
                    using (var compressionStream = new GZipStream(targetStream, CompressionMode.Compress))
                    {
                        sourceStream.CopyTo(compressionStream);
                    }
                }
            }

            File.Delete(sourceFile);
        }

        public static void Decompress(string compressedFile, string targetFile)
        {
            using (var sourceStream = new FileStream(compressedFile, FileMode.OpenOrCreate))
            {
                using (var targetStream = File.Create(targetFile))
                {
                    using (var decompressionStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(targetStream);
                    }
                }
            }

            File.Delete(compressedFile);
        }

        public static void Delete(string path)
        {
            File.Delete(path);
        }

        public static void FolderCompress(string folderPath, string compressedFilePath)
        {
            ZipFile.CreateFromDirectory(folderPath, compressedFilePath);
        }

        public static void FolderDecompress(string zipFilePath, string decompressedFilePath)
        {
            ZipFile.ExtractToDirectory(zipFilePath, decompressedFilePath);
        }
    }
}