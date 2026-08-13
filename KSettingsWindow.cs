#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
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

        private const string SelectedToolKey = "Kingfisher.KSetting.SelectedTool";

        private const string DeleteDataTitleFormat = "Delete {0} data?";
        private const string DeleteDataEmptyBodyFormat = "{0} has nothing saved in {1} right now.\n\nIts in-memory copy will still be cleared.";
        private const string DeleteDataBodyFormat = "This permanently deletes:\n\n{0}\n\nfrom {1}. It cannot be undone.";
        private const string ResetSettingsTitleFormat = "Reset {0} settings?";

        private const string ResetSettingsBodyFormat = "This puts every {0} setting back to its default.\n\n" +
                                                       "Saved data in .KData is left alone. Settings are stored per project and per machine, so both copies are cleared, and scripts will reload.";

        private const string FileSeparator = "\n";
        private const string DeleteConfirmLabel = "Delete";
        private const string ResetConfirmLabel = "Reset";
        private const string CancelLabel = "Cancel";
        private const string BreadcrumbRoot = "Tools  ›";
        private const string EnabledLabel = "Enabled";

        private const float MinWindowWidth = 480f;
        private const float MinWindowHeight = 340f;
        private const float SidebarWidth = 150f;
        private const float FooterHeight = 48f;
        private const float HeaderHeight = 34f;
        private const float Padding = 10f;
        private const float DividerThickness = 1f;
        private const float SearchTop = 8f;
        private const float SearchGap = 8f;
        private const float SidebarRowHeight = 22f;
        private const float ContentIndent = 22f;
        private const float ContentTopSpacing = 6f;
        private const float ContentBottomSpacing = 10f;
        private const float FirstSectionTopSpacing = 0f;
        private const float SectionTopSpacing = 16f;
        private const float SectionHeaderHeight = 18f;
        private const float SectionHeaderGap = 6f;
        private const float SectionRuleGap = 8f;
        private const float MasterToggleGap = 12f;
        private const float RowSpacing = 6f;
        private const float SliderLabelWidth = 130f;
        private const float ButtonHeight = 28f;
        private const float ButtonGap = 8f;
        private const float DeleteButtonWidth = 112f;
        private const float ResetButtonWidth = 78f;
        private const float OpenButtonWidth = 78f;
        private const float RevealButtonWidth = 78f;
        private const float BreadcrumbGap = 6f;

        private const float BoxSize = 16f;
        private const float BoxRadius = 3f;
        private const float BoxBorder = 1f;
        private const float BoxLabelGap = 6f;
        private const float TickThickness = 1.5f;
        private const float TickShortArm = 4f;
        private const float TickLongArm = 10f;
        private const float TickRotation = -45f;
        private const float TickShiftX = 1f;
        private const float DotSize = 6f;
        private const float DisabledAlpha = .5f;

        private const float LightSelectionBoost = 1.2f;
        private const float BreadcrumbRootAlpha = .5f;
        private const float SectionTextAlpha = .6f;
        private const float FooterTextAlpha = .45f;
        private const float EmptyTextAlpha = .5f;

        private const double LiveRepaintIntervalSeconds = .05;

        private static readonly float RowHeight = EditorGUIUtility.singleLineHeight + RowSpacing;
        private static readonly float RowInset = RowSpacing * .5f;

        private static readonly Vector2 TickCenterOffset = new((TickShortArm - TickLongArm * 2f) * .25f, (TickShortArm - TickThickness * 2f) * .25f);

        private static readonly Color DarkWindowColor = GetGreyscale(.22f);
        private static readonly Color LightWindowColor = GetGreyscale(.78f);
        private static readonly Color DarkSidebarColor = GetGreyscale(.19f);
        private static readonly Color LightSidebarColor = GetGreyscale(.735f);
        private static readonly Color DarkSelectionColor = new(.17f, .365f, .535f);
        private static readonly Color LightSelectionColor = new Color(.2f, .375f, .555f) * LightSelectionBoost;
        private static readonly Color DarkDividerColor = GetGreyscale(.13f);
        private static readonly Color LightDividerColor = GetGreyscale(.6f);
        private static readonly Color DarkRowHoverColor = GetGreyscale(1f, .05f);
        private static readonly Color LightRowHoverColor = GetGreyscale(0f, .05f);
        private static readonly Color DarkSettingTextTint = new(.9f, .95f, 1.12f);
        private static readonly Color AccentColor = new(.208f, .455f, .941f);
        private static readonly Color DarkBoxColor = GetGreyscale(.16f);
        private static readonly Color LightBoxColor = GetGreyscale(.98f);
        private static readonly Color DarkBoxBorderColor = GetGreyscale(.44f);
        private static readonly Color LightBoxBorderColor = GetGreyscale(.55f);
        private static readonly Color DarkSettingTextColor = new(.72f, .75f, .83f);
        private static readonly Color LightSettingTextColor = new(.13f, .15f, .2f);
        private static readonly Color DarkDangerTintColor = new(1.35f, .62f, .58f);
        private static readonly Color LightDangerTintColor = new(1.3f, .72f, .68f);
        private static readonly Color DarkDangerTextColor = new(1f, .78f, .75f);
        private static readonly Color LightDangerTextColor = new(.42f, .06f, .04f);
        private static readonly Color DisabledLabelTint = GetGreyscale(1f, .45f);

        private static readonly GUIContent BreadcrumbRootContent = new(BreadcrumbRoot);
        private static readonly GUIContent NoSettingsContent = new("This tool has no settings.");
        private static readonly GUIContent NoMatchContent = new("Nothing matches the search.");
        private static readonly GUIContent DeleteDataButtonContent = new("Delete data");
        private static readonly GUIContent ResetButtonContent = new("Reset");
        private static readonly GUIContent OpenButtonContent = new("Open", "Open this tool's own window");
        private static readonly GUIContent RevealButtonContent = new("Reveal");
        private static readonly GUIContent EmptyStateContent = new("No K tools found in this project.");

        private static readonly GUILayoutOption[] ExpandWidthOptions = { GUILayout.ExpandWidth(true) };
        private static readonly Dictionary<string, (GUIContent Content, float Width, int StylesVersion)> SectionTitleContents = new();
        private static readonly List<string> StoredDataFiles = new();

        private static Color _previousContentColor;

        private static GUIStyle _breadcrumbRootStyle;
        private static GUIStyle _breadcrumbStyle;
        private static GUIStyle _buttonStyle;
        private static GUIStyle _dangerButtonStyle;
        private static GUIStyle _sidebarItemStyle;
        private static GUIStyle _sidebarItemSelectedStyle;
        private static GUIStyle _sectionStyle;
        private static GUIStyle _settingLabelStyle;
        private static GUIStyle _footerStyle;
        private static GUIStyle _emptyStyle;
        private static float _breadcrumbRootWidth;
        private static bool _hasBuiltStyles;
        private static bool _isStyleDark;
        private static int _stylesVersion;
        private static double _nextRepaintAllViewsTime;
        private static double _nextRepaintTime;

        private readonly List<KTool> _visibleTools = new();
        private readonly List<KToolSection> _sections = new();

        private SearchField _searchField;
        private KTool _selectedTool;
        private string _selectedToolName;
        private string _pendingToolName;
        private string _search = string.Empty;
        private Vector2 _scroll;
        private bool _isFilterDirty;
        private bool _isSearchActive;
        private bool _hasDataFolder;

        #endregion

        #region Property

        private static bool IsDark => EditorGUIUtility.isProSkin;

        private static bool IsDragStart => Event.current.rawType == EventType.MouseDown;

        private static bool IsDragEnd => Event.current.rawType == EventType.MouseUp;

        private static Color WindowBackground => IsDark ? DarkWindowColor : LightWindowColor;

        private static Color SidebarBackground => IsDark ? DarkSidebarColor : LightSidebarColor;

        private static Color SelectedBackground => IsDark ? DarkSelectionColor : LightSelectionColor;

        private static Color DividerColor => IsDark ? DarkDividerColor : LightDividerColor;

        private static Color RowHover => IsDark ? DarkRowHoverColor : LightRowHoverColor;

        private static Color DangerTint => IsDark ? DarkDangerTintColor : LightDangerTintColor;

        private static Color SettingTextTint => IsDark ? DarkSettingTextTint : Color.white;

        private static Color SettingTextColor => IsDark ? DarkSettingTextColor : LightSettingTextColor;

        private static Color BoxColor => IsDark ? DarkBoxColor : LightBoxColor;

        private static Color BoxBorderColor => IsDark ? DarkBoxBorderColor : LightBoxBorderColor;

        private bool HasSearch => !string.IsNullOrEmpty(this._search);

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            wantsMouseMove = true;

            this._searchField = new SearchField();
            this._selectedToolName = SessionState.GetString(SelectedToolKey, string.Empty);
            this._hasDataFolder = Directory.Exists(KData.FolderPath);

            RebuildFilter();
        }

        private void OnGUI()
        {
            BuildStyles();

            EditorGUI.DrawRect(new Rect(0f, 0f, position.width, position.height), WindowBackground);

            if (Event.current.type == EventType.Layout)
            {
                ApplyPending();
            }

            if (KTools.Installed.Count == 0)
            {
                DrawEmptyState();

                return;
            }

            var bodyHeight = position.height - FooterHeight;

            DrawSidebar(new Rect(0f, 0f, SidebarWidth, bodyHeight));
            DrawContent(new Rect(SidebarWidth, 0f, position.width - SidebarWidth, bodyHeight));
            DrawFooter(new Rect(0f, bodyHeight, position.width, FooterHeight));

            if (Event.current.type != EventType.MouseMove) return;

            RequestRepaint();
        }

        private void OnFocus() => this._hasDataFolder = Directory.Exists(KData.FolderPath);

        private void OnLostFocus() => KSettings.EndDeferredSave();

        #endregion

        #region Drawing

        private void DrawSidebar(Rect rect)
        {
            EditorGUI.DrawRect(rect, SidebarBackground);
            EditorGUI.DrawRect(new Rect(rect.xMax - DividerThickness, rect.y, DividerThickness, rect.height), DividerColor);

            var searchRect = new Rect(rect.x + Padding, rect.y + SearchTop, rect.width - Padding * 2f, EditorGUIUtility.singleLineHeight);

            DrawSearch(searchRect);

            var y = searchRect.yMax + SearchGap;

            for (var i = 0; i < this._visibleTools.Count; i++)
            {
                DrawSidebarRow(new Rect(rect.x, y, rect.width - DividerThickness, SidebarRowHeight), this._visibleTools[i]);

                y += SidebarRowHeight;
            }
        }

        private void DrawSearch(Rect rect)
        {
            var newSearch = this._searchField.OnGUI(rect, this._search);

            if (newSearch == this._search) return;

            this._search = newSearch;
            this._isFilterDirty = true;

            Repaint();
        }

        private void DrawSidebarRow(Rect rect, KTool tool)
        {
            var activeName = this._pendingToolName ?? this._selectedTool?.Name;
            var isSelected = activeName == tool.Name;
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

            this._pendingToolName = tool.Name;

            GUI.FocusControl(null);

            Event.current.Use();

            Repaint();
        }

        private void DrawContent(Rect rect)
        {
            GUILayout.BeginArea(rect);

            DrawBreadcrumb(rect.width);

            this._scroll = GUILayout.BeginScrollView(this._scroll);

            GUILayout.Space(ContentTopSpacing);

            if (this._selectedTool == null)
            {
                DrawNotice(NoMatchContent);
            }
            else
            {
                DrawMasterToggle();
                DrawSections();
            }

            GUILayout.Space(ContentBottomSpacing);

            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawBreadcrumb(float width)
        {
            var rect = GUILayoutUtility.GetRect(width, HeaderHeight);
            var rootRect = new Rect(rect.x + Padding, rect.y, _breadcrumbRootWidth, rect.height);

            GUI.Label(rootRect, BreadcrumbRootContent, _breadcrumbRootStyle);

            if (this._selectedTool == null) return;

            var nameX = rootRect.xMax + BreadcrumbGap;

            GUI.Label(new Rect(nameX, rect.y, rect.xMax - Padding - nameX, rect.height), this._selectedTool.Name, _breadcrumbStyle);
        }

        private void DrawMasterToggle()
        {
            if (this._selectedTool.DisabledSetting is not { } disabledSetting) return;

            var isEnabled = !disabledSetting.Value;
            var newIsEnabled = DrawCheckbox(GetRowRect(), isEnabled, EnabledLabel);

            GUILayout.Space(MasterToggleGap);

            if (newIsEnabled == isEnabled) return;

            Apply(disabledSetting, !newIsEnabled);
        }

        private void DrawSections()
        {
            if (this._sections.Count == 0)
            {
                DrawNotice(this._isSearchActive ? NoMatchContent : NoSettingsContent);

                return;
            }

            EditorGUI.BeginDisabledGroup(IsToolDisabled(this._selectedTool));

            for (var i = 0; i < this._sections.Count; i++)
            {
                DrawSection(this._sections[i], i);
            }

            EditorGUI.EndDisabledGroup();
        }

        private static void DrawSection(KToolSection section, int index)
        {
            if (section.Title != null)
            {
                DrawSectionHeader(section.Title, index == 0 ? FirstSectionTopSpacing : SectionTopSpacing);
            }

            for (var i = 0; i < section.Entries.Count; i++)
            {
                DrawEntry(section.Entries[i]);
            }
        }

        private static void DrawSectionHeader(string title, float topSpacing)
        {
            GUILayout.Space(topSpacing);

            var rect = GUILayoutUtility.GetRect(0f, SectionHeaderHeight, ExpandWidthOptions);
            var content = GetSectionTitleContent(title, out var width);
            var labelRect = new Rect(rect.x + Padding, rect.y, width, rect.height);

            GUI.Label(labelRect, content, _sectionStyle);

            var ruleX = labelRect.xMax + SectionRuleGap;
            var ruleWidth = rect.xMax - Padding - ruleX;

            if (ruleWidth > 0f)
            {
                EditorGUI.DrawRect(new Rect(ruleX, rect.y + (rect.height - DividerThickness) * .5f, ruleWidth, DividerThickness), DividerColor);
            }

            GUILayout.Space(SectionHeaderGap);
        }

        private static void DrawEntry(KToolEntry entry)
        {
            if (entry.IsChoice)
            {
                DrawChoice(entry);

                return;
            }

            var setting = entry.Settings[0];

            if (setting.IsSlider)
            {
                DrawSliderRow(setting);

                return;
            }

            if (setting.IsColor)
            {
                DrawColorRow(setting);

                return;
            }

            DrawToggleRow(setting);
        }

        private static void DrawChoice(KToolEntry entry)
        {
            for (var i = 0; i < entry.Settings.Count; i++)
            {
                var option = entry.Settings[i];

                if (!DrawRadio(GetRowRect(), option.Value, option.Label)) continue;

                if (option.Value) continue;

                Apply(option, true);
            }
        }

        private static void DrawToggleRow(KToolSetting setting)
        {
            var newIsOn = DrawCheckbox(GetRowRect(), setting.Value, setting.Label);

            if (newIsOn == setting.Value) return;

            Apply(setting, newIsOn);
        }

        private static void DrawColorRow(KToolSetting setting)
        {
            if (IsDragStart)
            {
                KSettings.BeginDeferredSave();
            }

            var rect = GetRowRect();
            var value = setting.ColorValue;
            var previousLabelWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = SliderLabelWidth;

            BeginLabelTint();

            var newValue = EditorGUI.ColorField(rect, setting.Label, value);

            EndLabelTint();

            EditorGUIUtility.labelWidth = previousLabelWidth;

            if (newValue != value)
            {
                setting.ColorValue = newValue;

                RequestLiveRepaint();
            }

            if (IsDragEnd)
            {
                KSettings.EndDeferredSave();
            }
        }

        private static void DrawSliderRow(KToolSetting setting)
        {
            if (IsDragStart)
            {
                KSettings.BeginDeferredSave();
            }

            var rect = GetRowRect();
            var value = setting.SliderValue;
            var previousLabelWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = SliderLabelWidth;

            BeginLabelTint();

            var newValue = EditorGUI.Slider(rect, setting.Label, value, setting.Min, setting.Max);

            EndLabelTint();

            EditorGUIUtility.labelWidth = previousLabelWidth;

            if (!Mathf.Approximately(newValue, value))
            {
                setting.SliderValue = newValue;

                RequestLiveRepaint();
            }

            if (IsDragEnd)
            {
                KSettings.EndDeferredSave();
            }
        }

        private static void RequestLiveRepaint()
        {
            var now = EditorApplication.timeSinceStartup;

            if (!IsDragEnd && now < _nextRepaintAllViewsTime) return;

            _nextRepaintAllViewsTime = now + LiveRepaintIntervalSeconds;

            InternalEditorUtility.RepaintAllViews();
        }

        private void RequestRepaint()
        {
            var now = EditorApplication.timeSinceStartup;

            if (now < _nextRepaintTime) return;

            _nextRepaintTime = now + LiveRepaintIntervalSeconds;

            Repaint();
        }

        private static void DrawNotice(GUIContent content) => GUI.Label(GetRowRect(), content, _footerStyle);

        private static Rect GetRowRect()
        {
            var rect = GUILayoutUtility.GetRect(0f, RowHeight, ExpandWidthOptions);

            return new Rect(rect.x + Padding + ContentIndent, rect.y + RowInset, rect.width - Padding * 2f - ContentIndent, EditorGUIUtility.singleLineHeight);
        }

        private void DrawFooter(Rect rect)
        {
            EditorGUI.DrawRect(rect, SidebarBackground);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, DividerThickness), DividerColor);

            var buttonY = rect.y + (rect.height - ButtonHeight) * .5f;

            var actionsLeft = DrawToolActions(rect, buttonY);

            DrawDataPath(rect, buttonY, actionsLeft);
        }

        private float DrawToolActions(Rect rect, float buttonY)
        {
            var right = rect.xMax - Padding;

            if (this._selectedTool == null) return right;

            if (this._selectedTool.CanDeleteData)
            {
                var deleteRect = new Rect(right - DeleteButtonWidth, buttonY, DeleteButtonWidth, ButtonHeight);
                var previousBackground = GUI.backgroundColor;

                GUI.backgroundColor = DangerTint;

                if (GUI.Button(deleteRect, DeleteDataButtonContent, _dangerButtonStyle))
                {
                    ConfirmDeleteData(this._selectedTool);
                }

                GUI.backgroundColor = previousBackground;

                right = deleteRect.x - ButtonGap;
            }

            var resetRect = new Rect(right - ResetButtonWidth, buttonY, ResetButtonWidth, ButtonHeight);

            if (GUI.Button(resetRect, ResetButtonContent, _buttonStyle))
            {
                ConfirmResetSettings(this._selectedTool);
            }

            if (!this._selectedTool.CanOpenTool) return resetRect.x;

            var openRect = new Rect(resetRect.x - ButtonGap - OpenButtonWidth, buttonY, OpenButtonWidth, ButtonHeight);

            if (GUI.Button(openRect, OpenButtonContent, _buttonStyle))
            {
                EditorApplication.delayCall += this._selectedTool.OpenTool;
            }

            return openRect.x;
        }

        private void DrawDataPath(Rect rect, float buttonY, float actionsLeft)
        {
            var revealRect = new Rect(rect.x + Padding, buttonY, RevealButtonWidth, ButtonHeight);

            EditorGUI.BeginDisabledGroup(!this._hasDataFolder);

            if (GUI.Button(revealRect, RevealButtonContent, _buttonStyle))
            {
                EditorUtility.RevealInFinder(KData.FolderPath);
            }

            EditorGUI.EndDisabledGroup();

            var labelX = revealRect.xMax + ButtonGap;
            var labelWidth = actionsLeft - ButtonGap - labelX;

            if (labelWidth <= 0f) return;

            GUI.Label(new Rect(labelX, rect.y, labelWidth, rect.height), KData.RelativeFolderPath, _footerStyle);
        }

        private void DrawEmptyState()
        {
            var rect = new Rect(Padding, Padding, position.width - Padding * 2f, position.height - Padding * 2f);

            GUI.Label(rect, EmptyStateContent, _emptyStyle);
        }

        #endregion

        #region Setting Control

        private static bool DrawCheckbox(Rect rect, bool isOn, string label)
        {
            var newIsOn = GUI.Toggle(rect, isOn, label, _settingLabelStyle);
            var boxRect = GetBoxRect(rect);

            DrawBox(boxRect, isOn, BoxRadius);

            if (!isOn) return newIsOn;

            DrawTick(boxRect);

            return newIsOn;
        }

        private static bool DrawRadio(Rect rect, bool isOn, string label)
        {
            var newIsOn = GUI.Toggle(rect, isOn, label, _settingLabelStyle);
            var boxRect = GetBoxRect(rect);

            DrawBox(boxRect, isOn, BoxSize * .5f);

            if (!isOn) return newIsOn;

            var dotRect = new Rect(boxRect.center.x - DotSize * .5f, boxRect.center.y - DotSize * .5f, DotSize, DotSize);

            DrawRounded(dotRect, Dim(Color.white), 0f, DotSize * .5f);

            return newIsOn;
        }

        private static void DrawBox(Rect rect, bool isOn, float radius)
        {
            if (isOn)
            {
                DrawRounded(rect, Dim(AccentColor), 0f, radius);

                return;
            }

            DrawRounded(rect, Dim(BoxColor), 0f, radius);
            DrawRounded(rect, Dim(BoxBorderColor), BoxBorder, radius);
        }

        private static void DrawTick(Rect box)
        {
            if (Event.current.type != EventType.Repaint) return;

            var pivot = new Vector2(box.center.x + TickShiftX, box.center.y);
            var previousMatrix = GUI.matrix;
            var color = Dim(Color.white);
            var corner = pivot + TickCenterOffset;

            GUIUtility.RotateAroundPivot(TickRotation, pivot);

            EditorGUI.DrawRect(new Rect(corner.x, corner.y - TickShortArm, TickThickness, TickShortArm + TickThickness), color);
            EditorGUI.DrawRect(new Rect(corner.x, corner.y, TickLongArm, TickThickness), color);

            GUI.matrix = previousMatrix;
        }

        private static void DrawRounded(Rect rect, Color color, float borderWidth, float radius)
        {
            if (Event.current.type != EventType.Repaint) return;

            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, borderWidth, radius);
        }

        private static void BeginLabelTint()
        {
            _previousContentColor = GUI.contentColor;

            GUI.contentColor *= SettingTextTint;
        }

        private static void EndLabelTint() => GUI.contentColor = _previousContentColor;

        private static Rect GetBoxRect(Rect rect) => new(rect.x, rect.y + (rect.height - BoxSize) * .5f, BoxSize, BoxSize);

        private static Color Dim(Color color) => GUI.enabled ? color : new Color(color.r, color.g, color.b, color.a * DisabledAlpha);

        #endregion

        #region Filtering

        private void ApplyPending()
        {
            if (this._pendingToolName != null)
            {
                this._selectedToolName = this._pendingToolName;
                this._pendingToolName = null;
                this._scroll = Vector2.zero;
                this._isFilterDirty = true;

                SessionState.SetString(SelectedToolKey, this._selectedToolName);
            }

            if (!this._isFilterDirty) return;

            RebuildFilter();
        }

        private void RebuildFilter()
        {
            this._isFilterDirty = false;
            this._isSearchActive = HasSearch;

            this._visibleTools.Clear();

            var tools = KTools.Installed;

            for (var i = 0; i < tools.Count; i++)
            {
                if (!IsToolVisible(tools[i])) continue;

                this._visibleTools.Add(tools[i]);
            }

            this._selectedTool = FindSelectedTool();

            RebuildSections();
        }

        private void RebuildSections()
        {
            this._sections.Clear();

            if (this._selectedTool == null) return;

            var sections = this._selectedTool.Sections;

            for (var i = 0; i < sections.Count; i++)
            {
                var section = sections[i];

                if (!this._isSearchActive || ContainsSearch(section.Title))
                {
                    this._sections.Add(section);

                    continue;
                }

                AddMatchingEntries(section);
            }
        }

        private void AddMatchingEntries(KToolSection section)
        {
            KToolSection matches = null;

            for (var i = 0; i < section.Entries.Count; i++)
            {
                if (!MatchesEntry(section.Entries[i])) continue;

                matches ??= new KToolSection(section.Title);

                matches.Entries.Add(section.Entries[i]);
            }

            if (matches == null) return;

            this._sections.Add(matches);
        }

        private KTool FindSelectedTool()
        {
            for (var i = 0; i < this._visibleTools.Count; i++)
            {
                if (this._visibleTools[i].Name != this._selectedToolName) continue;

                return this._visibleTools[i];
            }

            return this._visibleTools.Count > 0 ? this._visibleTools[0] : null;
        }

        private bool IsToolVisible(KTool tool)
        {
            if (!this._isSearchActive) return true;

            if (ContainsSearch(tool.Name)) return true;

            for (var i = 0; i < tool.Sections.Count; i++)
            {
                var section = tool.Sections[i];

                if (ContainsSearch(section.Title)) return true;

                for (var j = 0; j < section.Entries.Count; j++)
                {
                    if (!MatchesEntry(section.Entries[j])) continue;

                    return true;
                }
            }

            return false;
        }

        private bool MatchesEntry(KToolEntry entry)
        {
            for (var i = 0; i < entry.Settings.Count; i++)
            {
                if (!ContainsSearch(entry.Settings[i].Label)) continue;

                return true;
            }

            return false;
        }

        private bool ContainsSearch(string text) => text != null && text.IndexOf(this._search, StringComparison.OrdinalIgnoreCase) >= 0;

        #endregion

        #region Confirmation Dialog

        private void ConfirmDeleteData(KTool tool)
        {
            tool.CollectStoredDataFiles(StoredDataFiles);

            var body = StoredDataFiles.Count == 0
                ? string.Format(DeleteDataEmptyBodyFormat, tool.Name, tool.DataFolder)
                : string.Format(DeleteDataBodyFormat, string.Join(FileSeparator, StoredDataFiles), tool.DataFolder);

            if (!EditorUtility.DisplayDialog(string.Format(DeleteDataTitleFormat, tool.Name), body, DeleteConfirmLabel, CancelLabel)) return;

            EditorApplication.delayCall += () =>
            {
                tool.DeleteData();

                this._hasDataFolder = Directory.Exists(KData.FolderPath);

                Repaint();
            };
        }

        private static void ConfirmResetSettings(KTool tool)
        {
            var body = string.Format(ResetSettingsBodyFormat, tool.Name);

            if (!EditorUtility.DisplayDialog(string.Format(ResetSettingsTitleFormat, tool.Name), body, ResetConfirmLabel, CancelLabel)) return;

            EditorApplication.delayCall += () =>
            {
                tool.ResetSettings();

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
            _stylesVersion++;

            _breadcrumbRootStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
            _breadcrumbRootStyle.normal.textColor = GetSkinTextColor(BreadcrumbRootAlpha);
            _breadcrumbRootWidth = _breadcrumbRootStyle.CalcSize(BreadcrumbRootContent).x;

            _breadcrumbStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };

            _buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter };

            var dangerText = IsDark ? DarkDangerTextColor : LightDangerTextColor;

            _dangerButtonStyle = new GUIStyle(_buttonStyle);
            _dangerButtonStyle.normal.textColor = dangerText;
            _dangerButtonStyle.hover.textColor = dangerText;
            _dangerButtonStyle.active.textColor = dangerText;

            _sidebarItemStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };

            _sidebarItemSelectedStyle = new GUIStyle(_sidebarItemStyle);
            _sidebarItemSelectedStyle.normal.textColor = Color.white;

            _sectionStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
            _sectionStyle.normal.textColor = GetSkinTextColor(SectionTextAlpha);

            _settingLabelStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
            _settingLabelStyle.padding.left = Mathf.RoundToInt(BoxSize + BoxLabelGap);

            SetTextColor(_settingLabelStyle, SettingTextColor);

            _footerStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
            _footerStyle.normal.textColor = GetSkinTextColor(FooterTextAlpha);

            _emptyStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _emptyStyle.normal.textColor = GetSkinTextColor(EmptyTextAlpha);
        }

        private static void SetTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.onNormal.textColor = color;
            style.hover.textColor = color;
            style.onHover.textColor = color;
            style.active.textColor = color;
            style.onActive.textColor = color;
            style.focused.textColor = color;
            style.onFocused.textColor = color;
        }

        private static Color GetSkinTextColor(float alpha) => GetGreyscale(IsDark ? 1f : 0f, alpha);

        private static Color GetGreyscale(float value, float alpha = 1f) => new(value, value, value, alpha);

        #endregion

        #region Method

        [MenuItem(MenuPath, false, MenuPriority)]
        public static void Open()
        {
            KTools.Rediscover();

            var window = GetWindow<KSettingsWindow>(utility: false, title: WindowTitle, focus: true);

            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);

            window.RebuildFilter();
        }

        private static void Apply(KToolSetting setting, bool isOn)
        {
            setting.Value = isOn;

            InternalEditorUtility.RepaintAllViews();
        }

        private static bool IsToolDisabled(KTool tool) => tool.DisabledSetting != null && tool.DisabledSetting.Value;

        private static GUIContent GetSectionTitleContent(string title, out float width)
        {
            if (SectionTitleContents.TryGetValue(title, out var entry) && entry.StylesVersion == _stylesVersion)
            {
                width = entry.Width;

                return entry.Content;
            }

            var content = entry.Content ?? new GUIContent(title);

            width = _sectionStyle.CalcSize(content).x;

            SectionTitleContents[title] = (content, width, _stylesVersion);

            return content;
        }

        #endregion
    }
}
#endif
