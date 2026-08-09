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
        #region Field

        private const string MenuPath = "Tools/KTools Setting";
        private const string WindowTitle = "Kingfisher";
        private const int MenuPriority = 0;

        private const string DeleteDataTitleFormat = "Delete {0} data?";
        private const string DeleteDataEmptyBodyFormat = "{0} has nothing saved in .KData right now.\n\nIts in-memory copy will still be cleared.";
        private const string DeleteDataBodyFormat = "This permanently deletes:\n\n{0}\n\nfrom {1}. It cannot be undone.";
        private const string ResetSettingsTitleFormat = "Reset {0} settings?";

        private const string ResetSettingsBodyFormat = "This puts every {0} setting back to its default.\n\n" +
                                                       "Saved data in .KData is left alone. Settings are stored per project and per machine, so both copies are cleared, and scripts will reload.";

        private const string FileSeparator = "\n";
        private const string DeleteConfirmLabel = "Delete";
        private const string ResetConfirmLabel = "Reset";
        private const string CancelLabel = "Cancel";

        private const float MinWindowWidth = 460f;
        private const float MinWindowHeight = 320f;
        private const float SidebarWidth = 138f;
        private const float FooterHeight = 26f;
        private const float HeaderHeight = 40f;
        private const float RowHeight = 22f;
        private const float Padding = 10f;
        private const float ToggleWidth = 76f;
        private const float ButtonHeight = 18f;
        private const float DividerThickness = 1f;
        private const float SidebarHeaderTop = 10f;
        private const float SidebarHeaderHeight = 14f;
        private const float SidebarHeaderGap = 6f;
        private const float SidebarRowGap = 4f;
        private const float RowInset = 2f;
        private const float ChoiceIndent = 2f;
        private const float ContentTopSpacing = 2f;
        private const float ContentBottomSpacing = 8f;
        private const float FirstSectionSpacing = 6f;
        private const float SectionSpacing = 14f;
        private const float SectionHeaderHeight = 16f;
        private const float SectionHeaderGap = 4f;
        private const float DeleteButtonWidth = 82f;
        private const float ResetButtonWidth = 92f;
        private const float ButtonGap = 6f;
        private const float RevealButtonWidth = 58f;
        private const float FooterButtonTop = 4f;
        private const float FooterButtonInset = 9f;

        private const int TitleFontSize = 14;
        private const int NoPendingTool = -1;

        private const float LightSelectionBoost = 1.2f;
        private const float SidebarHeaderTextAlpha = .4f;
        private const float SectionTextAlpha = .45f;
        private const float FooterTextAlpha = .45f;
        private const float EmptyTextAlpha = .5f;

        private static readonly Color DarkWindowColor = GetGreyscale(.22f);
        private static readonly Color LightWindowColor = GetGreyscale(.78f);
        private static readonly Color DarkSidebarColor = GetGreyscale(.19f);
        private static readonly Color LightSidebarColor = GetGreyscale(.735f);
        private static readonly Color DarkSelectionColor = new(.17f, .365f, .535f);
        private static readonly Color LightSelectionColor = new Color(.2f, .375f, .555f) * LightSelectionBoost;
        private static readonly Color DarkDividerColor = GetGreyscale(.13f);
        private static readonly Color LightDividerColor = GetGreyscale(.6f);
        private static readonly Color DarkRowStripeColor = GetGreyscale(1f, .025f);
        private static readonly Color LightRowStripeColor = GetGreyscale(0f, .025f);
        private static readonly Color DarkRowHoverColor = GetGreyscale(1f, .05f);
        private static readonly Color LightRowHoverColor = GetGreyscale(0f, .05f);
        private static readonly Color DarkDangerTintColor = new(1.35f, .62f, .58f);
        private static readonly Color LightDangerTintColor = new(1.3f, .72f, .68f);
        private static readonly Color DarkDangerTextColor = new(1f, .78f, .75f);
        private static readonly Color LightDangerTextColor = new(.42f, .06f, .04f);
        private static readonly Color DisabledLabelTint = GetGreyscale(1f, .45f);

        private static readonly GUIContent SidebarHeaderContent = new("TOOLS");
        private static readonly GUIContent NoSettingsContent = new("   This tool has no settings.");
        private static readonly GUIContent DeleteDataButtonContent = new("Delete data");
        private static readonly GUIContent ResetSettingsButtonContent = new("Reset settings");
        private static readonly GUIContent EnabledToggleContent = new("Enabled");
        private static readonly GUIContent RevealButtonContent = new("Reveal");
        private static readonly GUIContent EmptyStateContent = new("No K tools found in this project.");

        private static readonly GUILayoutOption[] ExpandWidthOptions = { GUILayout.ExpandWidth(true) };
        private static readonly Dictionary<string, GUIContent> SectionTitleContents = new();
        private static readonly List<string> StoredDataFiles = new();

        private static GUIStyle _titleStyle;
        private static GUIStyle _dangerButtonStyle;
        private static GUIStyle _sidebarHeaderStyle;
        private static GUIStyle _sidebarItemStyle;
        private static GUIStyle _sidebarItemSelectedStyle;
        private static GUIStyle _sectionStyle;
        private static GUIStyle _footerStyle;
        private static GUIStyle _emptyStyle;
        private static bool _hasBuiltStyles;
        private static bool _isStyleDark;

        private int _selectedTool;
        private int _pendingTool = NoPendingTool;
        private Vector2 _scroll;

        #endregion

        #region Property

        private static bool IsDark => EditorGUIUtility.isProSkin;

        private static Color WindowBackground => IsDark ? DarkWindowColor : LightWindowColor;

        private static Color SidebarBackground => IsDark ? DarkSidebarColor : LightSidebarColor;

        private static Color SelectedBackground => IsDark ? DarkSelectionColor : LightSelectionColor;

        private static Color DividerColor => IsDark ? DarkDividerColor : LightDividerColor;

        private static Color RowStripe => IsDark ? DarkRowStripeColor : LightRowStripeColor;

        private static Color RowHover => IsDark ? DarkRowHoverColor : LightRowHoverColor;

        private static Color DangerTint => IsDark ? DarkDangerTintColor : LightDangerTintColor;

        #endregion

        #region Unity Lifecycle

        private void OnEnable() => wantsMouseMove = true;

        private void OnGUI()
        {
            BuildStyles();

            EditorGUI.DrawRect(new Rect(0f, 0f, position.width, position.height), WindowBackground);

            var tools = KTools.Installed;

            if (tools.Count == 0)
            {
                DrawEmptyState();

                return;
            }

            if (Event.current.type == EventType.Layout && this._pendingTool != NoPendingTool)
            {
                this._selectedTool = this._pendingTool;
                this._pendingTool = NoPendingTool;
                this._scroll = Vector2.zero;
            }

            if (this._selectedTool >= tools.Count)
            {
                this._selectedTool = 0;
            }

            var bodyHeight = position.height - FooterHeight;

            DrawSidebar(new Rect(0f, 0f, SidebarWidth, bodyHeight), tools);
            DrawContent(new Rect(SidebarWidth, 0f, position.width - SidebarWidth, bodyHeight), tools[this._selectedTool]);
            DrawFooter(new Rect(0f, bodyHeight, position.width, FooterHeight));

            if (Event.current.type != EventType.MouseMove) return;

            Repaint();
        }

        #endregion

        #region Drawing

        private void DrawSidebar(Rect rect, List<KTool> tools)
        {
            EditorGUI.DrawRect(rect, SidebarBackground);
            EditorGUI.DrawRect(new Rect(rect.xMax - DividerThickness, rect.y, DividerThickness, rect.height), DividerColor);

            var headerRect = new Rect(rect.x + Padding, rect.y + SidebarHeaderTop, rect.width - Padding, SidebarHeaderHeight);

            GUI.Label(headerRect, SidebarHeaderContent, _sidebarHeaderStyle);

            var y = headerRect.yMax + SidebarHeaderGap;
            var rowHeight = RowHeight + SidebarRowGap;

            for (var i = 0; i < tools.Count; i++)
            {
                DrawSidebarRow(new Rect(rect.x, y, rect.width - DividerThickness, rowHeight), tools[i], i);

                y += rowHeight;
            }
        }

        private void DrawSidebarRow(Rect rect, KTool tool, int index)
        {
            var isSelected = (this._pendingTool == NoPendingTool ? this._selectedTool : this._pendingTool) == index;
            var isHovered = rect.Contains(Event.current.mousePosition);

            if (isSelected)
            {
                EditorGUI.DrawRect(rect, SelectedBackground);
            }
            else if (isHovered)
            {
                EditorGUI.DrawRect(rect, RowHover);
            }

            var labelRect = new Rect(rect.x + Padding, rect.y, rect.width - Padding, rect.height);
            var isOff = IsToolDisabled(tool);
            var previousColor = GUI.color;

            if (isOff && !isSelected)
            {
                GUI.color = DisabledLabelTint;
            }

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

            GUILayout.Space(ContentTopSpacing);

            if (tool.Sections.Count == 0)
            {
                GUILayout.Label(NoSettingsContent, _footerStyle);
            }

            using (new EditorGUI.DisabledScope(IsToolDisabled(tool)))
            {
                DrawSections(tool);
            }

            GUILayout.Space(ContentBottomSpacing);

            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawSections(KTool tool)
        {
            var rowIndex = 0;

            for (var sectionIndex = 0; sectionIndex < tool.Sections.Count; sectionIndex++)
            {
                var section = tool.Sections[sectionIndex];

                if (section.Title != null)
                {
                    DrawSectionHeader(section.Title, sectionIndex == 0 ? FirstSectionSpacing : SectionSpacing);

                    rowIndex = 0;
                }

                for (var entryIndex = 0; entryIndex < section.Entries.Count; entryIndex++)
                {
                    var entry = section.Entries[entryIndex];

                    if (entry.IsChoice)
                    {
                        DrawChoice(entry, ref rowIndex);
                    }
                    else if (entry.Settings[0].IsSlider)
                    {
                        DrawSliderRow(entry.Settings[0], rowIndex++);
                    }
                    else
                    {
                        DrawToggleRow(entry.Settings[0], rowIndex++);
                    }
                }
            }
        }

        private void DrawSectionHeader(string title, float topSpacing)
        {
            GUILayout.Space(topSpacing);

            var rect = GUILayoutUtility.GetRect(0f, SectionHeaderHeight, ExpandWidthOptions);

            GUI.Label(new Rect(rect.x + Padding, rect.y, rect.width - Padding, rect.height), GetSectionTitleContent(title), _sectionStyle);

            EditorGUI.DrawRect(new Rect(rect.x + Padding, rect.yMax, rect.width - Padding * 2f, DividerThickness), DividerColor);

            GUILayout.Space(SectionHeaderGap);
        }

        private void DrawChoice(KToolEntry entry, ref int rowIndex)
        {
            for (var i = 0; i < entry.Settings.Count; i++)
            {
                var option = entry.Settings[i];
                var rect = BeginRow(rowIndex++);
                var optionRect = new Rect(rect.x + Padding + ChoiceIndent, rect.y + RowInset, rect.width - Padding * 2f - ChoiceIndent, rect.height - RowInset * 2f);

                if (!GUI.Toggle(optionRect, option.Value, option.Label, EditorStyles.radioButton)) continue;

                if (option.Value) continue;

                Apply(option, true);
            }
        }

        private void DrawToggleRow(KToolSetting setting, int rowIndex)
        {
            var rect = BeginRow(rowIndex);
            var toggleRect = new Rect(rect.x + Padding, rect.y + RowInset, rect.width - Padding * 2f, rect.height - RowInset * 2f);
            var newValue = EditorGUI.ToggleLeft(toggleRect, setting.Label, setting.Value);

            if (newValue == setting.Value) return;

            Apply(setting, newValue);
        }

        private void DrawSliderRow(KToolSetting setting, int rowIndex)
        {
            var rect = BeginRow(rowIndex);
            var sliderRect = new Rect(rect.x + Padding, rect.y + RowInset, rect.width - Padding * 2f, rect.height - RowInset * 2f);
            var value = setting.SliderValue;
            var newValue = EditorGUI.Slider(sliderRect, setting.Label, value, setting.Min, setting.Max);

            if (Mathf.Approximately(newValue, value)) return;

            setting.SliderValue = newValue;

            InternalEditorUtility.RepaintAllViews();
        }

        private Rect BeginRow(int rowIndex)
        {
            var rect = GUILayoutUtility.GetRect(0f, RowHeight, ExpandWidthOptions);

            if (rowIndex % 2 == 1)
            {
                EditorGUI.DrawRect(rect, RowStripe);
            }

            if (GUI.enabled && rect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(rect, RowHover);
            }

            return rect;
        }

        private void DrawToolHeader(float width, KTool tool)
        {
            var rect = GUILayoutUtility.GetRect(width, HeaderHeight);

            GUI.Label(new Rect(rect.x + Padding, rect.y, rect.width - Padding, rect.height), tool.Name, _titleStyle);

            DrawEnabledToggle(rect, tool);
            DrawResetButtons(rect, tool);

            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - DividerThickness, rect.width, DividerThickness), DividerColor);
        }

        private void DrawResetButtons(Rect headerRect, KTool tool)
        {
            var right = headerRect.xMax - Padding - (tool.DisabledSetting != null ? ToggleWidth + ButtonGap : 0f);
            var y = headerRect.y + (headerRect.height - ButtonHeight) / 2f;

            if (tool.CanDeleteData)
            {
                var deleteRect = new Rect(right - DeleteButtonWidth, y, DeleteButtonWidth, ButtonHeight);
                var previousBackground = GUI.backgroundColor;

                GUI.backgroundColor = DangerTint;

                if (GUI.Button(deleteRect, DeleteDataButtonContent, _dangerButtonStyle))
                {
                    ConfirmDeleteData(tool);
                }

                GUI.backgroundColor = previousBackground;

                right = deleteRect.x - ButtonGap;
            }

            var resetRect = new Rect(right - ResetButtonWidth, y, ResetButtonWidth, ButtonHeight);

            if (GUI.Button(resetRect, ResetSettingsButtonContent, EditorStyles.miniButton))
            {
                ConfirmResetSettings(tool);
            }
        }

        private void DrawEnabledToggle(Rect headerRect, KTool tool)
        {
            if (tool.DisabledSetting is not { } disabledSetting) return;

            var toggleRect = new Rect(headerRect.xMax - ToggleWidth - Padding, headerRect.y + (headerRect.height - ButtonHeight) / 2f, ToggleWidth, ButtonHeight);
            var isEnabled = !disabledSetting.Value;
            var newIsEnabled = EditorGUI.ToggleLeft(toggleRect, EnabledToggleContent, isEnabled);

            if (newIsEnabled == isEnabled) return;

            Apply(disabledSetting, !newIsEnabled);
        }

        private void DrawFooter(Rect rect)
        {
            EditorGUI.DrawRect(rect, SidebarBackground);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, DividerThickness), DividerColor);

            var folderPath = KData.FolderPath;

            GUI.Label(new Rect(rect.x + Padding, rect.y, rect.width - RevealButtonWidth - Padding * 3f, rect.height), KData.RelativeFolderPath, _footerStyle);

            var buttonRect = new Rect(rect.xMax - RevealButtonWidth - Padding, rect.y + FooterButtonTop, RevealButtonWidth, rect.height - FooterButtonInset);

            using (new EditorGUI.DisabledScope(!Directory.Exists(folderPath)))
            {
                if (GUI.Button(buttonRect, RevealButtonContent, EditorStyles.miniButton))
                {
                    EditorUtility.RevealInFinder(folderPath);
                }
            }
        }

        private void DrawEmptyState()
        {
            var rect = new Rect(Padding, Padding, position.width - Padding * 2f, position.height - Padding * 2f);

            GUI.Label(rect, EmptyStateContent, _emptyStyle);
        }

        #endregion

        #region Confirmation Dialog

        private void ConfirmDeleteData(KTool tool)
        {
            tool.CollectStoredDataFiles(StoredDataFiles);

            var body = StoredDataFiles.Count == 0
                ? string.Format(DeleteDataEmptyBodyFormat, tool.Name)
                : string.Format(DeleteDataBodyFormat, string.Join(FileSeparator, StoredDataFiles), KData.RelativeFolderPath);

            if (!EditorUtility.DisplayDialog(string.Format(DeleteDataTitleFormat, tool.Name), body, DeleteConfirmLabel, CancelLabel)) return;

            // Off the GUI stack - the dialog already interrupted this OnGUI pass.
            EditorApplication.delayCall += () =>
            {
                tool.DeleteData();

                Repaint();
            };
        }

        private void ConfirmResetSettings(KTool tool)
        {
            var body = string.Format(ResetSettingsBodyFormat, tool.Name);

            if (!EditorUtility.DisplayDialog(string.Format(ResetSettingsTitleFormat, tool.Name), body, ResetConfirmLabel, CancelLabel)) return;

            EditorApplication.delayCall += () =>
            {
                tool.ResetSettings();

                // The tools cache settings in statics that only a reload clears.
                EditorUtility.RequestScriptReload();
            };
        }

        #endregion

        #region Style

        private static void BuildStyles()
        {
            if (_hasBuiltStyles && _isStyleDark == IsDark) return;

            _hasBuiltStyles = true;
            _isStyleDark = IsDark;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = TitleFontSize, alignment = TextAnchor.MiddleLeft };

            var dangerText = IsDark ? DarkDangerTextColor : LightDangerTextColor;

            _dangerButtonStyle = new GUIStyle(EditorStyles.miniButton);
            _dangerButtonStyle.normal.textColor = dangerText;
            _dangerButtonStyle.hover.textColor = dangerText;
            _dangerButtonStyle.active.textColor = dangerText;

            _sidebarHeaderStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
            _sidebarHeaderStyle.normal.textColor = GetSkinTextColor(SidebarHeaderTextAlpha);

            _sidebarItemStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };

            _sidebarItemSelectedStyle = new GUIStyle(_sidebarItemStyle);
            _sidebarItemSelectedStyle.normal.textColor = Color.white;

            _sectionStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            _sectionStyle.normal.textColor = GetSkinTextColor(SectionTextAlpha);

            _footerStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
            _footerStyle.normal.textColor = GetSkinTextColor(FooterTextAlpha);

            _emptyStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _emptyStyle.normal.textColor = GetSkinTextColor(EmptyTextAlpha);
        }

        private static Color GetSkinTextColor(float alpha) => GetGreyscale(IsDark ? 1f : 0f, alpha);

        private static Color GetGreyscale(float value, float alpha = 1) => new(value, value, value, alpha);

        #endregion

        #region Method

        [MenuItem(MenuPath, false, MenuPriority)]
        public static void Open()
        {
            KTools.Rediscover();

            var window = GetWindow<KSettingsWindow>(utility: false, title: WindowTitle, focus: true);

            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
        }

        private static void Apply(KToolSetting setting, bool value)
        {
            setting.Value = value;

            InternalEditorUtility.RepaintAllViews();
        }

        private static bool IsToolDisabled(KTool tool) => tool.DisabledSetting != null && tool.DisabledSetting.Value;

        private static GUIContent GetSectionTitleContent(string title)
        {
            if (SectionTitleContents.TryGetValue(title, out var content)) return content;

            content = new GUIContent(title.ToUpperInvariant());
            SectionTitleContents[title] = content;

            return content;
        }

        #endregion
    }
}
#endif
