using System;
using System.Security.Cryptography;

namespace Yuki.SaveSystem
{
    /// <summary>内置存档密码的 AES 密文；密钥随客户端分发，仅用于避免源码明文存储。</summary>
    internal static class SavePassword
    {
        private const string EncryptedValue = "PKFIWeg/yxcsrarn4Pfi8Q==";
        private const string Key = "FMLxymSXi26XS2vW3kAFk/Rb+uapiWmBPMkv3z1bYb8=";
        private const string IV = "MWKW4XXFfcFYpEfTkEZGlg==";

        internal static byte[] Decrypt()
        {
            byte[] key = Convert.FromBase64String(Key);
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = Convert.FromBase64String(IV);
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    byte[] ciphertext = Convert.FromBase64String(EncryptedValue);
                    using (var decryptor = aes.CreateDecryptor())
                        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                }
            }
            finally
            {
                Array.Clear(key, 0, key.Length);
            }
        }
    }
}
