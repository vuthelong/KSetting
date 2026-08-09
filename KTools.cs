#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;

namespace Kingfisher.KSetting
{
    public static class KTools
    {
        #region Field

        private const string MenuTypeNameFormat = "Kingfisher.{0}.{0}Menu, Kingfisher.{0}";

        public static readonly string[] ToolNames = { "KFolders", "KHierarchy", "KInspector", "KFavorites", "KTabs", "KEmoji" };

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
        private const char LabelSeparator = '|';

        private const int MarkerLength = 1;
        private const int LegacyKeyOffset = 1;
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

        // K-Tabs keeps nothing of its own in .KData, so it gets no delete button.
        public bool CanDeleteData => this._deleteDataMethod != null;

        // Only the tools with a window of their own get an open button.
        public bool CanOpenTool => this._openToolMethod != null;

        // Tools whose data is project content keep it in the project instead of .KData.
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

                if (!isBool && property.PropertyType != typeof(float)) continue;

                if (isBool && property.Name == DisabledPropertyName)
                {
                    DisabledSetting = new KToolSetting(property);

                    continue;
                }

                propertiesByName[property.Name] = property;

                // Sliders are only drawn where the layout asks for one, so they stay out of the leftover "Other" section.
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
            var section = new KToolSection(null);

            KToolEntry choice = null;

            for (var i = 0; i < layout.Length; i++)
            {
                var line = layout[i];

                if (string.IsNullOrEmpty(line)) continue;

                if (line[0] == SectionMarker)
                {
                    if (section.Entries.Count != 0)
                    {
                        Sections.Add(section);
                    }

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

                var isChoice = line[0] == ChoiceMarker;
                var body = isChoice ? line.Substring(MarkerLength).Trim() : line.Trim();

                string label = null;

                if (body.IndexOf(LabelSeparator) is var pipe && pipe != -1)
                {
                    label = body.Substring(pipe + 1).Trim();
                    body = body.Substring(0, pipe).Trim();
                }

                if (!propertiesByName.TryGetValue(body, out var property)) continue;

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
                    section.Entries.Add(choice = new KToolEntry());
                }

                choice.Settings.Add(setting);
            }

            if (section.Entries.Count != 0)
            {
                Sections.Add(section);
            }

            var leftovers = new List<string>();

            for (var i = 0; i < declarationOrder.Count; i++)
            {
                if (placed.Contains(declarationOrder[i])) continue;

                leftovers.Add(declarationOrder[i]);
            }

            AddSection(OtherSectionTitle, leftovers, propertiesByName);
        }

        // Slider layout line: "~PropertyName|Label|min|max".
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

        #endregion

        #region Stored Data

        public void DeleteData() => this._deleteDataMethod?.Invoke(null, null);

        public void CollectStoredDataFiles(List<string> results)
        {
            results.Clear();

            if (DataPath is { } dataPath)
            {
                if (File.Exists(dataPath))
                    results.Add(Path.GetFileName(dataPath));

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

        // Deleting the keys rather than writing false back: the tools pick their own defaults when a key is missing, and not every default is false.
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

            // The tools fall back to these pre-rename keys whenever the current one is missing, so leaving them behind would restore the old value.
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

        private readonly PropertyInfo _property;

        #endregion

        #region Property

        public string Label { get; }

        public bool IsSlider { get; }

        public float Min { get; }

        public float Max { get; }

        public bool Value
        {
            get => (bool)this._property.GetValue(null);
            set => this._property.SetValue(null, value);
        }

        public float SliderValue
        {
            get => (float)this._property.GetValue(null);
            set => this._property.SetValue(null, value);
        }

        #endregion

        #region Method

        public KToolSetting(PropertyInfo property, string label = null)
        {
            this._property = property;

            Label = label ?? Prettify(property.Name);
        }

        public KToolSetting(PropertyInfo property, string label, float min, float max)
        {
            this._property = property;

            Label = label ?? Prettify(property.Name);
            IsSlider = true;
            Min = min;
            Max = max;
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
                    builder.Append(' ').Append(char.ToLowerInvariant(character));
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
