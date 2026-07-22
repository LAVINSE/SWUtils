using System;
using System.Collections.Generic;
using UnityEngine;

namespace SW.BehaviourTree
{
    /// <summary>Behaviour Tree 노드가 공유하는 이름 기반 런타임 데이터 저장소입니다.</summary>
    [Serializable]
    public sealed class SWBehaviourBlackboard
    {
        [SerializeReference] private List<SWBehaviourBlackboardEntry> entries = new();
        [NonSerialized] private Dictionary<string, SWBehaviourBlackboardEntry> entriesByName;

        /// <summary>직렬화된 Blackboard 항목 목록입니다.</summary>
        public IReadOnlyList<SWBehaviourBlackboardEntry> Entries => entries;

        /// <summary>지정한 이름의 값을 가져옵니다.</summary>
        public bool TryGetValue<T>(string name, out T value)
        {
            EnsureLookup();
            if (!string.IsNullOrWhiteSpace(name) &&
                entriesByName.TryGetValue(name, out SWBehaviourBlackboardEntry entry) &&
                entry.TryGetValue(out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>지정한 이름의 값을 가져오며 없으면 기본값을 반환합니다.</summary>
        public T GetValue<T>(string name, T defaultValue = default)
        {
            return TryGetValue(name, out T value) ? value : defaultValue;
        }

        /// <summary>기존 Blackboard 항목의 값을 변경합니다.</summary>
        public bool SetValue<T>(string name, T value)
        {
            EnsureLookup();
            return !string.IsNullOrWhiteSpace(name) &&
                entriesByName.TryGetValue(name, out SWBehaviourBlackboardEntry entry) &&
                entry.TrySetValue(value);
        }

        /// <summary>이름으로 Blackboard 항목을 찾아 반환합니다.</summary>
        public bool TryGetEntry(string name, out SWBehaviourBlackboardEntry entry)
        {
            EnsureLookup();
            if (!string.IsNullOrWhiteSpace(name))
                return entriesByName.TryGetValue(name, out entry);
            entry = null;
            return false;
        }

        /// <summary>반복 조회 비용을 줄이는 타입 안전 Key 참조를 반환합니다.</summary>
        public SWBehaviourBlackboardKey<T> FindKey<T>(string name)
        {
            return TryGetEntry(name, out SWBehaviourBlackboardEntry entry) &&
                typeof(T).IsAssignableFrom(entry.SystemValueType)
                ? new SWBehaviourBlackboardKey<T>(name, entry)
                : null;
        }

        /// <summary>새 Blackboard 항목을 추가합니다.</summary>
        public SWBehaviourBlackboardEntry Add(string name, SWBehaviourBlackboardValueType valueType)
        {
            EnsureLookup();
            string uniqueName = GetUniqueName(string.IsNullOrWhiteSpace(name) ? "New Key" : name.Trim());
            SWBehaviourBlackboardEntry entry = new(uniqueName, valueType);
            entries.Add(entry);
            entriesByName.Add(uniqueName, entry);
            return entry;
        }

        /// <summary>사용자 정의 Blackboard Key 타입을 생성하여 추가합니다.</summary>
        public SWBehaviourBlackboardEntry Add(string name, Type entryType)
        {
            if (entryType == null || entryType.IsAbstract ||
                !typeof(SWBehaviourBlackboardEntry).IsAssignableFrom(entryType))
                return null;
            EnsureLookup();
            SWBehaviourBlackboardEntry entry =
                Activator.CreateInstance(entryType) as SWBehaviourBlackboardEntry;
            if (entry == null)
                return null;
            string uniqueName = GetUniqueName(string.IsNullOrWhiteSpace(name) ? "New Key" : name.Trim());
            entry.Initialize(uniqueName);
            entries.Add(entry);
            entriesByName.Add(uniqueName, entry);
            return entry;
        }

        /// <summary>지정한 식별자의 Blackboard 항목을 제거합니다.</summary>
        public bool Remove(string identifier)
        {
            int index = entries.FindIndex(entry =>
                entry != null && entry.Identifier == identifier);
            if (index < 0)
                return false;

            entries.RemoveAt(index);
            RebuildLookup();
            return true;
        }

        /// <summary>직렬화 이후 빠른 이름 조회 테이블을 다시 만듭니다.</summary>
        public void RebuildLookup()
        {
            RemoveInvalidEntries();
            entriesByName = new Dictionary<string, SWBehaviourBlackboardEntry>(StringComparer.Ordinal);
            for (int index = 0; index < entries.Count; index++)
            {
                SWBehaviourBlackboardEntry entry = entries[index];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Name))
                    entriesByName[entry.Name] = entry;
            }
        }

        /// <summary>이전 직렬화 형식에서 남은 null Blackboard 항목을 제거합니다.</summary>
        public bool RemoveInvalidEntries()
        {
            entries ??= new List<SWBehaviourBlackboardEntry>();
            int removedCount = entries.RemoveAll(entry => entry == null);
            return removedCount > 0;
        }

        private void EnsureLookup()
        {
            if (entriesByName == null)
                RebuildLookup();
        }

        private string GetUniqueName(string requestedName)
        {
            string candidate = requestedName;
            int suffix = 2;
            while (entriesByName.ContainsKey(candidate))
                candidate = $"{requestedName} {suffix++}";
            return candidate;
        }
    }

    /// <summary>Blackboard가 기본 제공하는 값 종류입니다.</summary>
    public enum SWBehaviourBlackboardValueType
    {
        Boolean,
        Integer,
        Float,
        String,
        Vector2,
        Vector3,
        Object,
        Custom,
    }

    /// <summary>Blackboard에 직렬화되는 하나의 이름과 값입니다.</summary>
    [Serializable]
    public class SWBehaviourBlackboardEntry
    {
        [SerializeField] private string identifier;
        [SerializeField] private string name;
        [SerializeField] private SWBehaviourBlackboardValueType valueType;
        [SerializeField] private bool booleanValue;
        [SerializeField] private int integerValue;
        [SerializeField] private float floatValue;
        [SerializeField] private string stringValue;
        [SerializeField] private Vector2 vector2Value;
        [SerializeField] private Vector3 vector3Value;
        [SerializeField] private UnityEngine.Object objectValue;

        /// <summary>Blackboard 항목의 고유 식별자입니다.</summary>
        public string Identifier => identifier;

        /// <summary>노드에서 값을 찾을 때 사용하는 이름입니다.</summary>
        public string Name { get => name; set => name = value; }

        /// <summary>저장된 값의 종류입니다.</summary>
        public virtual SWBehaviourBlackboardValueType ValueType => valueType;

        /// <summary>NodeProperty와 Editor가 타입 호환성을 검사할 실제 값 타입입니다.</summary>
        public virtual Type SystemValueType => valueType switch
        {
            SWBehaviourBlackboardValueType.Boolean => typeof(bool),
            SWBehaviourBlackboardValueType.Integer => typeof(int),
            SWBehaviourBlackboardValueType.Float => typeof(float),
            SWBehaviourBlackboardValueType.String => typeof(string),
            SWBehaviourBlackboardValueType.Vector2 => typeof(Vector2),
            SWBehaviourBlackboardValueType.Vector3 => typeof(Vector3),
            SWBehaviourBlackboardValueType.Object => typeof(UnityEngine.Object),
            _ => typeof(object),
        };

        /// <summary>사용자 정의 Key 직렬화를 위한 기본 생성자입니다.</summary>
        protected SWBehaviourBlackboardEntry()
        {
            identifier = Guid.NewGuid().ToString("N");
            name = "New Key";
            valueType = SWBehaviourBlackboardValueType.Custom;
        }

        /// <summary>Blackboard 항목을 생성합니다.</summary>
        public SWBehaviourBlackboardEntry(string name, SWBehaviourBlackboardValueType valueType)
        {
            identifier = Guid.NewGuid().ToString("N");
            this.name = name;
            this.valueType = valueType;
            stringValue = string.Empty;
        }

        /// <summary>요청한 타입과 저장 타입이 일치하면 값을 반환합니다.</summary>
        public virtual bool TryGetValue<T>(out T value)
        {
            object boxedValue = valueType switch
            {
                SWBehaviourBlackboardValueType.Boolean => booleanValue,
                SWBehaviourBlackboardValueType.Integer => integerValue,
                SWBehaviourBlackboardValueType.Float => floatValue,
                SWBehaviourBlackboardValueType.String => stringValue,
                SWBehaviourBlackboardValueType.Vector2 => vector2Value,
                SWBehaviourBlackboardValueType.Vector3 => vector3Value,
                SWBehaviourBlackboardValueType.Object => objectValue,
                _ => null,
            };
            if (boxedValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return boxedValue == null && default(T) is null;
        }

        /// <summary>요청한 타입과 저장 타입이 일치하면 값을 변경합니다.</summary>
        public virtual bool TrySetValue<T>(T value)
        {
            switch (valueType)
            {
                case SWBehaviourBlackboardValueType.Boolean when value is bool typedValue:
                    booleanValue = typedValue; return true;
                case SWBehaviourBlackboardValueType.Integer when value is int typedValue:
                    integerValue = typedValue; return true;
                case SWBehaviourBlackboardValueType.Float when value is float typedValue:
                    floatValue = typedValue; return true;
                case SWBehaviourBlackboardValueType.String when value is string typedValue:
                    stringValue = typedValue; return true;
                case SWBehaviourBlackboardValueType.Vector2 when value is Vector2 typedValue:
                    vector2Value = typedValue; return true;
                case SWBehaviourBlackboardValueType.Vector3 when value is Vector3 typedValue:
                    vector3Value = typedValue; return true;
                case SWBehaviourBlackboardValueType.Object when value is UnityEngine.Object typedValue:
                    objectValue = typedValue; return true;
                default:
                    return false;
            }
        }

        /// <summary>Override와 Editor에서 사용할 박싱된 값을 반환합니다.</summary>
        public virtual object GetBoxedValue()
        {
            return valueType switch
            {
                SWBehaviourBlackboardValueType.Boolean => booleanValue,
                SWBehaviourBlackboardValueType.Integer => integerValue,
                SWBehaviourBlackboardValueType.Float => floatValue,
                SWBehaviourBlackboardValueType.String => stringValue,
                SWBehaviourBlackboardValueType.Vector2 => vector2Value,
                SWBehaviourBlackboardValueType.Vector3 => vector3Value,
                SWBehaviourBlackboardValueType.Object => objectValue,
                _ => null,
            };
        }

        /// <summary>Override에서 전달한 박싱된 값으로 Key를 변경합니다.</summary>
        public virtual bool TrySetBoxedValue(object value)
        {
            return valueType switch
            {
                SWBehaviourBlackboardValueType.Boolean when value is bool typedValue =>
                    TrySetValue(typedValue),
                SWBehaviourBlackboardValueType.Integer when value is int typedValue =>
                    TrySetValue(typedValue),
                SWBehaviourBlackboardValueType.Float when value is float typedValue =>
                    TrySetValue(typedValue),
                SWBehaviourBlackboardValueType.String when value is string typedValue =>
                    TrySetValue(typedValue),
                SWBehaviourBlackboardValueType.Vector2 when value is Vector2 typedValue =>
                    TrySetValue(typedValue),
                SWBehaviourBlackboardValueType.Vector3 when value is Vector3 typedValue =>
                    TrySetValue(typedValue),
                SWBehaviourBlackboardValueType.Object when value is UnityEngine.Object typedValue =>
                    TrySetValue(typedValue),
                _ => false,
            };
        }

        internal void Initialize(string entryName)
        {
            identifier = Guid.NewGuid().ToString("N");
            name = entryName;
        }
    }

    /// <summary>사용자 정의 값 타입을 저장하는 Blackboard Key 기반 클래스입니다.</summary>
    [Serializable]
    public abstract class SWBehaviourBlackboardEntry<T> : SWBehaviourBlackboardEntry
    {
        [SerializeField] private T value;

        public T Value { get => value; set => this.value = value; }
        public override SWBehaviourBlackboardValueType ValueType => SWBehaviourBlackboardValueType.Custom;
        public override Type SystemValueType => typeof(T);

        public override bool TryGetValue<TValue>(out TValue result)
        {
            if (value is TValue typedValue)
            {
                result = typedValue;
                return true;
            }
            result = default;
            return value is null && default(TValue) is null;
        }

        public override bool TrySetValue<TValue>(TValue changedValue)
        {
            if (changedValue is T typedValue)
            {
                value = typedValue;
                return true;
            }
            return false;
        }

        public override object GetBoxedValue() => value;

        public override bool TrySetBoxedValue(object changedValue)
        {
            if (changedValue is not T typedValue)
                return false;
            value = typedValue;
            return true;
        }
    }
}
