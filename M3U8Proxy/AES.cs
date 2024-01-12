using System.Security.Cryptography;
using System.Text;

namespace M3U8Proxy;

class AES
{
    private static readonly string KeyString = "anifydobesupercoolbrodudeawesome";

    public static string Encrypt(string plainText)
    {
        byte[] cipherData;
        Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(KeyString);
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        ICryptoTransform cipher = aes.CreateEncryptor(aes.Key, aes.IV);

        using (MemoryStream ms = new MemoryStream())
        {
            using (CryptoStream cs = new CryptoStream(ms, cipher, CryptoStreamMode.Write))
            {
                using (StreamWriter sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }
            }

            cipherData = ms.ToArray();
        }

        byte[] combinedData = new byte[aes.IV.Length + cipherData.Length];
        Array.Copy(aes.IV, 0, combinedData, 0, aes.IV.Length);
        Array.Copy(cipherData, 0, combinedData, aes.IV.Length, cipherData.Length);
        return Convert.ToBase64String(combinedData);
    }

    public static string Decrypt(string combinedString)
    {
        string plainText;
        byte[] combinedData = Convert.FromBase64String(combinedString);
        Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(KeyString);
        byte[] iv = new byte[aes.BlockSize / 8];
        byte[] cipherText = new byte[combinedData.Length - iv.Length];
        Array.Copy(combinedData, iv, iv.Length);
        Array.Copy(combinedData, iv.Length, cipherText, 0, cipherText.Length);
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        ICryptoTransform decipher = aes.CreateDecryptor(aes.Key, aes.IV);

        using (MemoryStream ms = new MemoryStream(cipherText))
        {
            using (CryptoStream cs = new CryptoStream(ms, decipher, CryptoStreamMode.Read))
            {
                using (StreamReader sr = new StreamReader(cs))
                {
                    plainText = sr.ReadToEnd();
                }
            }

            return plainText;
        }
    }
}