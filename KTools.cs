#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Kingfisher.KSetting
{
    public static class KTools
    {
        #region Field

        private const string MenuTypeNameFormat = "Kingfisher.{0}.{0}Menu, Kingfisher.{0}";

        public static readonly string[] ToolNames = { "KFolders", "KHierarchy", "KInspector", "KFavorites", "KReference", "KTabs", "KEmoji" };

        private static List<KTool> _installed;

        #endregion

        #region Property

        public static List<KTool> Installed => _installed ??= Discover();

        #endregion

        #region Method

        public static void Rediscover() => _installed = null;

        private static List<KTool> Discover()
        {
            var tools = new List<KTool>();

            for (var i = 0; i < ToolNames.Length; i++)
            {
                var name = ToolNames[i];

                if (Type.GetType(string.Format(MenuTypeNameFormat, name)) is not { } menuType) continue;

                tools.Add(new KTool(name, menuType));
            }

            return tools;
        }

        #endregion
    }

    public class KTool
    {
        #region Field

        private const string DisabledPropertyName = "PluginDisabled";
        private const string LayoutFieldName = "SettingsLayout";
        private const string DeleteDataMethodName = "DeleteData";
        private const string OpenToolMethodName = "OpenTool";
        private const string DataPathPropertyName = "DataPath";
        private const string OtherSectionTitle = "Other";
        private const string SettingKeySeparator = "-";
        private const string LegacyKeyPrefix = "v";
        private const string CurrentKeyInfix = "-kingfisher-";
        private const string LegacyKeyInfix = "-vtools-";

        private const char PathSeparator = '/';
        private const char WindowsPathSeparator = '\\';

        private const char SectionMarker = '#';
        private const char SliderMarker = '~';
        private const char ChoiceMarker = '*';
        private const char ColorMarker = '&';
        private const char LabelSeparator = '|';

        private const int ColorNameIndex = 0;
        private const int ColorLabelIndex = 1;
        private const int ColorPartCount = 2;

        private const int MarkerLength = 1;
        private const int SeparatorLength = 1;
        private const int LegacyKeyOffset = 1;
        private const int NotFoundIndex = -1;
        private const int SliderPartCount = 4;
        private const int SliderNameIndex = 0;
        private const int SliderLabelIndex = 1;
        private const int SliderMinIndex = 2;
        private const int SliderMaxIndex = 3;

        private readonly MethodInfo _deleteDataMethod;
        private readonly MethodInfo _openToolMethod;
        private readonly PropertyInfo _dataPathProperty;

        #endregion

        #region Property

        public string Name { get; }

        public List<KToolSection> Sections { get; } = new();

        public KToolSetting DisabledSetting { get; }

        public bool CanDeleteData => this._deleteDataMethod != null;

        public bool CanOpenTool => this._openToolMethod != null;

        public string DataPath => this._dataPathProperty?.GetValue(null) as string;

        public string DataFolder => DataPath is { } path ? Path.GetDirectoryName(path)?.Replace(WindowsPathSeparator, PathSeparator) : KData.RelativeFolderPath;

        #endregion

        #region Section Building

        public KTool(string name, Type menuType)
        {
            Name = name;

            this._deleteDataMethod = menuType.GetMethod(DeleteDataMethodName, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            this._openToolMethod = menuType.GetMethod(OpenToolMethodName, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            this._dataPathProperty = menuType.GetProperty(DataPathPropertyName, BindingFlags.Public | BindingFlags.Static);

            var propertiesByName = new Dictionary<string, PropertyInfo>();
            var declarationOrder = new List<string>();

            foreach (var property in menuType.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (!property.CanRead || !property.CanWrite) continue;

                var isBool = property.PropertyType == typeof(bool);

                if (!isBool && property.PropertyType != typeof(float) && property.PropertyType != typeof(Color)) continue;

                if (isBool && property.Name == DisabledPropertyName)
                {
                    DisabledSetting = new KToolSetting(property);

                    continue;
                }

                propertiesByName[property.Name] = property;

                if (isBool)
                {
                    declarationOrder.Add(property.Name);
                }
            }

            BuildSections(menuType, propertiesByName, declarationOrder);
        }

        private void BuildSections(Type menuType, Dictionary<string, PropertyInfo> propertiesByName, List<string> declarationOrder)
        {
            var layout = menuType.GetField(LayoutFieldName, BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string[];

            if (layout == null)
            {
                AddSection(null, declarationOrder, propertiesByName);

                return;
            }

            var placed = new HashSet<string>();

            ParseLayout(layout, propertiesByName, placed);
            AddSection(OtherSectionTitle, GetLeftovers(declarationOrder, placed), propertiesByName);
        }

        private void ParseLayout(string[] layout, Dictionary<string, PropertyInfo> propertiesByName, HashSet<string> placed)
        {
            var section = new KToolSection(null);

            KToolEntry choice = null;

            for (var i = 0; i < layout.Length; i++)
            {
                var line = layout[i];

                if (string.IsNullOrEmpty(line)) continue;

                if (line[0] == SectionMarker)
                {
                    AddFilledSection(section);

                    section = new KToolSection(line.Substring(MarkerLength).Trim());
                    choice = null;

                    continue;
                }

                if (line[0] == SliderMarker)
                {
                    AddSlider(section, line.Substring(MarkerLength), propertiesByName, placed);

                    choice = null;

                    continue;
                }

                if (line[0] == ColorMarker)
                {
                    AddColor(section, line.Substring(MarkerLength), propertiesByName, placed);

                    choice = null;

                    continue;
                }

                var isChoice = line[0] == ChoiceMarker;
                var body = SplitLabel(isChoice ? line.Substring(MarkerLength).Trim() : line.Trim(), out var label);

                if (!propertiesByName.TryGetValue(body, out var property)) continue;

                if (property.PropertyType != typeof(bool)) continue;

                placed.Add(body);

                var setting = new KToolSetting(property, label);

                if (!isChoice)
                {
                    section.Entries.Add(new KToolEntry(setting));
                    choice = null;

                    continue;
                }

                if (choice == null)
                {
                    choice = new KToolEntry();

                    section.Entries.Add(choice);
                }

                choice.Settings.Add(setting);
            }

            AddFilledSection(section);
        }

        private static string SplitLabel(string body, out string label)
        {
            var separator = body.IndexOf(LabelSeparator);

            if (separator == NotFoundIndex)
            {
                label = null;

                return body;
            }

            label = body.Substring(separator + SeparatorLength).Trim();

            return body.Substring(0, separator).Trim();
        }

        private static List<string> GetLeftovers(List<string> declarationOrder, HashSet<string> placed)
        {
            var leftovers = new List<string>();

            for (var i = 0; i < declarationOrder.Count; i++)
            {
                if (placed.Contains(declarationOrder[i])) continue;

                leftovers.Add(declarationOrder[i]);
            }

            return leftovers;
        }

        private static void AddSlider(KToolSection section, string body, Dictionary<string, PropertyInfo> propertiesByName, HashSet<string> placed)
        {
            var parts = body.Split(LabelSeparator);

            if (parts.Length < SliderPartCount) return;

            var propertyName = parts[SliderNameIndex].Trim();

            if (!propertiesByName.TryGetValue(propertyName, out var property)) return;

            if (!float.TryParse(parts[SliderMinIndex].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var min)) return;

            if (!float.TryParse(parts[SliderMaxIndex].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var max)) return;

            placed.Add(propertyName);

            section.Entries.Add(new KToolEntry(new KToolSetting(property, parts[SliderLabelIndex].Trim(), min, max)));
        }

        private static void AddColor(KToolSection section, string body, Dictionary<string, PropertyInfo> propertiesByName, HashSet<string> placed)
        {
            var parts = body.Split(LabelSeparator);

            var propertyName = parts[ColorNameIndex].Trim();
            var label = parts.Length >= ColorPartCount ? parts[ColorLabelIndex].Trim() : null;

            if (!propertiesByName.TryGetValue(propertyName, out var property)) return;

            if (property.PropertyType != typeof(Color)) return;

            placed.Add(propertyName);

            section.Entries.Add(new KToolEntry(new KToolSetting(property, label, isColor: true)));
        }

        private void AddSection(string title, List<string> propertyNames, Dictionary<string, PropertyInfo> propertiesByName)
        {
            if (propertyNames.Count == 0) return;

            var section = new KToolSection(title);

            for (var i = 0; i < propertyNames.Count; i++)
            {
                section.Entries.Add(new KToolEntry(new KToolSetting(propertiesByName[propertyNames[i]])));
            }

            Sections.Add(section);
        }

        private void AddFilledSection(KToolSection section)
        {
            if (section.Entries.Count == 0) return;

            Sections.Add(section);
        }

        #endregion

        #region Stored Data

        public void DeleteData() => this._deleteDataMethod?.Invoke(null, null);

        public void CollectStoredDataFiles(List<string> results)
        {
            results.Clear();

            if (DataPath is { } dataPath)
            {
                if (File.Exists(dataPath))
                {
                    results.Add(Path.GetFileName(dataPath));
                }

                return;
            }

            if (!Directory.Exists(KData.FolderPath)) return;

            var filePaths = Directory.GetFiles(KData.FolderPath);

            for (var i = 0; i < filePaths.Length; i++)
            {
                var fileName = Path.GetFileName(filePaths[i]);

                if (!fileName.StartsWith(Name, StringComparison.Ordinal)) continue;

                results.Add(fileName);
            }
        }

        #endregion

        #region Setting Reset

        public void ResetSettings()
        {
            var removedKeys = new List<string>();

            KSettings.RemoveByPrefix(Name + SettingKeySeparator, removedKeys);

            for (var i = 0; i < removedKeys.Count; i++)
            {
                DeleteEditorPrefsKey(removedKeys[i]);
            }
        }

        private static void DeleteEditorPrefsKey(string key)
        {
            EditorPrefs.DeleteKey(key);

            var previous = key.Substring(LegacyKeyOffset);

            EditorPrefs.DeleteKey(previous);
            EditorPrefs.DeleteKey(LegacyKeyPrefix + previous.Replace(CurrentKeyInfix, LegacyKeyInfix));
        }

        #endregion

        #region Method

        public void OpenTool() => this._openToolMethod?.Invoke(null, null);

        #endregion
    }

    public class KToolSection
    {
        #region Property

        public string Title { get; }

        public List<KToolEntry> Entries { get; } = new();

        #endregion

        #region Method

        public KToolSection(string title) => Title = title;

        #endregion
    }

    public class KToolEntry
    {
        #region Field

        private readonly bool _forcedChoice;

        #endregion

        #region Property

        public List<KToolSetting> Settings { get; } = new();

        public bool IsChoice => Settings.Count > 1 || this._forcedChoice;

        #endregion

        #region Method

        public KToolEntry(KToolSetting single) => Settings.Add(single);

        public KToolEntry() => this._forcedChoice = true;

        #endregion
    }

    public class KToolSetting
    {
        #region Field

        private const string EnabledSuffix = "Enabled";

        private const char WordSeparator = ' ';

        private Func<bool> _getValue;
        private Action<bool> _setValue;
        private Func<float> _getSliderValue;
        private Action<float> _setSliderValue;
        private Func<Color> _getColorValue;
        private Action<Color> _setColorValue;

        #endregion

        #region Property

        public string Label { get; }

        public bool IsSlider { get; }

        public bool IsColor { get; }

        public float Min { get; }

        public float Max { get; }

        public Color ColorValue
        {
            get => this._getColorValue();
            set => this._setColorValue(value);
        }

        public bool Value
        {
            get => this._getValue();
            set => this._setValue(value);
        }

        public float SliderValue
        {
            get => this._getSliderValue();
            set => this._setSliderValue(value);
        }

        #endregion

        #region Method

        public KToolSetting(PropertyInfo property, string label = null)
        {
            Label = label ?? Prettify(property.Name);

            BindProperty(property);
        }

        public KToolSetting(PropertyInfo property, string label, bool isColor)
        {
            Label = label ?? Prettify(property.Name);
            IsColor = isColor;

            BindProperty(property);
        }

        public KToolSetting(PropertyInfo property, string label, float min, float max)
        {
            Label = label ?? Prettify(property.Name);
            IsSlider = true;
            Min = min;
            Max = max;

            BindProperty(property);
        }

        private void BindProperty(PropertyInfo property)
        {
            if (property.PropertyType == typeof(bool))
            {
                this._getValue = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), property.GetGetMethod());
                this._setValue = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), property.GetSetMethod());

                return;
            }

            if (property.PropertyType == typeof(float))
            {
                this._getSliderValue = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), property.GetGetMethod());
                this._setSliderValue = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), property.GetSetMethod());

                return;
            }

            this._getColorValue = (Func<Color>)Delegate.CreateDelegate(typeof(Func<Color>), property.GetGetMethod());
            this._setColorValue = (Action<Color>)Delegate.CreateDelegate(typeof(Action<Color>), property.GetSetMethod());
        }

        private static string Prettify(string propertyName)
        {
            if (propertyName.EndsWith(EnabledSuffix, StringComparison.Ordinal))
            {
                propertyName = propertyName.Substring(0, propertyName.Length - EnabledSuffix.Length);
            }

            var builder = new StringBuilder();

            for (var i = 0; i < propertyName.Length; i++)
            {
                var character = propertyName[i];

                if (i == 0)
                {
                    builder.Append(char.ToUpperInvariant(character));

                    continue;
                }

                if (char.IsUpper(character) && !char.IsUpper(propertyName[i - 1]))
                {
                    builder.Append(WordSeparator).Append(char.ToLowerInvariant(character));
                }
                else
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        #endregion
    }
}
#endif
