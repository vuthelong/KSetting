#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kingfisher.KSetting
{
    public static class KSettings
    {
        #region Field

        public const string FileName = "KSettings.asset";

        private static readonly List<string> RemovalKeys = new();

        private static KSettingsData _data;

        private static int _deferDepth;
        private static bool _isDirty;

        #endregion

        #region Property

        private static KSettingsData Data => _data ? _data : _data = KData.LoadOrCreate<KSettingsData>(FileName);

        #endregion

        #region Setting Access

        public static bool HasBool(string key) => Data.bools.ContainsKey(key);

        public static bool HasInt(string key) => Data.ints.ContainsKey(key);

        public static bool HasFloat(string key) => Data.floats.ContainsKey(key);

        public static bool GetBool(string key, bool defaultValue = false) => Get(Data.bools, key, defaultValue);

        public static int GetInt(string key, int defaultValue = 0) => Get(Data.ints, key, defaultValue);

        public static float GetFloat(string key, float defaultValue = 0f) => Get(Data.floats, key, defaultValue);

        public static void SetBool(string key, bool value) => Set(Data.bools, key, value);

        public static void SetInt(string key, int value) => Set(Data.ints, key, value);

        public static void SetFloat(string key, float value) => Set(Data.floats, key, value);

        private static T Get<T>(SerializableDictionary<string, T> values, string key, T defaultValue) => values.TryGetValue(key, out var value) ? value : defaultValue;

        private static void Set<T>(SerializableDictionary<string, T> values, string key, T value)
        {
            if (values.TryGetValue(key, out var existing))
            {
                if (EqualityComparer<T>.Default.Equals(existing, value)) return;

                values[key] = value;

                RequestSave();

                return;
            }

            values.Add(key, value);

            RequestSave();
        }

        #endregion

        #region Persistence

        public static void Save() => KData.Save(Data, FileName);

        public static void BeginDeferredSave() => _deferDepth++;

        public static void EndDeferredSave()
        {
            if (_deferDepth > 0)
            {
                _deferDepth--;
            }

            if (_deferDepth > 0 || !_isDirty) return;

            _isDirty = false;

            Save();
        }

        private static void RequestSave()
        {
            if (_deferDepth > 0)
            {
                _isDirty = true;

                return;
            }

            Save();
        }

        public static void RemoveByPrefix(string prefix, List<string> removedKeys)
        {
            var removed = Remove(Data.bools, prefix, removedKeys);

            removed |= Remove(Data.ints, prefix, removedKeys);
            removed |= Remove(Data.floats, prefix, removedKeys);

            if (!removed) return;

            Save();
        }

        private static bool Remove<T>(SerializableDictionary<string, T> values, string prefix, List<string> removedKeys)
        {
            var removed = false;

            RemovalKeys.Clear();

            foreach (var key in values.Keys)
            {
                if (!key.StartsWith(prefix, StringComparison.Ordinal)) continue;

                RemovalKeys.Add(key);
            }

            for (var i = 0; i < RemovalKeys.Count; i++)
            {
                removedKeys?.Add(RemovalKeys[i]);

                values.Remove(RemovalKeys[i]);

                removed = true;
            }

            return removed;
        }

        #endregion
    }

    public class KSettingsData : ScriptableObject
    {
        #region Field

        public SerializableDictionary<string, bool> bools = new();

        public SerializableDictionary<string, int> ints = new();

        public SerializableDictionary<string, float> floats = new();

        #endregion
    }

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        #region Field

        [SerializeField] private List<TKey> keys = new();

        [SerializeField] private List<TValue> values = new();

        #endregion

        #region Method

        public void OnBeforeSerialize()
        {
            this.keys.Clear();
            this.values.Clear();

            if (this.keys.Capacity < Count)
            {
                this.keys.Capacity = Count;
            }

            if (this.values.Capacity < Count)
            {
                this.values.Capacity = Count;
            }

            foreach (var pair in this)
            {
                this.keys.Add(pair.Key);
                this.values.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();

            for (var i = 0; i < this.keys.Count; i++)
            {
                this[this.keys[i]] = this.values[i];
            }
        }

        #endregion
    }
}
#endif
