#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Kingfisher.KSetting
{
    public static class KTools
    {
        #region Fields

        public static readonly string[] ToolNames = { "KFolders", "KHierarchy", "KInspector", "KFavorites", "KTabs" };

        private static List<KTool> _installed;

        #endregion

        #region Properties

        public static List<KTool> Installed => _installed ??= Discover();

        #endregion

        #region Public Methods

        public static void Rediscover() => _installed = null;

        #endregion

        #region Private Methods

        private static List<KTool> Discover()
        {
            var tools = new List<KTool>();

            for (var i = 0; i < ToolNames.Length; i++)
            {
                var name = ToolNames[i];

                if (Type.GetType($"Kingfisher.{name}.{name}Menu, Kingfisher.{name}") is not { } menuType) continue;

                tools.Add(new KTool(name, menuType));
            }

            return tools;
        }

        #endregion
    }

    public class KTool
    {
        #region Fields

        private const string DisabledPropertyName = "PluginDisabled";
        private const string LayoutFieldName = "SettingsLayout";

        #endregion

        #region Properties

        public string Name { get; }

        public List<KToolSection> Sections { get; } = new();

        public KToolSetting DisabledSetting { get; }

        #endregion

        #region Constructor

        public KTool(string name, Type menuType)
        {
            Name = name;

            var propertiesByName = new Dictionary<string, PropertyInfo>();
            var declarationOrder = new List<string>();

            foreach (var property in menuType.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (property.PropertyType != typeof(bool)) continue;
                if (!property.CanRead || !property.CanWrite) continue;

                if (property.Name == DisabledPropertyName)
                {
                    DisabledSetting = new KToolSetting(property);

                    continue;
                }

                propertiesByName[property.Name] = property;
                declarationOrder.Add(property.Name);
            }

            BuildSections(menuType, propertiesByName, declarationOrder);
        }

        #endregion

        #region Private Methods

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

                if (line[0] == '#')
                {
                    if (section.Entries.Count != 0)
                        Sections.Add(section);

                    section = new KToolSection(line.Substring(1).Trim());
                    choice = null;

                    continue;
                }

                var isChoice = line[0] == '*';
                var body = isChoice ? line.Substring(1).Trim() : line.Trim();

                string label = null;

                if (body.IndexOf('|') is var pipe && pipe != -1)
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
                    section.Entries.Add(choice = new KToolEntry());

                choice.Settings.Add(setting);
            }

            if (section.Entries.Count != 0)
                Sections.Add(section);

            var leftovers = new List<string>();

            for (var i = 0; i < declarationOrder.Count; i++)
                if (!placed.Contains(declarationOrder[i]))
                    leftovers.Add(declarationOrder[i]);

            AddSection("Other", leftovers, propertiesByName);
        }

        private void AddSection(string title, List<string> propertyNames, Dictionary<string, PropertyInfo> propertiesByName)
        {
            if (propertyNames.Count == 0) return;

            var section = new KToolSection(title);

            for (var i = 0; i < propertyNames.Count; i++)
                section.Entries.Add(new KToolEntry(new KToolSetting(propertiesByName[propertyNames[i]])));

            Sections.Add(section);
        }

        #endregion
    }

    public class KToolSection
    {
        #region Properties

        public string Title { get; }

        public List<KToolEntry> Entries { get; } = new();

        #endregion

        #region Constructor

        public KToolSection(string title) => Title = title;

        #endregion
    }

    public class KToolEntry
    {
        #region Fields

        private readonly bool _forcedChoice;

        #endregion

        #region Properties

        public List<KToolSetting> Settings { get; } = new();

        public bool IsChoice => Settings.Count > 1 || this._forcedChoice;

        #endregion

        #region Constructor

        public KToolEntry(KToolSetting single) => Settings.Add(single);

        public KToolEntry() => this._forcedChoice = true;

        #endregion
    }

    public class KToolSetting
    {
        #region Fields

        private readonly PropertyInfo _property;

        #endregion

        #region Properties

        public string Label { get; }

        public bool Value
        {
            get => (bool)this._property.GetValue(null);
            set => this._property.SetValue(null, value);
        }

        #endregion

        #region Constructor

        public KToolSetting(PropertyInfo property, string label = null)
        {
            this._property = property;

            Label = label ?? Prettify(property.Name);
        }

        #endregion

        #region Private Methods

        private static string Prettify(string propertyName)
        {
            if (propertyName.EndsWith("Enabled", StringComparison.Ordinal))
                propertyName = propertyName.Substring(0, propertyName.Length - "Enabled".Length);

            var builder = new StringBuilder();

            for (var i = 0; i < propertyName.Length; i++)
            {
                var c = propertyName[i];

                if (i == 0)
                {
                    builder.Append(char.ToUpperInvariant(c));

                    continue;
                }

                if (char.IsUpper(c) && !char.IsUpper(propertyName[i - 1]))
                    builder.Append(' ').Append(char.ToLowerInvariant(c));
                else
                    builder.Append(c);
            }

            return builder.ToString();
        }

        #endregion
    }
}
#endif
