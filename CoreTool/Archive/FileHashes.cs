using System;
using System.IO;
using System.Security.Cryptography;

namespace CoreTool.Archive
{
    public class FileHashes
    {
        public string MD5 { get; set; }
        public string SHA256 { get; set; }

        public FileHashes() { }

        public FileHashes(string filePath)
        {
            CalculateMD5(filePath);
            CalculateSHA256(filePath);
        }

        public FileHashes(string MD5, string SHA256)
        {
            this.MD5 = MD5;
            this.SHA256 = SHA256;
        }

        internal void CalculateMD5(string filePath)
        {
            using (MD5 md5 = System.Security.Cryptography.MD5.Create())
            using (FileStream fileStream = File.OpenRead(filePath))
            {
                this.MD5 = BitConverter.ToString(md5.ComputeHash(fileStream)).Replace("-", "").ToLowerInvariant();
            }
        }

        internal void CalculateSHA256(string filePath)
        {
            using (SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            using (FileStream fileStream = File.OpenRead(filePath))
            {
                this.SHA256 = BitConverter.ToString(sha256.ComputeHash(fileStream)).Replace("-", "").ToLowerInvariant();
            }
        }

        internal FileHashes Merge(FileHashes hashes)
        {
            return new FileHashes(string.IsNullOrEmpty(this.MD5) && hashes != null ? hashes.MD5 : this.MD5, string.IsNullOrEmpty(this.SHA256) && hashes != null ? hashes.SHA256 : this.SHA256);
        }
    }
}