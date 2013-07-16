using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Web.Security;

namespace Hange.Utility
{
    /// <summary>
    /// 加密类
    /// </summary>
    public static class SecurityCls
    {
        private const string Key64 = "shyluo32";
        private const string Iv64 = "lovejuan";

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="data">字符</param>
        /// <returns></returns>
        public static string Encode(string data)
        {
            byte[] byKey = Encoding.ASCII.GetBytes(Key64);
            byte[] byIv = Encoding.ASCII.GetBytes(Iv64);

            var cryptoProvider = new DESCryptoServiceProvider();
            var ms = new MemoryStream();
            var cst = new CryptoStream(ms, cryptoProvider.CreateEncryptor(byKey, byIv), CryptoStreamMode.Write);

            var sw = new StreamWriter(cst);
            sw.Write(data);
            sw.Flush();
            cst.FlushFinalBlock();
            sw.Flush();
            return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);

        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="data">字符</param>
        /// <returns></returns>
        public static string Decode(string data)
        {
            byte[] byKey = Encoding.ASCII.GetBytes(Key64);
            byte[] byIv = Encoding.ASCII.GetBytes(Iv64);

            byte[] byEnc;
            try
            {
                byEnc = Convert.FromBase64String(data);
            }
            catch
            {
                return null;
            }

            var cryptoProvider = new DESCryptoServiceProvider();
            var ms = new MemoryStream(byEnc);
            var cst = new CryptoStream(ms, cryptoProvider.CreateDecryptor(byKey, byIv), CryptoStreamMode.Read);
            var sr = new StreamReader(cst);
            return sr.ReadToEnd();
        }

        /// <summary>
        /// Md5加密
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Md5(string str)
        {
            string md5 = FormsAuthentication.HashPasswordForStoringInConfigFile(str, "MD5");
            return md5;
        }
    }
}
