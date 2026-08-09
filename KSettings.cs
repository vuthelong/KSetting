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

        private static KSettingsData _data;

        #endregion

        #region Property

        private static KSettingsData Data => _data ? _data : _data = KData.LoadOrCreate<KSettingsData>(FileName);

        #endregion

        #region Setting Access

        public static bool HasBool(string key) => Data.boolKeys.Contains(key);

        public static bool HasInt(string key) => Data.intKeys.Contains(key);

        public static bool HasFloat(string key) => Data.floatKeys.Contains(key);

        public static bool GetBool(string key, bool defaultValue = false) => Get(Data.boolKeys, Data.bools, key, defaultValue);

        public static int GetInt(string key, int defaultValue = 0) => Get(Data.intKeys, Data.ints, key, defaultValue);

        public static float GetFloat(string key, float defaultValue = 0) => Get(Data.floatKeys, Data.floats, key, defaultValue);

        public static void SetBool(string key, bool value) => Set(Data.boolKeys, Data.bools, key, value);

        public static void SetInt(string key, int value) => Set(Data.intKeys, Data.ints, key, value);

        public static void SetFloat(string key, float value) => Set(Data.floatKeys, Data.floats, key, value);

        private static T Get<T>(List<string> keys, List<T> values, string key, T defaultValue)
        {
            var i = keys.IndexOf(key);

            return i != -1 && i < values.Count ? values[i] : defaultValue;
        }

        private static void Set<T>(List<string> keys, List<T> values, string key, T value)
        {
            var i = keys.IndexOf(key);

            if (i == -1 || i >= values.Count)
            {
                keys.Add(key);
                values.Add(value);

                Save();

                return;
            }

            if (EqualityComparer<T>.Default.Equals(values[i], value)) return;

            values[i] = value;

            Save();
        }

        #endregion

        #region Persistence

        public static void Save() => KData.Save(Data, FileName);

        public static void RemoveByPrefix(string prefix, List<string> removedKeys)
        {
            var removed = Remove(Data.boolKeys, Data.bools, prefix, removedKeys);

            removed |= Remove(Data.intKeys, Data.ints, prefix, removedKeys);
            removed |= Remove(Data.floatKeys, Data.floats, prefix, removedKeys);

            if (!removed) return;

            Save();
        }

        private static bool Remove<T>(List<string> keys, List<T> values, string prefix, List<string> removedKeys)
        {
            var removed = false;

            for (var i = keys.Count - 1; i >= 0; i--)
            {
                if (!keys[i].StartsWith(prefix, StringComparison.Ordinal)) continue;

                removedKeys?.Add(keys[i]);

                keys.RemoveAt(i);

                if (i < values.Count)
                {
                    values.RemoveAt(i);
                }

                removed = true;
            }

            return removed;
        }

        #endregion
    }

    public class KSettingsData : ScriptableObject
    {
        #region Field

        public List<string> boolKeys = new();
        public List<bool> bools = new();

        public List<string> intKeys = new();
        public List<int> ints = new();

        public List<string> floatKeys = new();
        public List<float> floats = new();

        #endregion
    }
}
#endif
