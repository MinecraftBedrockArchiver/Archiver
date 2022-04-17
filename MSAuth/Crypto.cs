using System.Security.Cryptography;
using System.Text;

namespace MicrosoftAuth
{
    public class Crypto
    {

        public static byte[] CreateNonce(int size = 32)
            => RandomNumberGenerator.GetBytes(size);

        public static string SignData(byte[] dataToSign, byte[] key, string keyUsage, byte[] nonce)
        {
            var usedKey = GenerateSharedKey(32, key, keyUsage, nonce);

            var usedAlgo = new HMACSHA256(usedKey);
            usedAlgo.Initialize();

            var signature = usedAlgo.ComputeHash(dataToSign);

            return Convert.ToBase64String(signature);
        }

        public static byte[] GenerateSharedKey(int keyLength, byte[] inKey, string keyUsage, byte[] nonce)
        {
            // I have no idea how or why this works, just that it does

            byte[] sharedKeyMaterial = new byte[4 + keyUsage.Length + 1 + nonce.Length + 4];
            int offset = 0;
            offset += 4;

            Array.Copy(Encoding.UTF8.GetBytes(keyUsage), 0, sharedKeyMaterial, offset, keyUsage.Length);
            offset += keyUsage.Length;

            sharedKeyMaterial[offset] = 0x0;
            offset++;

            Array.Copy(nonce, 0, sharedKeyMaterial, offset, nonce.Length);
            offset += nonce.Length;

            var keyBitLength = keyLength * 8;

            sharedKeyMaterial[offset] = (byte)((keyBitLength & 0xff000000) >> 24);
            sharedKeyMaterial[offset + 1] = (byte)((keyBitLength & 0x00ff0000) >> 16);
            sharedKeyMaterial[offset + 2] = (byte)((keyBitLength & 0x0000ff00) >> 8);
            sharedKeyMaterial[offset + 3] = (byte)(keyBitLength & 0x000000ff);

            offset += 4;

            int currentKeyLength = 0;
            int currentHashCount = 1;

            var sharedKey = new byte[keyLength];

            while (currentKeyLength < keyLength)
            {
                sharedKeyMaterial[0] = (byte)((currentHashCount & 0xff000000) >> 24);
                sharedKeyMaterial[1] = (byte)((currentHashCount & 0x00ff0000) >> 16);
                sharedKeyMaterial[2] = (byte)((currentHashCount & 0x0000ff00) >> 8);
                sharedKeyMaterial[3] = (byte)(currentHashCount & 0x000000ff);

                currentHashCount++;

                var usedAlgo = new HMACSHA256(inKey);
                usedAlgo.Initialize();

                var signature = usedAlgo.ComputeHash(sharedKeyMaterial);
                var amount = Math.Min(signature.Length, keyLength - currentKeyLength);
                Array.Copy(signature, 0, sharedKey, currentKeyLength, amount);
                currentKeyLength += amount;
            }

            return sharedKey;
        }

        public static byte[] HashSha256(string data)
            => SHA256.HashData(Encoding.UTF8.GetBytes(data));

        public static string DecryptSecurityResponse(byte[] key, string encryptedData)
        {
            var encryptedBytes = Convert.FromBase64String(encryptedData);
            var iv = encryptedBytes[..16];
            var encrypted = encryptedBytes[16..];

            var algo = Aes.Create();
            algo.Mode = CipherMode.CBC;
            algo.KeySize = 256;
            algo.Key = key;
            algo.IV = iv;

            var decryptor = algo.CreateDecryptor();

            var decryptedBody = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

            return Encoding.UTF8.GetString(decryptedBody);

        }
    }
}