using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;

namespace CoreTool.Archive
{
    public class FileHashes
    {
        public string MD5 { get; set; }
		public string SHA256 { get; set; }
		public string SHA1 { get; set; }

		public FileHashes() { }

        public FileHashes(string filePath)
        {
            CalculateHashes(filePath);
		}

        public FileHashes(string MD5, string SHA256, string SHA1)
        {
            this.MD5 = MD5;
			this.SHA256 = SHA256;
			this.SHA1 = SHA1;
		}

        internal void CalculateHashes(string filePath)
        {
            using (MD5 md5 = System.Security.Cryptography.MD5.Create())
			using (SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
			using (SHA1 sha1 = System.Security.Cryptography.SHA1.Create())
			using (FileStream fileStream = File.OpenRead(filePath))
			{
				byte[] buffer = new byte[8192]; // 8KB buffer
				int bytesRead;

				while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
				{
					if (string.IsNullOrEmpty(this.MD5)) md5.TransformBlock(buffer, 0, bytesRead, buffer, 0);
					if (string.IsNullOrEmpty(this.SHA256)) sha256.TransformBlock(buffer, 0, bytesRead, buffer, 0);
					if (string.IsNullOrEmpty(this.SHA1)) sha1.TransformBlock(buffer, 0, bytesRead, buffer, 0);
				}

				// Finalize the hash computations
				if (string.IsNullOrEmpty(this.MD5)) md5.TransformFinalBlock(buffer, 0, 0);
				if (string.IsNullOrEmpty(this.SHA256)) sha256.TransformFinalBlock(buffer, 0, 0);
				if (string.IsNullOrEmpty(this.SHA1)) sha1.TransformFinalBlock(buffer, 0, 0);

				// Convert the byte arrays to hex strings
				if (string.IsNullOrEmpty(this.MD5)) this.MD5 = BitConverter.ToString(md5.Hash).Replace("-", "").ToLowerInvariant();
				if (string.IsNullOrEmpty(this.SHA256)) this.SHA256 = BitConverter.ToString(sha256.Hash).Replace("-", "").ToLowerInvariant();
				if (string.IsNullOrEmpty(this.SHA1)) this.SHA1 = BitConverter.ToString(sha1.Hash).Replace("-", "").ToLowerInvariant();
			}
		}

        internal FileHashes Merge(FileHashes hashes)
        {
            return new FileHashes(string.IsNullOrEmpty(this.MD5) && hashes != null ? hashes.MD5 : this.MD5, string.IsNullOrEmpty(this.SHA256) && hashes != null ? hashes.SHA256 : this.SHA256, string.IsNullOrEmpty(this.SHA1) && hashes != null ? hashes.SHA1 : this.SHA1);
        }

		internal bool HasMissing()
		{
			return string.IsNullOrEmpty(this.MD5) || string.IsNullOrEmpty(this.SHA256) || string.IsNullOrEmpty(this.SHA1);
		}
	}
}