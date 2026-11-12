using System;
using System.Security.Cryptography;
using System.Text;

using SW.Util;

namespace SW.Data
{
    /// <summary>
    /// SWSaveDataManager의 저장 파일과 클라우드 백업을 AES로 감쌉니다.
    /// 키는 SWPlayerPrefs와 같은 설정(Salt)에서 만들고, IV는 쓸 때마다 새로 만들어 앞에 붙입니다.
    /// 머리표가 없는 내용은 암호화 전 버전의 평문으로 보고 그대로 읽습니다.
    /// </summary>
    internal static class SWSaveCrypto
    {
        #region 상수
        /// <summary>암호화한 내용 앞에 붙는 머리표입니다. 평문 JSON은 '{'로 시작하므로 겹치지 않습니다.</summary>
        private const string Header = "SWENC1:";
        /// <summary>AES 블록(IV) 크기입니다.</summary>
        private const int IVSize = 16;
        #endregion // 상수

        #region 암호화
        /// <summary>내용을 암호화합니다. 실패하면 null입니다.</summary>
        /// <param name="plain">평문</param>
        /// <returns>머리표가 붙은 암호문</returns>
        public static string Encrypt(string plain)
        {
            if (string.IsNullOrEmpty(plain))
            {
                return plain;
            }

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = SWPlayerPrefs.GetFileKey();
                    aes.GenerateIV();
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(plain);
                        byte[] encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                        byte[] packed = new byte[IVSize + encrypted.Length];
                        Buffer.BlockCopy(aes.IV, 0, packed, 0, IVSize);
                        Buffer.BlockCopy(encrypted, 0, packed, IVSize, encrypted.Length);
                        return Header + Convert.ToBase64String(packed);
                    }
                }
            }
            catch (Exception exception)
            {
                SWLog.LogError($"[SWSaveCrypto] 암호화 실패: {exception.Message}");
                return null;
            }
        }

        /// <summary>
        /// 내용을 평문으로 바꿉니다. 머리표가 없으면 평문으로 보고 그대로 돌려줍니다.
        /// 복호화하지 못하면(다른 Salt, 손상) false입니다.
        /// </summary>
        /// <param name="stored">파일·클라우드에서 읽은 내용</param>
        /// <param name="plain">평문</param>
        /// <returns>평문을 얻었으면 true</returns>
        public static bool TryDecrypt(string stored, out string plain)
        {
            plain = stored;
            if (string.IsNullOrEmpty(stored) || !stored.StartsWith(Header, StringComparison.Ordinal))
            {
                return true;
            }

            try
            {
                byte[] packed = Convert.FromBase64String(stored.Substring(Header.Length));
                if (packed.Length <= IVSize)
                {
                    plain = null;
                    return false;
                }

                byte[] iv = new byte[IVSize];
                Buffer.BlockCopy(packed, 0, iv, 0, IVSize);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = SWPlayerPrefs.GetFileKey();
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        byte[] decrypted = decryptor.TransformFinalBlock(packed, IVSize, packed.Length - IVSize);
                        plain = Encoding.UTF8.GetString(decrypted);
                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                SWLog.LogWarning($"[SWSaveCrypto] 복호화 실패: {exception.Message}");
                plain = null;
                return false;
            }
        }
        #endregion // 암호화

        #region 비교
        /// <summary>저장 내용 비교용 지문(SHA-256)입니다. 같은 내용이면 같은 값입니다.</summary>
        /// <param name="parts">비교할 내용들</param>
        /// <returns>16진수 지문</returns>
        public static string Fingerprint(params string[] parts)
        {
            using (SHA256 sha = SHA256.Create())
            {
                var builder = new StringBuilder();
                foreach (string part in parts)
                {
                    builder.Append(part?.Length ?? -1).Append(':').Append(part).Append('\n');
                }

                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                var hex = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash)
                {
                    hex.Append(value.ToString("x2"));
                }

                return hex.ToString();
            }
        }
        #endregion // 비교
    }
}
