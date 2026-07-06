using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR_WIN
using Microsoft.Win32;
#endif

using SW.Data;

using SW.Editor.Hierarchy;

using SW.Editor.Util;

namespace SW.Editor.Window
{
    /// <summary>
    /// SWPlayerPrefs와 기본 PlayerPrefs 데이터를 조회, 수정, 삭제, JSON 입출력하는 에디터 창.
    /// </summary>
    public class SWPlayerPrefsViewerWindow : EditorWindow
    {
        private enum PlayerPrefsViewMode
        {
            SWPlayerPrefs,
            UnityPlayerPrefs
        }

        private enum PlayerPrefsValueType
        {
            String,
            Integer,
            Float
        }

        private enum PlayerPrefsSearchTarget
        {
            All,
            Key,
            Value,
            Type
        }

        /// <summary>
        /// JSON 입출력에 사용하는 PlayerPrefs 항목 컨테이너입니다.
        /// </summary>
        [Serializable]
        private class PrefsData
        {
            /// <summary>저장된 PlayerPrefs 항목 목록입니다.</summary>
            public List<PrefsEntry> entries = new();
        }

        /// <summary>
        /// 단일 PlayerPrefs 키와 값입니다.
        /// </summary>
        [Serializable]
        private class PrefsEntry
        {
            /// <summary>PlayerPrefs 키입니다.</summary>
            public string key;
            /// <summary>PlayerPrefs 값입니다.</summary>
            public string value;
            /// <summary>PlayerPrefs 값 타입입니다.</summary>
            public PlayerPrefsValueType valueType;
        }

        private const string SlotPrefKey = "SWTools.PlayerPrefsViewer.Slot";
        private static readonly string[] ViewModeTabNames = { "SWUtils PlayerPrefs", "Unity PlayerPrefs" };

        private readonly List<PrefsEntry> entries = new();
        private Vector2 scrollPosition;
        private Vector2 jsonScrollPosition;
        private PlayerPrefsViewMode viewMode;
        private string slotName = "default";
        private string searchFilter = "";
        private PlayerPrefsSearchTarget searchTarget = PlayerPrefsSearchTarget.All;
        private string editKey = "";
        private string editValue = "";
        private PlayerPrefsValueType editValueType = PlayerPrefsValueType.String;
        private string jsonText = "";
        private string statusMessage = "";

        /// <summary>
        /// PlayerPrefs Viewer 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Debug/PlayerPrefs Viewer")]
        public static void ShowWindow()
        {
            SWPlayerPrefsViewerWindow window = GetWindow<SWPlayerPrefsViewerWindow>();
            SWEditorUtils.SetupWindow(window, "SW PlayerPrefs", "d_SaveAs", 460, 520);
            window.Show();
        }

        private void OnEnable()
        {
            slotName = EditorPrefs.GetString(SlotPrefKey, SWPlayerPrefs.CurrentSlot);
            ApplySlot(false);
            RefreshEntries();
        }

        private void OnGUI()
        {
            PlayerPrefsViewMode previousViewMode = viewMode;
            viewMode = (PlayerPrefsViewMode)SWEditorUtils.DrawTabBar((int)viewMode, ViewModeTabNames);
            if (previousViewMode != viewMode)
                RefreshEntries();

            if (viewMode == PlayerPrefsViewMode.SWPlayerPrefs)
                DrawSlotSection();
            else
                DrawUnityPlayerPrefsSection();

            EditorGUILayout.Space(8);
            DrawEditSection();
            EditorGUILayout.Space(8);
            DrawListSection();

            if (viewMode == PlayerPrefsViewMode.SWPlayerPrefs)
            {
                EditorGUILayout.Space(8);
                DrawJsonSection();
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }
        }

        private void DrawSlotSection()
        {
            SWEditorUtils.DrawHeader("Slot");

            EditorGUILayout.BeginHorizontal();
            slotName = EditorGUILayout.TextField("Current Slot", slotName);
            if (GUILayout.Button("Apply", GUILayout.Width(70)))
                ApplySlot(true);
            if (GUILayout.Button("Default", GUILayout.Width(70)))
            {
                slotName = "default";
                ApplySlot(true);
            }
            EditorGUILayout.EndHorizontal();

            DrawSearchSection();
        }

        private void DrawUnityPlayerPrefsSection()
        {
            SWEditorUtils.DrawHeader("Unity PlayerPrefs");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Company", PlayerSettings.companyName);
                EditorGUILayout.TextField("Product", PlayerSettings.productName);
            }

            DrawSearchSection();

#if !UNITY_EDITOR_WIN
            EditorGUILayout.HelpBox("현재 기본 PlayerPrefs 자동 목록은 Windows Editor 저장소만 지원합니다. 키를 직접 입력하면 추가, 수정, 삭제는 가능합니다.", MessageType.Warning);
#endif
        }

        private void DrawSearchSection()
        {
            EditorGUILayout.BeginHorizontal();
            searchFilter = EditorGUILayout.TextField("Search", searchFilter);
            searchTarget = (PlayerPrefsSearchTarget)EditorGUILayout.EnumPopup(searchTarget, GUILayout.Width(86));
            using (new SWEditorUtils.GUIEnabledScope(!string.IsNullOrWhiteSpace(searchFilter)))
            {
                if (GUILayout.Button("Clear", GUILayout.Width(58)))
                    searchFilter = "";
            }
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                RefreshEntries();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEditSection()
        {
            SWEditorUtils.DrawHeader("Add / Edit");

            editKey = EditorGUILayout.TextField("Key", editKey);
            editValue = EditorGUILayout.TextField("Value", editValue);
            if (viewMode == PlayerPrefsViewMode.UnityPlayerPrefs)
                editValueType = (PlayerPrefsValueType)EditorGUILayout.EnumPopup("Type", editValueType);

            EditorGUILayout.BeginHorizontal();
            using (new SWEditorUtils.GUIEnabledScope(!string.IsNullOrWhiteSpace(editKey)))
            {
                string saveButtonText = viewMode == PlayerPrefsViewMode.SWPlayerPrefs ? "Save String" : "Save";
                if (GUILayout.Button(saveButtonText, GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
                    SaveEntry();
            }

            if (GUILayout.Button("Clear Fields", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
            {
                editKey = "";
                editValue = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawListSection()
        {
            int filteredCount = GetFilteredEntryCount();
            string listTitle = string.IsNullOrWhiteSpace(searchFilter)
                ? $"Entries ({entries.Count})"
                : $"Entries ({filteredCount}/{entries.Count})";
            SWEditorUtils.DrawHeader(listTitle);

            if (entries.Count == 0)
            {
                string emptyMessage = viewMode == PlayerPrefsViewMode.SWPlayerPrefs
                    ? "No SWPlayerPrefs entries in this slot."
                    : "No Unity PlayerPrefs entries found.";
                SWEditorUtils.DrawEmptyNotice(emptyMessage, MessageType.None);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(160));
            for (int index = 0; index < entries.Count; index++)
            {
                PrefsEntry entry = entries[index];
                if (!MatchesFilter(entry))
                    continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.SelectableLabel(entry.key, EditorStyles.boldLabel, GUILayout.Height(18));
                if (viewMode == PlayerPrefsViewMode.UnityPlayerPrefs)
                    entry.valueType = (PlayerPrefsValueType)EditorGUILayout.EnumPopup(entry.valueType, GUILayout.Width(70));

                if (GUILayout.Button("Edit", GUILayout.Width(48), GUILayout.Height(20)))
                {
                    editKey = entry.key;
                    editValue = entry.value;
                    editValueType = entry.valueType;
                    GUI.FocusControl(null);
                }

                if (GUILayout.Button("Save", GUILayout.Width(50), GUILayout.Height(20)))
                {
                    SaveEntry(entry.key, entry.value, entry.valueType);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Copy", GUILayout.Width(52), GUILayout.Height(20)))
                {
                    EditorGUIUtility.systemCopyBuffer = entry.value ?? "";
                    statusMessage = $"Copied value: {entry.key}";
                }

                if (GUILayout.Button("Delete", GUILayout.Width(58), GUILayout.Height(20)))
                {
                    DeleteEntry(entry.key);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();

                entry.value = EditorGUILayout.TextField(entry.value ?? "");

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();

            if (viewMode == PlayerPrefsViewMode.SWPlayerPrefs &&
                SWEditorUtils.DangerButton("Delete All In Slot", "Delete SWPlayerPrefs",
                $"Delete all encrypted SWPlayerPrefs entries in slot '{SWPlayerPrefs.CurrentSlot}'?",
                "Delete"))
            {
                SWPlayerPrefs.DeleteAll();
                RefreshEntries("Deleted all entries in current slot.");
            }

            if (viewMode == PlayerPrefsViewMode.UnityPlayerPrefs &&
                SWEditorUtils.DangerButton("Clear All PlayerPrefs", "Clear All PlayerPrefs",
                    "Delete every PlayerPrefs value for this Unity project? This also removes SWPlayerPrefs encrypted data stored in PlayerPrefs.",
                    "Clear All"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                SWPlayerPrefs.SetSlot(slotName);
                RefreshEntries("Cleared all PlayerPrefs.");
            }
        }

        private void DrawJsonSection()
        {
            SWEditorUtils.DrawHeader("JSON Export / Import");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
            {
                jsonText = SWPlayerPrefs.ExportToJson();
                statusMessage = "Exported current slot to JSON.";
            }

            using (new SWEditorUtils.GUIEnabledScope(!string.IsNullOrEmpty(jsonText)))
            {
                if (GUILayout.Button("Copy JSON", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
                {
                    EditorGUIUtility.systemCopyBuffer = jsonText;
                    statusMessage = "Copied JSON.";
                }
            }
            EditorGUILayout.EndHorizontal();

            jsonScrollPosition = EditorGUILayout.BeginScrollView(jsonScrollPosition, GUILayout.Height(90));
            jsonText = EditorGUILayout.TextArea(jsonText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            using (new SWEditorUtils.GUIEnabledScope(!string.IsNullOrWhiteSpace(jsonText)))
            {
                if (SWEditorUtils.DangerButton("Import Replace", "Import SWPlayerPrefs",
                        "Replace current slot data with this JSON?", "Import"))
                    ImportJson(false);

                if (GUILayout.Button("Import Merge", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
                    ImportJson(true);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ApplySlot(bool showStatus)
        {
            if (string.IsNullOrWhiteSpace(slotName))
                slotName = "default";

            slotName = slotName.Trim();
            SWPlayerPrefs.SetSlot(slotName);
            EditorPrefs.SetString(SlotPrefKey, slotName);
            RefreshEntries(showStatus ? $"Applied slot: {slotName}" : "");
        }

        private void RefreshEntries(string message = "")
        {
            entries.Clear();
            if (viewMode == PlayerPrefsViewMode.UnityPlayerPrefs)
            {
                entries.AddRange(UnityPlayerPrefsStore.LoadEntries());
                entries.Sort((left, right) => string.Compare(left.key, right.key, StringComparison.Ordinal));
                statusMessage = message;
                Repaint();
                return;
            }

            string json = SWPlayerPrefs.ExportToJson();
            if (!string.IsNullOrEmpty(json))
            {
                PrefsData data = JsonUtility.FromJson<PrefsData>(json);
                if (data?.entries != null)
                    entries.AddRange(data.entries);
            }

            entries.Sort((left, right) => string.Compare(left.key, right.key, StringComparison.Ordinal));
            statusMessage = message;
            Repaint();
        }

        private void SaveEntry()
        {
            string key = editKey.Trim();
            SaveEntry(key, editValue, editValueType);
        }

        private void SaveEntry(string key, string value, PlayerPrefsValueType valueType)
        {
            if (viewMode == PlayerPrefsViewMode.SWPlayerPrefs)
            {
                SWPlayerPrefs.SetString(key, value ?? "");
                SWPlayerPrefs.Save();
                RefreshEntries($"Saved key: {key}");
                return;
            }

            if (!UnityPlayerPrefsStore.SaveEntry(key, value, valueType, out string errorMessage))
            {
                statusMessage = errorMessage;
                Repaint();
                return;
            }

            RefreshEntries($"Saved key: {key}");
        }

        private void DeleteEntry(string key)
        {
            if (!EditorUtility.DisplayDialog("Delete Entry", $"Delete '{key}'?", "Delete", "Cancel"))
                return;

            if (viewMode == PlayerPrefsViewMode.SWPlayerPrefs)
            {
                SWPlayerPrefs.DeleteKey(key);
                SWPlayerPrefs.Save();
            }
            else
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
            RefreshEntries($"Deleted key: {key}");
        }

        private void ImportJson(bool merge)
        {
            bool success = merge
                ? SWPlayerPrefs.MergeFromJson(jsonText)
                : SWPlayerPrefs.ImportFromJson(jsonText);

            RefreshEntries(success ? "Import completed." : "Import failed. Check JSON format.");
        }

        private bool MatchesFilter(PrefsEntry entry)
        {
            if (string.IsNullOrWhiteSpace(searchFilter))
                return true;

            string filter = searchFilter.Trim();
            switch (searchTarget)
            {
                case PlayerPrefsSearchTarget.Key:
                    return SWEditorUtils.MatchesFilter(entry.key, filter);
                case PlayerPrefsSearchTarget.Value:
                    return SWEditorUtils.MatchesFilter(entry.value, filter);
                case PlayerPrefsSearchTarget.Type:
                    return SWEditorUtils.MatchesFilter(entry.valueType.ToString(), filter);
                default:
                    return SWEditorUtils.MatchesFilter(entry.key, filter) ||
                           SWEditorUtils.MatchesFilter(entry.value, filter) ||
                           SWEditorUtils.MatchesFilter(entry.valueType.ToString(), filter);
            }
        }

        private int GetFilteredEntryCount()
        {
            if (string.IsNullOrWhiteSpace(searchFilter))
                return entries.Count;

            int count = 0;
            foreach (PrefsEntry entry in entries)
            {
                if (MatchesFilter(entry))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// 기본 Unity PlayerPrefs 저장소 접근을 담당합니다.
        /// </summary>
        private static class UnityPlayerPrefsStore
        {
            /// <summary>
            /// 현재 프로젝트의 기본 PlayerPrefs 항목을 불러옵니다.
            /// </summary>
            public static List<PrefsEntry> LoadEntries()
            {
#if UNITY_EDITOR_WIN
                return LoadWindowsEditorEntries();
#else
                return new List<PrefsEntry>();
#endif
            }

            /// <summary>
            /// 기본 PlayerPrefs에 값을 저장합니다.
            /// </summary>
            public static bool SaveEntry(string key, string value, PlayerPrefsValueType valueType, out string errorMessage)
            {
                errorMessage = "";
                key = key?.Trim();
                if (value == null)
                    value = "";

                if (string.IsNullOrWhiteSpace(key))
                {
                    errorMessage = "Key is empty.";
                    return false;
                }

                switch (valueType)
                {
                    case PlayerPrefsValueType.Integer:
                        if (!int.TryParse(value, out int intValue))
                        {
                            errorMessage = $"Integer value parse failed: {value}";
                            return false;
                        }
                        PlayerPrefs.SetInt(key, intValue);
                        break;
                    case PlayerPrefsValueType.Float:
                        if (!float.TryParse(value, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out float floatValue))
                        {
                            errorMessage = $"Float value parse failed: {value}";
                            return false;
                        }
                        PlayerPrefs.SetFloat(key, floatValue);
                        break;
                    default:
                        PlayerPrefs.SetString(key, value);
                        break;
                }

                PlayerPrefs.Save();
                return true;
            }

#if UNITY_EDITOR_WIN
            private static List<PrefsEntry> LoadWindowsEditorEntries()
            {
                var loadedEntries = new List<PrefsEntry>();
                string registryPath = $@"Software\Unity\UnityEditor\{PlayerSettings.companyName}\{PlayerSettings.productName}";

                using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(registryPath))
                {
                    if (registryKey == null)
                        return loadedEntries;

                    foreach (string storedName in registryKey.GetValueNames())
                    {
                        string key = NormalizeWindowsEditorKey(storedName);
                        if (IsSWUtilsManagedKey(key))
                            continue;

                        object storedValue = registryKey.GetValue(storedName);
                        RegistryValueKind valueKind = registryKey.GetValueKind(storedName);
                        loadedEntries.Add(CreateEntry(key, storedValue, valueKind));
                    }
                }

                return loadedEntries;
            }

            private static PrefsEntry CreateEntry(string key, object storedValue, RegistryValueKind valueKind)
            {
                switch (valueKind)
                {
                    case RegistryValueKind.DWord:
                        return new PrefsEntry
                        {
                            key = key,
                            value = Convert.ToInt32(storedValue).ToString(),
                            valueType = PlayerPrefsValueType.Integer
                        };
                    case RegistryValueKind.Binary:
                        byte[] bytes = storedValue as byte[];
                        if (bytes != null && bytes.Length >= 4)
                        {
                            return new PrefsEntry
                            {
                                key = key,
                                value = BitConverter.ToSingle(bytes, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
                                valueType = PlayerPrefsValueType.Float
                            };
                        }
                        break;
                }

                return new PrefsEntry
                {
                    key = key,
                    value = storedValue?.ToString() ?? "",
                    valueType = PlayerPrefsValueType.String
                };
            }

            private static string NormalizeWindowsEditorKey(string storedName)
            {
                int hashSeparatorIndex = storedName.LastIndexOf("_h", StringComparison.Ordinal);
                if (hashSeparatorIndex <= 0 || hashSeparatorIndex + 2 >= storedName.Length)
                    return storedName;

                for (int index = hashSeparatorIndex + 2; index < storedName.Length; index++)
                {
                    if (!char.IsDigit(storedName[index]))
                        return storedName;
                }

                return storedName.Substring(0, hashSeparatorIndex);
            }
#endif

            private static bool IsSWUtilsManagedKey(string key)
            {
                return key.StartsWith("SwEnc_", StringComparison.Ordinal) ||
                       key.StartsWith("SwUtilsPrefs_KeyIndex_", StringComparison.Ordinal);
            }
        }
    }
}
