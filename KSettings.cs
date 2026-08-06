#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Kingfisher.KSetting
{
    public static class KSettings
    {
        #region Fields

        public const string FileName = "KSettings.asset";

        private static KSettingsData _data;

        #endregion

        #region Properties

        private static KSettingsData Data => _data ? _data : _data = KData.LoadOrCreate<KSettingsData>(FileName);

        #endregion

        #region Public Methods

        public static bool HasBool(string key) => Data.boolKeys.Contains(key);

        public static bool HasInt(string key) => Data.intKeys.Contains(key);

        public static bool HasFloat(string key) => Data.floatKeys.Contains(key);

        public static bool GetBool(string key, bool defaultValue = false) => Get(Data.boolKeys, Data.bools, key, defaultValue);

        public static int GetInt(string key, int defaultValue = 0) => Get(Data.intKeys, Data.ints, key, defaultValue);

        public static float GetFloat(string key, float defaultValue = 0) => Get(Data.floatKeys, Data.floats, key, defaultValue);

        public static void SetBool(string key, bool value) => Set(Data.boolKeys, Data.bools, key, value);

        public static void SetInt(string key, int value) => Set(Data.intKeys, Data.ints, key, value);

        public static void SetFloat(string key, float value) => Set(Data.floatKeys, Data.floats, key, value);

        public static void Save() => KData.Save(Data, FileName);

        #endregion

        #region Private Methods

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
    }

    public class KSettingsData : ScriptableObject
    {
        #region Fields

        // bools
        public List<string> boolKeys = new();
        public List<bool> bools = new();

        // ints
        public List<string> intKeys = new();
        public List<int> ints = new();

        // floats
        public List<string> floatKeys = new();
        public List<float> floats = new();

        #endregion
    }
}
#endif
