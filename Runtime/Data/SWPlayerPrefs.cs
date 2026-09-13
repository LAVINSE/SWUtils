using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

using SW.Util;

namespace SW.Data
{
    /// <summary>
    /// 키 이름은 해시로 바꾸고 값은 AES로 암호화하여 PlayerPrefs에 저장합니다.
    /// 슬롯별 JSON 가져오기와 내보내기를 제공합니다.
    /// </summary>
    public static class SWPlayerPrefs
    {
        /// <summary>
        /// JSON 직렬화용 데이터 컨테이너.
        /// </summary>
        [Serializable]
        private class PrefsData
        {
            /// <summary>저장된 항목 목록.</summary>
            public List<PrefsEntry> entries;
        }

        /// <summary>
        /// JSON 직렬화용 단일 항목.
        /// </summary>
        [Serializable]
        private class PrefsEntry
        {
            /// <summary>저장 키.</summary>
            public string key;
            /// <summary>저장 값 (평문 상태).</summary>
            public string value;
        }

        /// <summary>구분자 문자가 포함된 키를 보존하는 이름 목록입니다.</summary>
        [Serializable]
        private class KeyIndexData
        {
            public List<string> keys;
        }

        #region 필드
        /// <summary>암호화 설정 에셋 캐시입니다.</summary>
        private static SWPlayerPrefsSettings settingsCache;
        /// <summary>관리 중인 키 목록을 저장하는 PlayerPrefs 키.</summary>
        private static string KeyIndexName => $"SwUtilsPrefs_KeyIndex_{currentSlot}";
        /// <summary>암호화된 키 접두사 (일반 PlayerPrefs와 구분).</summary>
        private const string EncryptedPrefix = "SwEnc_";
        /// <summary>관리 중인 키 목록 캐시.</summary>
        private static HashSet<string> keyIndexCache;

        /// <summary>현재 활성 슬롯 이름. 모든 키 앞에 자동으로 붙습니다.</summary>
        private static string currentSlot = SWSaveSlot.Default;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 활성 슬롯 이름.</summary>
        public static string CurrentSlot => currentSlot;

        /// <summary>현재 암호화에 사용하는 salt 값입니다.</summary>
        public static string CurrentSalt => Settings.Salt;

        /// <summary>현재 암호화 IV 생성에 사용하는 salt 값입니다.</summary>
        public static string CurrentIVSalt => Settings.IVSalt;

        /// <summary>
        /// 암호화 설정 캐시를 비우고 다음 접근 시 Resources 설정 에셋을 다시 읽습니다.
        /// </summary>
        public static void ReloadSettings()
        {
            settingsCache = null;
        }

        /// <summary>프로젝트 설정 에셋 또는 기본 설정을 반환합니다.</summary>
        private static SWPlayerPrefsSettings Settings
        {
            get
            {
                if (settingsCache != null) return settingsCache;

                settingsCache = Resources.Load<SWPlayerPrefsSettings>(
                    SWPlayerPrefsSettings.ResourceAssetName);

                if (settingsCache != null) return settingsCache;

                settingsCache = ScriptableObject.CreateInstance<SWPlayerPrefsSettings>();
                return settingsCache;
            }
        }

        /// <summary>관리 중인 키 목록.</summary>
        private static HashSet<string> KeyIndex
        {
            get
            {
                if (keyIndexCache == null)
                {
                    keyIndexCache = ReadKeyIndex(currentSlot);
                }
                return keyIndexCache;
            }
        }
        #endregion // 프로퍼티

        #region 슬롯 관리
        /// <summary>
        /// 활성 슬롯을 변경합니다. 슬롯을 생략한 읽기와 쓰기에 적용됩니다.
        /// </summary>
        /// <param name="slotName">슬롯 이름</param>
        public static void SetSlot(string slotName)
        {
            if (string.IsNullOrWhiteSpace(slotName)) slotName = SWSaveSlot.Default;
            currentSlot = slotName.Trim();
            keyIndexCache = null; // 인덱스 캐시 리셋
        }
        #endregion // 슬롯 관리

        #region 암호화 / 복호화
        /// <summary>
        /// AES 키와 IV를 생성합니다. 솔트 기반으로 항상 동일한 값을 반환.
        /// </summary>
        /// <param name="key">생성된 AES 키</param>
        /// <param name="iv">생성된 AES IV</param>
        private static void GetKeyIV(out byte[] key, out byte[] iv)
        {
            using (var derive = new Rfc2898DeriveBytes(CurrentSalt, Encoding.UTF8.GetBytes(CurrentIVSalt), 1000))
            {
                key = derive.GetBytes(16);
                iv = derive.GetBytes(16);
            }
        }

        /// <summary>
        /// 문자열을 AES 암호화하여 Base64 문자열로 반환합니다.
        /// </summary>
        /// <param name="plain">암호화할 평문</param>
        /// <returns>Base64 인코딩된 암호문</returns>
        private static string Encrypt(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return string.Empty;

            try
            {
                GetKeyIV(out byte[] key, out byte[] iv);
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(plain);
                        byte[] encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                        return Convert.ToBase64String(encrypted);
                    }
                }
            }
            catch (Exception exception)
            {
                SWLog.LogError($"[SWPlayerPrefs] Encrypt failed: {exception.Message}");
                return null;
            }
        }

        /// <summary>
        /// Base64 암호문을 복호화하여 평문 문자열로 반환합니다.
        /// </summary>
        /// <param name="cipher">Base64 인코딩된 암호문</param>
        /// <returns>복호화된 평문</returns>
        private static string Decrypt(string cipher)
        {
            if (string.IsNullOrEmpty(cipher)) return string.Empty;

            try
            {
                GetKeyIV(out byte[] key, out byte[] iv);
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] bytes = Convert.FromBase64String(cipher);
                        byte[] decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                        return Encoding.UTF8.GetString(decrypted);
                    }
                }
            }
            catch (Exception exception)
            {
                SWLog.LogError($"[SWPlayerPrefs] Decrypt failed: {exception.Message}");
                return null;
            }
        }

        /// <summary>
        /// 키를 해시하여 저장용 키로 변환합니다.
        /// </summary>
        /// <param name="key">원본 키</param>
        /// <returns>해시되어 접두사가 붙은 저장용 키</returns>
        private static string HashKey(string key, string slot = null)
        {
            using (var sha = SHA256.Create())
            {
                // 슬롯 이름을 해시에 포함 → 슬롯별로 다른 키 생성
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key + CurrentSalt + NormalizeSlot(slot)));
                return EncryptedPrefix + Convert.ToBase64String(bytes)
                    .Replace("/", "_").Replace("+", "-").Substring(0, 22);
            }
        }
        #endregion // 암호화 / 복호화

        #region 키 인덱스 관리
        /// <summary>명시한 슬롯 이름을 정규화합니다.</summary>
        private static string NormalizeSlot(string slot)
            => string.IsNullOrWhiteSpace(slot) ? currentSlot : slot.Trim();

        /// <summary>문자열 배열 형식의 이름 목록을 저장할 키를 반환합니다.</summary>
        private static string GetKeyIndexName(string slot) => $"SwUtilsPrefs_KeyIndexV2_{slot}";

        /// <summary>새 형식이 없으면 기존 구분자 형식의 키 목록을 읽습니다.</summary>
        private static HashSet<string> ReadKeyIndex(string slot)
        {
            string indexKey = GetKeyIndexName(slot);
            if (PlayerPrefs.HasKey(indexKey))
            {
                KeyIndexData data = JsonUtility.FromJson<KeyIndexData>(PlayerPrefs.GetString(indexKey));
                if (data?.keys == null)
                    throw new InvalidDataException("저장 키 목록을 읽을 수 없습니다.");
                return new HashSet<string>(data.keys, StringComparer.Ordinal);
            }
            string legacy = PlayerPrefs.GetString($"SwUtilsPrefs_KeyIndex_{slot}", string.Empty);
            HashSet<string> keys = new(StringComparer.Ordinal);
            foreach (string key in legacy.Split('|'))
                if (!string.IsNullOrEmpty(key)) keys.Add(key);
            return keys;
        }

        /// <summary>키 목록을 구분자가 없는 배열 형식으로 저장합니다.</summary>
        private static void WriteKeyIndex(string slot, HashSet<string> keys)
        {
            PlayerPrefs.SetString(GetKeyIndexName(slot), JsonUtility.ToJson(new KeyIndexData { keys = new List<string>(keys) }));
        }
        #endregion // 키 인덱스 관리

        #region Set
        /// <summary>
        /// 문자열 값을 암호화하여 저장합니다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="value">저장할 값</param>
        public static void SetString(string key, string value)
        {
            SetString(key, value, currentSlot);
        }

        /// <summary>현재 선택을 바꾸지 않고 지정한 슬롯에 문자열을 저장합니다.</summary>
        public static void SetString(string key, string value, string slot)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("저장 키는 비어 있을 수 없습니다.", nameof(key));
            string selectedSlot = NormalizeSlot(slot);
            string plainValue = value ?? string.Empty;
            string encryptedValue = Encrypt(plainValue);
            if (encryptedValue == null || plainValue.Length > 0 && encryptedValue.Length == 0)
                throw new InvalidOperationException("저장값 암호화에 실패했습니다.");
            HashSet<string> keys = selectedSlot == currentSlot ? KeyIndex : ReadKeyIndex(selectedSlot);
            PlayerPrefs.SetString(HashKey(key, selectedSlot), encryptedValue);
            if (keys.Add(key)) WriteKeyIndex(selectedSlot, keys);
        }

        /// <summary>
        /// 정수 값을 암호화하여 저장합니다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="value">저장할 값</param>
        public static void SetInt(string key, int value)
        {
            SetString(key, value.ToString());
        }

        /// <summary>
        /// 64비트 정수 값을 암호화하여 저장합니다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="value">저장할 값</param>
        public static void SetLong(string key, long value)
        {
            SetString(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 실수 값을 암호화하여 저장합니다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="value">저장할 값</param>
        public static void SetFloat(string key, float value)
        {
            SetString(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 배정밀도 실수 값을 암호화하여 저장합니다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="value">저장할 값</param>
        public static void SetDouble(string key, double value)
        {
            SetString(key, value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Boolean 값을 암호화하여 저장합니다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="value">저장할 값</param>
        public static void SetBool(string key, bool value)
        {
            SetString(key, value ? "1" : "0");
        }
        #endregion // Set

        #region Get
        /// <summary>
        /// 암호화된 문자열 값을 복호화하여 반환합니다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="defaultValue">키가 없을 때 반환할 기본값</param>
        /// <returns>복호화된 값</returns>
        public static string GetString(string key, string defaultValue = "")
        {
            return GetString(key, defaultValue, currentSlot);
        }

        /// <summary>현재 선택을 바꾸지 않고 지정한 슬롯의 문자열을 읽습니다.</summary>
        public static string GetString(string key, string defaultValue, string slot)
        {
            string encryptedKey = HashKey(key, slot);
            if (!PlayerPrefs.HasKey(encryptedKey)) return defaultValue;
            string encryptedValue = PlayerPrefs.GetString(encryptedKey, string.Empty);
            string decrypted = Decrypt(encryptedValue);
            return decrypted ?? defaultValue;
        }

        /// <summary>
        /// 암호화된 정수 값을 복호화하여 반환합니다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="defaultValue">키가 없을 때 반환할 기본값</param>
        /// <returns>복호화된 값</returns>
        public static int GetInt(string key, int defaultValue = 0)
        {
            string stored = GetString(key, null);
            if (string.IsNullOrEmpty(stored)) return defaultValue;
            return int.TryParse(stored, out int result) ? result : defaultValue;
        }

        /// <summary>
        /// 암호화된 64비트 정수 값을 복호화하여 반환합니다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="defaultValue">키가 없을 때 반환할 기본값</param>
        /// <returns>복호화된 값</returns>
        public static long GetLong(string key, long defaultValue = 0L)
        {
            string stored = GetString(key, null);
            if (string.IsNullOrEmpty(stored)) return defaultValue;
            return long.TryParse(stored, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out long result) ? result : defaultValue;
        }

        /// <summary>
        /// 암호화된 실수 값을 복호화하여 반환합니다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="defaultValue">키가 없을 때 반환할 기본값</param>
        /// <returns>복호화된 값</returns>
        public static float GetFloat(string key, float defaultValue = 0f)
        {
            string stored = GetString(key, null);
            if (string.IsNullOrEmpty(stored)) return defaultValue;
            return float.TryParse(stored, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float result) ? result : defaultValue;
        }

        /// <summary>
        /// 암호화된 배정밀도 실수 값을 복호화하여 반환합니다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="defaultValue">키가 없을 때 반환할 기본값</param>
        /// <returns>복호화된 값</returns>
        public static double GetDouble(string key, double defaultValue = 0d)
        {
            string stored = GetString(key, null);
            if (string.IsNullOrEmpty(stored)) return defaultValue;
            return double.TryParse(stored, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double result) ? result : defaultValue;
        }

        /// <summary>
        /// 암호화된 Boolean 값을 복호화하여 반환합니다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="defaultValue">키가 없을 때 반환할 기본값</param>
        /// <returns>복호화된 값</returns>
        public static bool GetBool(string key, bool defaultValue = false)
        {
            string stored = GetString(key, null);
            if (string.IsNullOrEmpty(stored)) return defaultValue;
            return stored == "1";
        }
        #endregion // Get

        #region 관리
        /// <summary>
        /// 키 존재 여부를 확인합니다.
        /// </summary>
        /// <param name="key">확인할 키</param>
        /// <returns>존재하면 true</returns>
        public static bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(HashKey(key));
        }

        /// <summary>
        /// 특정 키를 삭제합니다.
        /// </summary>
        /// <param name="key">삭제할 키</param>
        public static void DeleteKey(string key)
        {
            DeleteKey(key, currentSlot);
        }

        /// <summary>현재 선택을 바꾸지 않고 지정한 슬롯에서 하나의 키를 삭제합니다.</summary>
        public static void DeleteKey(string key, string slot)
        {
            string selectedSlot = NormalizeSlot(slot);
            HashSet<string> keys = selectedSlot == currentSlot ? KeyIndex : ReadKeyIndex(selectedSlot);
            PlayerPrefs.DeleteKey(HashKey(key, selectedSlot));
            if (keys.Remove(key)) WriteKeyIndex(selectedSlot, keys);
        }

        /// <summary>
        /// 암호화된 모든 데이터를 삭제합니다.
        /// 일반 PlayerPrefs는 영향받지 않습니다.
        /// </summary>
        public static void DeleteAll()
        {
            foreach (var key in new List<string>(KeyIndex))
            {
                PlayerPrefs.DeleteKey(HashKey(key));
            }
            keyIndexCache?.Clear();
            PlayerPrefs.DeleteKey(KeyIndexName);
            PlayerPrefs.DeleteKey(GetKeyIndexName(currentSlot));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// PlayerPrefs를 디스크에 저장합니다.
        /// </summary>
        public static void Save()
        {
            PlayerPrefs.Save();
        }
        #endregion // 관리

        #region JSON Export / Import
        /// <summary>
        /// 관리 중인 모든 암호화 데이터를 JSON 문자열로 반환합니다.
        /// 클라우드 저장용으로 사용합니다.
        /// </summary>
        /// <returns>JSON 문자열 (복호화된 평문 상태)</returns>
        public static string ExportToJson(Predicate<string> keyFilter = null)
        {
            return ExportSlotToJson(currentSlot, keyFilter);
        }

        /// <summary>지정한 슬롯을 현재 선택 변경 없이 내보냅니다.</summary>
        public static string ExportSlotToJson(string slot, Predicate<string> keyFilter = null)
        {
            string selectedSlot = NormalizeSlot(slot);
            var data = new PrefsData { entries = new List<PrefsEntry>() };
            foreach (var key in ReadKeyIndex(selectedSlot))
            {
                if (keyFilter != null && !keyFilter(key)) continue;

                string value = GetString(key, null, selectedSlot);
                if (value != null)
                {
                    data.entries.Add(new PrefsEntry { key = key, value = value });
                }
            }
            return JsonUtility.ToJson(data);
        }

        /// <summary>
        /// JSON 문자열로부터 데이터를 복원합니다.
        /// 입력을 검증한 뒤 현재 슬롯을 교체합니다. 적용 오류가 나면 이전 값을 복구합니다.
        /// </summary>
        /// <param name="json">ExportToJson으로 생성된 JSON</param>
        /// <returns>복원 성공 여부</returns>
        public static bool ImportFromJson(string json)
        {
            return ImportFromJson(json, currentSlot);
        }

        /// <summary>모든 항목을 검증한 뒤 지정한 슬롯을 교체합니다. 적용 실패 시 이전 값을 복원합니다.</summary>
        public static bool ImportFromJson(string json, string slot) => ApplyJson(json, NormalizeSlot(slot), false);

        /// <summary>실제 저장값을 변경하지 않고 가져올 데이터와 암호화 가능 여부를 확인합니다.</summary>
        public static bool CanImportJson(string json) => TryPrepareValues(json, out _);

        /// <summary>
        /// JSON 문자열을 기존 데이터에 병합합니다. 동일 키는 덮어씁니다.
        /// </summary>
        /// <param name="json">ExportToJson으로 생성된 JSON</param>
        /// <returns>병합 성공 여부</returns>
        public static bool MergeFromJson(string json)
        {
            return ApplyJson(json, currentSlot, true);
        }

        /// <summary>현재 선택을 바꾸지 않고 지정한 슬롯에 검증된 값을 병합합니다.</summary>
        public static bool MergeFromJson(string json, string slot) => ApplyJson(json, NormalizeSlot(slot), true);

        /// <summary>항목 검증과 암호화를 저장소 변경 전에 완료합니다.</summary>
        private static bool TryPrepareValues(string json, out Dictionary<string, string> values)
        {
            values = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                PrefsData data = JsonUtility.FromJson<PrefsData>(json);
                if (data?.entries == null) return false;
                Dictionary<string, string> prepared = new(StringComparer.Ordinal);
                foreach (PrefsEntry entry in data.entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.key) || entry.value == null || prepared.ContainsKey(entry.key))
                        return false;
                    string encrypted = Encrypt(entry.value);
                    if (encrypted == null || entry.value.Length > 0 && encrypted.Length == 0) return false;
                    prepared.Add(entry.key, encrypted);
                }
                values = prepared;
                return true;
            }
            catch (Exception exception)
            {
                SWLog.LogError($"[SWPlayerPrefs] 가져오기 검증 실패: {exception.Message}");
                return false;
            }
        }

        /// <summary>실행 중 적용 오류가 발생하면 변경 대상 키와 이름 목록을 복원합니다.</summary>
        private static bool ApplyJson(string json, string slot, bool merge)
        {
            if (!TryPrepareValues(json, out Dictionary<string, string> values)) return false;
            Dictionary<string, string> previousValues = new(StringComparer.Ordinal);
            bool mutationStarted = false;
            try
            {
                HashSet<string> previousKeys = ReadKeyIndex(slot);
                HashSet<string> resultingKeys = merge ? new HashSet<string>(previousKeys) : new HashSet<string>();
                resultingKeys.UnionWith(values.Keys);
                HashSet<string> affectedKeys = new(previousKeys);
                affectedKeys.UnionWith(values.Keys);
                foreach (string key in affectedKeys)
                {
                    string encryptedKey = HashKey(key, slot);
                    previousValues[encryptedKey] = PlayerPrefs.HasKey(encryptedKey) ? PlayerPrefs.GetString(encryptedKey) : null;
                }
                string indexKey = GetKeyIndexName(slot);
                previousValues[indexKey] = PlayerPrefs.HasKey(indexKey) ? PlayerPrefs.GetString(indexKey) : null;
                string serializedIndex = JsonUtility.ToJson(new KeyIndexData { keys = new List<string>(resultingKeys) });
                mutationStarted = true;
                foreach (KeyValuePair<string, string> pair in values)
                    PlayerPrefs.SetString(HashKey(pair.Key, slot), pair.Value);
                foreach (string key in previousKeys)
                    if (!resultingKeys.Contains(key)) PlayerPrefs.DeleteKey(HashKey(key, slot));
                PlayerPrefs.SetString(indexKey, serializedIndex);
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception exception)
            {
                if (mutationStarted)
                {
                    try
                    {
                        foreach (KeyValuePair<string, string> pair in previousValues)
                            if (pair.Value == null) PlayerPrefs.DeleteKey(pair.Key);
                            else PlayerPrefs.SetString(pair.Key, pair.Value);
                        PlayerPrefs.Save();
                    }
                    catch (Exception rollbackException)
                    {
                        SWLog.LogError($"[SWPlayerPrefs] 이전 값 복원 실패: {rollbackException.Message}");
                    }
                }
                SWLog.LogError($"[SWPlayerPrefs] 가져오기 적용 실패: {exception.Message}");
                return false;
            }
            finally
            {
                if (slot == currentSlot) keyIndexCache = null;
            }
        }
        #endregion // JSON Export / Import
    }
}
