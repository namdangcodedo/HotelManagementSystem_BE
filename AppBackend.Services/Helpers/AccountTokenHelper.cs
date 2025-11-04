using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace AppBackend.Services.Helpers
{
    public class AccountTokenHelper
    {
        private readonly string _encryptionKey;

        public AccountTokenHelper(IConfiguration configuration)
        {
            _encryptionKey = configuration["AccountToken:EncryptionKey"] ?? "StayHub2025SecretKeyForAccountActivation";
        }

        /// <summary>
        /// Mã hóa accountId thành token
        /// </summary>
        public string EncodeAccountId(int accountId)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = GetKey();
                    aes.GenerateIV();

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (var msEncrypt = new System.IO.MemoryStream())
                    {
                        msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(accountId.ToString());
                        }

                        var encrypted = msEncrypt.ToArray();
                        return Convert.ToBase64String(encrypted)
                            .Replace("+", "-")
                            .Replace("/", "_")
                            .Replace("=", "");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error encoding account ID: {ex.Message}");
            }
        }

        /// <summary>
        /// Giải mã token thành accountId
        /// </summary>
        public int DecodeAccountToken(string token)
        {
            try
            {
                string base64 = token.Replace("-", "+").Replace("_", "/");
                
                int padding = 4 - (base64.Length % 4);
                if (padding < 4)
                {
                    base64 += new string('=', padding);
                }

                byte[] cipherText = Convert.FromBase64String(base64);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = GetKey();

                    byte[] iv = new byte[aes.IV.Length];
                    Array.Copy(cipherText, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (var msDecrypt = new System.IO.MemoryStream(cipherText, iv.Length, cipherText.Length - iv.Length))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                    {
                        string decrypted = srDecrypt.ReadToEnd();
                        return int.Parse(decrypted);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Invalid account token: {ex.Message}");
            }
        }

        private byte[] GetKey()
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(_encryptionKey));
                return hash;
            }
        }
    }
}

