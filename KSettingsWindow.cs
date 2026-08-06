#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Kingfisher.KSetting
{
    public class KSettingsWindow : EditorWindow
    {
        #region Fields

        // Layout
        private const float SidebarWidth = 138;
        private const float FooterHeight = 26;
        private const float HeaderHeight = 40;
        private const float RowHeight = 22;
        private const float Padding = 10;

        // Styles
        private static GUIStyle _titleStyle;
        private static GUIStyle _sidebarHeaderStyle;
        private static GUIStyle _sidebarItemStyle;
        private static GUIStyle _sidebarItemSelectedStyle;
        private static GUIStyle _sectionStyle;
        private static GUIStyle _footerStyle;
        private static GUIStyle _emptyStyle;
        private static bool _stylesBuilt;
        private static bool _stylesAreDark;

        // Selection
        private int _selectedTool;
        private int _pendingTool = -1;
        private Vector2 _scroll;

        #endregion

        #region Properties

        private static bool IsDark => EditorGUIUtility.isProSkin;

        private static Color WindowBackground => IsDark ? Greyscale(.22f) : Greyscale(.78f);
        private static Color SidebarBackground => IsDark ? Greyscale(.19f) : Greyscale(.735f);
        private static Color SelectedBackground => IsDark ? new Color(.17f, .365f, .535f) : new Color(.2f, .375f, .555f) * 1.2f;
        private static Color DividerColor => IsDark ? Greyscale(.13f) : Greyscale(.6f);
        private static Color RowStripe => Greyscale(IsDark ? 1 : 0, .025f);
        private static Color RowHover => Greyscale(IsDark ? 1 : 0, .05f);

        #endregion

        #region Unity Lifecycle

        private void OnEnable() => wantsMouseMove = true;

        private void OnGUI()
        {
            BuildStyles();

            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), WindowBackground);

            var tools = KTools.Installed;

            if (tools.Count == 0)
            {
                DrawEmptyState();

                return;
            }

            if (Event.current.type == EventType.Layout && this._pendingTool != -1)
            {
                this._selectedTool = this._pendingTool;
                this._pendingTool = -1;
                this._scroll = Vector2.zero;
            }

            if (this._selectedTool >= tools.Count)
                this._selectedTool = 0;

            var bodyHeight = position.height - FooterHeight;

            DrawSidebar(new Rect(0, 0, SidebarWidth, bodyHeight), tools);
            DrawContent(new Rect(SidebarWidth, 0, position.width - SidebarWidth, bodyHeight), tools[this._selectedTool]);
            DrawFooter(new Rect(0, bodyHeight, position.width, FooterHeight));

            if (Event.current.type == EventType.MouseMove)
                Repaint();
        }

        #endregion

        #region Public Methods

        [MenuItem("Tools/Kingfisher/Settings", false, 0)]
        public static void Open()
        {
            KTools.Rediscover();

            var window = GetWindow<KSettingsWindow>(utility: false, title: "Kingfisher", focus: true);

            window.minSize = new Vector2(460, 320);
        }

        #endregion

        #region Drawing

        private void DrawSidebar(Rect rect, List<KTool> tools)
        {
            EditorGUI.DrawRect(rect, SidebarBackground);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), DividerColor);

            var headerRect = new Rect(rect.x + Padding, rect.y + 10, rect.width - Padding, 14);

            GUI.Label(headerRect, "TOOLS", _sidebarHeaderStyle);

            var y = headerRect.yMax + 6;

            for (var i = 0; i < tools.Count; i++)
            {
                DrawSidebarRow(new Rect(rect.x, y, rect.width - 1, RowHeight + 4), tools[i], i);

                y += RowHeight + 4;
            }
        }

        private void DrawSidebarRow(Rect rect, KTool tool, int index)
        {
            var isSelected = (this._pendingTool == -1 ? this._selectedTool : this._pendingTool) == index;
            var isHovered = rect.Contains(Event.current.mousePosition);

            if (isSelected)
                EditorGUI.DrawRect(rect, SelectedBackground);
            else if (isHovered)
                EditorGUI.DrawRect(rect, RowHover);

            var labelRect = new Rect(rect.x + Padding, rect.y, rect.width - Padding, rect.height);
            var isOff = tool.DisabledSetting != null && tool.DisabledSetting.Value;
            var previousColor = GUI.color;

            if (isOff && !isSelected)
                GUI.color = Greyscale(1, .45f);

            GUI.Label(labelRect, tool.Name, isSelected ? _sidebarItemSelectedStyle : _sidebarItemStyle);

            GUI.color = previousColor;

            if (Event.current.type != EventType.MouseDown) return;
            if (!isHovered) return;

            this._pendingTool = index;

            GUI.FocusControl(null);

            Event.current.Use();

            Repaint();
        }

        private void DrawContent(Rect rect, KTool tool)
        {
            GUILayout.BeginArea(rect);

            DrawToolHeader(rect.width, tool);

            this._scroll = GUILayout.BeginScrollView(this._scroll);

            GUILayout.Space(2);

            if (tool.Sections.Count == 0)
                GUILayout.Label("   This tool has no settings.", _footerStyle);

            using (new EditorGUI.DisabledScope(tool.DisabledSetting != null && tool.DisabledSetting.Value))
                DrawSections(tool);

            GUILayout.Space(8);

            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawSections(KTool tool)
        {
            var rowIndex = 0;

            for (var s = 0; s < tool.Sections.Count; s++)
            {
                var section = tool.Sections[s];

                if (section.Title != null)
                {
                    DrawSectionHeader(section.Title, isFirst: s == 0);

                    rowIndex = 0;
                }

                for (var e = 0; e < section.Entries.Count; e++)
                {
                    var entry = section.Entries[e];

                    if (entry.IsChoice)
                        DrawChoice(entry, ref rowIndex);
                    else
                        DrawToggleRow(entry.Settings[0], rowIndex++);
                }
            }
        }

        private void DrawSectionHeader(string title, bool isFirst)
        {
            GUILayout.Space(isFirst ? 6 : 14);

            var rect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));

            GUI.Label(new Rect(rect.x + Padding, rect.y, rect.width - Padding, rect.height), title.ToUpperInvariant(), _sectionStyle);

            EditorGUI.DrawRect(new Rect(rect.x + Padding, rect.yMax, rect.width - Padding * 2, 1), DividerColor);

            GUILayout.Space(4);
        }

        private void DrawChoice(KToolEntry entry, ref int rowIndex)
        {
            for (var i = 0; i < entry.Settings.Count; i++)
            {
                var option = entry.Settings[i];
                var rect = BeginRow(rowIndex++);
                var optionRect = new Rect(rect.x + Padding + 2, rect.y + 2, rect.width - Padding * 2 - 2, rect.height - 4);

                if (!GUI.Toggle(optionRect, option.Value, option.Label, EditorStyles.radioButton)) continue;
                if (option.Value) continue;

                Apply(option, true);
            }
        }

        private void DrawToggleRow(KToolSetting setting, int rowIndex)
        {
            var rect = BeginRow(rowIndex);
            var toggleRect = new Rect(rect.x + Padding, rect.y + 2, rect.width - Padding * 2, rect.height - 4);
            var newValue = EditorGUI.ToggleLeft(toggleRect, setting.Label, setting.Value);

            if (newValue == setting.Value) return;

            Apply(setting, newValue);
        }

        private Rect BeginRow(int rowIndex)
        {
            var rect = GUILayoutUtility.GetRect(0, RowHeight, GUILayout.ExpandWidth(true));

            if (rowIndex % 2 == 1)
                EditorGUI.DrawRect(rect, RowStripe);

            if (GUI.enabled && rect.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(rect, RowHover);

            return rect;
        }

        private void DrawToolHeader(float width, KTool tool)
        {
            var rect = GUILayoutUtility.GetRect(width, HeaderHeight);

            GUI.Label(new Rect(rect.x + Padding, rect.y, rect.width - Padding, rect.height), tool.Name, _titleStyle);

            DrawEnabledToggle(rect, tool);

            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), DividerColor);
        }

        private void DrawEnabledToggle(Rect headerRect, KTool tool)
        {
            if (tool.DisabledSetting is not { } disabledSetting) return;

            const float toggleWidth = 76f;

            var toggleRect = new Rect(headerRect.xMax - toggleWidth - Padding, headerRect.y + (headerRect.height - 18) / 2, toggleWidth, 18);
            var isEnabled = !disabledSetting.Value;
            var newIsEnabled = EditorGUI.ToggleLeft(toggleRect, "Enabled", isEnabled);

            if (newIsEnabled == isEnabled) return;

            Apply(disabledSetting, !newIsEnabled);
        }

        private void DrawFooter(Rect rect)
        {
            EditorGUI.DrawRect(rect, SidebarBackground);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), DividerColor);

            const float buttonWidth = 58f;

            var folderPath = KData.FolderPath;

            GUI.Label(new Rect(rect.x + Padding, rect.y, rect.width - buttonWidth - Padding * 3, rect.height), KData.RelativeFolderPath, _footerStyle);

            var buttonRect = new Rect(rect.xMax - buttonWidth - Padding, rect.y + 4, buttonWidth, rect.height - 9);

            using (new EditorGUI.DisabledScope(!Directory.Exists(folderPath)))
                if (GUI.Button(buttonRect, "Reveal", EditorStyles.miniButton))
                    EditorUtility.RevealInFinder(folderPath);
        }

        private void DrawEmptyState()
        {
            var rect = new Rect(Padding, Padding, position.width - Padding * 2, position.height - Padding * 2);

            GUI.Label(rect, "No K tools found in this project.", _emptyStyle);
        }

        #endregion

        #region Styles

        private static void BuildStyles()
        {
            if (_stylesBuilt && _stylesAreDark == IsDark) return;

            _stylesBuilt = true;
            _stylesAreDark = IsDark;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleLeft };

            _sidebarHeaderStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
            _sidebarHeaderStyle.normal.textColor = Greyscale(IsDark ? 1 : 0, .4f);

            _sidebarItemStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };

            _sidebarItemSelectedStyle = new GUIStyle(_sidebarItemStyle);
            _sidebarItemSelectedStyle.normal.textColor = Color.white;

            _sectionStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            _sectionStyle.normal.textColor = Greyscale(IsDark ? 1 : 0, .45f);

            _footerStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
            _footerStyle.normal.textColor = Greyscale(IsDark ? 1 : 0, .45f);

            _emptyStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _emptyStyle.normal.textColor = Greyscale(IsDark ? 1 : 0, .5f);
        }

        private static Color Greyscale(float value, float alpha = 1) => new(value, value, value, alpha);

        #endregion

        #region Private Methods

        private static void Apply(KToolSetting setting, bool value)
        {
            setting.Value = value;

            InternalEditorUtility.RepaintAllViews();
        }

        #endregion
    }
}
#endif
