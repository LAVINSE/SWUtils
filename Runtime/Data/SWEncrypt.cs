using System;

namespace SW.Data
{
    /// <summary>
    /// SWPlayerPrefs에 저장되는 단일 값을 변수처럼 다루는 래퍼.
    /// 실제 암호화와 디스크 저장은 SWPlayerPrefs가 담당합니다.
    /// </summary>
    /// <typeparam name="T">저장 값 타입</typeparam>
    [Serializable]
    public sealed class SWEncrypt<T>
    {
        /// <summary>
        /// 저장에 사용할 키.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// 저장된 값이 없거나 변환에 실패했을 때 사용할 기본값.
        /// </summary>
        public T DefaultValue { get; }

        /// <summary>
        /// 현재 슬롯에서 값을 읽거나 씁니다.
        /// 값 설정 후 즉시 디스크에 반영하려면 Save를 호출합니다.
        /// </summary>
        public T Value
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// 저장 키와 기본값을 설정합니다.
        /// </summary>
        /// <param name="key">저장에 사용할 키</param>
        /// <param name="defaultValue">기본값</param>
        public SWEncrypt(string key, T defaultValue)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("저장 키는 비어 있을 수 없습니다.", nameof(key));

            EnsureSupportedType();

            Key = key.Trim();
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// 현재 슬롯에 값이 저장되어 있는지 확인합니다.
        /// </summary>
        public bool HasValue()
        {
            return SWPlayerPrefs.HasKey(Key);
        }

        /// <summary>
        /// 현재 슬롯에서 값을 읽습니다.
        /// </summary>
        public T Get()
        {
            Type valueType = typeof(T);

            if (valueType == typeof(int))
                return (T)(object)SWPlayerPrefs.GetInt(Key, (int)(object)DefaultValue);

            if (valueType == typeof(long))
                return (T)(object)SWPlayerPrefs.GetLong(Key, (long)(object)DefaultValue);

            if (valueType == typeof(float))
                return (T)(object)SWPlayerPrefs.GetFloat(Key, (float)(object)DefaultValue);

            if (valueType == typeof(double))
                return (T)(object)SWPlayerPrefs.GetDouble(Key, (double)(object)DefaultValue);

            if (valueType == typeof(bool))
                return (T)(object)SWPlayerPrefs.GetBool(Key, (bool)(object)DefaultValue);

            if (valueType == typeof(string))
                return (T)(object)SWPlayerPrefs.GetString(Key, (string)(object)DefaultValue);

            throw CreateUnsupportedTypeException();
        }

        /// <summary>
        /// 현재 슬롯에서 값을 읽습니다.
        /// Get과 동일하며, 저장 흐름에서 읽기 의도를 드러낼 때 사용합니다.
        /// </summary>
        public T Load()
        {
            return Get();
        }

        /// <summary>
        /// 현재 슬롯에 값을 저장합니다.
        /// 디스크 반영은 Save 호출 시점에 수행됩니다.
        /// </summary>
        /// <param name="value">저장할 값</param>
        public void Set(T value)
        {
            Type valueType = typeof(T);

            if (valueType == typeof(int))
            {
                SWPlayerPrefs.SetInt(Key, (int)(object)value);
                return;
            }

            if (valueType == typeof(long))
            {
                SWPlayerPrefs.SetLong(Key, (long)(object)value);
                return;
            }

            if (valueType == typeof(float))
            {
                SWPlayerPrefs.SetFloat(Key, (float)(object)value);
                return;
            }

            if (valueType == typeof(double))
            {
                SWPlayerPrefs.SetDouble(Key, (double)(object)value);
                return;
            }

            if (valueType == typeof(bool))
            {
                SWPlayerPrefs.SetBool(Key, (bool)(object)value);
                return;
            }

            if (valueType == typeof(string))
            {
                SWPlayerPrefs.SetString(Key, (string)(object)value);
                return;
            }

            throw CreateUnsupportedTypeException();
        }

        /// <summary>
        /// 값을 저장한 뒤 즉시 디스크에 반영합니다.
        /// </summary>
        /// <param name="value">저장할 값</param>
        public void SetAndSave(T value)
        {
            Set(value);
            Save();
        }

        /// <summary>
        /// 현재 슬롯에서 저장 값을 삭제합니다.
        /// 디스크 반영은 Save 호출 시점에 수행됩니다.
        /// </summary>
        public void Delete()
        {
            SWPlayerPrefs.DeleteKey(Key);
        }

        /// <summary>
        /// 현재까지 변경된 PlayerPrefs 값을 디스크에 반영합니다.
        /// </summary>
        public void Save()
        {
            SWPlayerPrefs.Save();
        }

        /// <summary>
        /// 지원하지 않는 타입이면 예외를 발생시킨다.
        /// </summary>
        private static void EnsureSupportedType()
        {
            Type valueType = typeof(T);
            if (valueType == typeof(int)
                || valueType == typeof(long)
                || valueType == typeof(float)
                || valueType == typeof(double)
                || valueType == typeof(bool)
                || valueType == typeof(string))
            {
                return;
            }

            throw CreateUnsupportedTypeException();
        }

        /// <summary>
        /// 지원하지 않는 타입 예외를 생성합니다.
        /// </summary>
        private static NotSupportedException CreateUnsupportedTypeException()
        {
            return new NotSupportedException(
                $"SWEncrypt<{typeof(T).Name}>는 지원하지 않는 타입입니다. int, long, float, double, bool, string만 사용할 수 있습니다.");
        }
    }
}
