using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Cryptic_Transmitter
{
    internal class CryptoEngine
    {
        private string keyBase64;
        private string ivBase64;

        private readonly Action<string> log;
        private readonly Action<string> ivUpdated;

        public CryptoEngine(Action<string> logCallback, Action<string> ivCallback)
        {
            log = logCallback;
            ivUpdated = ivCallback;
        }

        public void SetKey(string base64Key) => keyBase64 = base64Key;
        public string GetKey() => keyBase64;
        public string GetIV() => ivBase64;

        public string Encrypt(string plainText)
        {
            byte[] key = GetKeyBytes();

            using var aes = new AesGcm(key);

            byte[] nonce = new byte[12];
            RandomNumberGenerator.Fill(nonce);

            ivBase64 = Convert.ToBase64String(nonce);
            ivUpdated?.Invoke(ivBase64);

            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipher = new byte[plaintextBytes.Length];
            byte[] tag = new byte[16];

            aes.Encrypt(nonce, plaintextBytes, cipher, tag);

            byte[] combined = new byte[nonce.Length + cipher.Length + tag.Length];
            Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
            Buffer.BlockCopy(cipher, 0, combined, nonce.Length, cipher.Length);
            Buffer.BlockCopy(tag, 0, combined, nonce.Length + cipher.Length, tag.Length);

            return Convert.ToBase64String(combined);
        }

        public string Decrypt(string base64Cipher)
        {
            byte[] key = GetKeyBytes();
            byte[] combined = Convert.FromBase64String(base64Cipher);

            byte[] nonce = new byte[12];
            byte[] tag = new byte[16];
            byte[] cipher = new byte[combined.Length - nonce.Length - tag.Length];

            Buffer.BlockCopy(combined, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(combined, nonce.Length, cipher, 0, cipher.Length);
            Buffer.BlockCopy(combined, nonce.Length + cipher.Length, tag, 0, tag.Length);

            using var aes = new AesGcm(key);
            byte[] plain = new byte[cipher.Length];

            aes.Decrypt(nonce, cipher, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }

        private byte[] GetKeyBytes()
        {
            if (string.IsNullOrWhiteSpace(keyBase64))
                throw new InvalidOperationException("Key not set.");

            byte[] key = Convert.FromBase64String(keyBase64);
            if (key.Length != 32)
                throw new InvalidOperationException("Key must be 32 bytes.");

            return key;
        }


        public void GenerateKey()
        {
            byte[] key = new byte[32];
            RandomNumberGenerator.Fill(key);
            string keyBase64 = Convert.ToBase64String(key);
            this.keyBase64 = keyBase64;
        }

        public static bool ValidateBase64(string input, out string errorMessage)
        {
            errorMessage = "";

            try
            {
                byte[] key = Convert.FromBase64String(input.Trim());

                if (key.Length != 32)
                {
                    return false;
                }


                return true;
            }
            catch
            {
                return false;
            }
        }

        public static byte[] Protect(string data)
        {
            return ProtectedData.Protect(
                Encoding.UTF8.GetBytes(data),
                null,
                DataProtectionScope.CurrentUser);
        }

        public static string Unprotect(byte[] data)
        {
            return Encoding.UTF8.GetString(
                ProtectedData.Unprotect(
                    data,
                    null,
                    DataProtectionScope.CurrentUser));
        }
    }
}
