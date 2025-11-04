using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace AppBackend.Services.Helpers
{
    public class BookingTokenHelper
    {
        private readonly string _encryptionKey;

        public BookingTokenHelper(IConfiguration configuration)
        {
            // Lấy key từ configuration hoặc sử dụng key mặc định
            _encryptionKey = configuration["BookingToken:EncryptionKey"] ?? "StayHub2025SecretKeyForBookingToken";
        }

        /// <summary>
        /// Mã hóa bookingId thành token
        /// </summary>
        public string EncodeBookingId(int bookingId)
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
                        // Ghi IV vào đầu stream
                        msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(bookingId.ToString());
                        }

                        var encrypted = msEncrypt.ToArray();
                        return Convert.ToBase64String(encrypted)
                            .Replace("+", "-")
                            .Replace("/", "_")
                            .Replace("=", ""); // URL-safe base64
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error encoding booking ID: {ex.Message}");
            }
        }

        /// <summary>
        /// Giải mã token thành bookingId
        /// </summary>
        public int DecodeBookingToken(string token)
        {
            try
            {
                // Convert from URL-safe base64
                string base64 = token.Replace("-", "+").Replace("_", "/");
                
                // Add padding if needed
                int padding = 4 - (base64.Length % 4);
                if (padding < 4)
                {
                    base64 += new string('=', padding);
                }

                byte[] cipherText = Convert.FromBase64String(base64);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = GetKey();

                    // Extract IV from the beginning
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
                throw new Exception($"Invalid booking token: {ex.Message}");
            }
        }

        private byte[] GetKey()
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(_encryptionKey));
                return hash; // 256 bits
            }
        }
    }
}

