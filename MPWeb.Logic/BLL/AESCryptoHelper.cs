using System;
using System.IO;
using System.Security.Cryptography;

namespace MPWeb.Logic.BLL
{
    public class AESCryptoHelper
    {
        public string EncryptString(string strToEncrypt, string keyBase64, string ivBase64)
        {
            byte[] encryptedStr;
            using (Aes aes = Aes.Create())
            {
                if (strToEncrypt == null || strToEncrypt.Length <= 0)
                    throw new ArgumentNullException("plainText");
                if (keyBase64 == null || keyBase64.Length <= 0)
                    throw new ArgumentNullException("Key");

                aes.Mode = CipherMode.CBC;
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Key = Convert.FromBase64String(keyBase64);
                aes.IV = Convert.FromBase64String(ivBase64);

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(strToEncrypt);
                        }
                        encryptedStr = msEncrypt.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(encryptedStr);
        }

        public string DecryptString(string rawCryptedString, string keyBase64, string ivBase64)
        {
            string decryptedString = null;
            var cryptedString = Convert.FromBase64String(rawCryptedString);

            if (cryptedString == null || cryptedString.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (keyBase64 == null || keyBase64.Length <= 0)
                throw new ArgumentNullException("Key");

            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Key = Convert.FromBase64String(keyBase64);
                aes.IV = Convert.FromBase64String(ivBase64);

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cryptedString))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader swDecrypt = new StreamReader(csDecrypt))
                        {
                            decryptedString = "";
                            decryptedString = string.Concat(decryptedString, swDecrypt.ReadToEnd());
                        }
                    }
                }
            }
            return decryptedString;
        }
    }
}
