using System;
using System.IO;
using UnityEditor;
using UnityEngine;

using SW.Data;

using SW.EditorTools.Util;

namespace SW.EditorTools.Window
{
    /// <summary>
    /// SWPlayerPrefs 암호화 salt 설정 에셋을 생성하고 수정하는 에디터 창입니다.
    /// </summary>
    public class SWPlayerPrefsSettingsWindow : EditorWindow
    {
        private const string ResourcesFolderPath = "Assets/Resources";
        private const string SettingsAssetPath = ResourcesFolderPath + "/" + SWPlayerPrefsSettings.ResourceAssetName + ".asset";

        private SWPlayerPrefsSettings settings;
        private string salt;
        private string ivSalt;
        private string statusMessage;

        /// <summary>
        /// SWPlayerPrefs 설정 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Utils/PlayerPrefs Salt Settings")]
        public static void ShowWindow()
        {
            SWPlayerPrefsSettingsWindow window = GetWindow<SWPlayerPrefsSettingsWindow>();
            SWEditorUtils.SetupWindow(window, "SW PlayerPrefs Settings", "d_SettingsIcon", 460, 330);
            window.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void OnGUI()
        {
            DrawAssetSection();
            EditorGUILayout.Space(8);
            DrawSaltSection();
            EditorGUILayout.Space(8);
            DrawWarningSection();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }
        }

        private void DrawAssetSection()
        {
            SWEditorUtils.DrawHeader("Settings Asset");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Current Asset", settings, typeof(SWPlayerPrefsSettings), false);
                EditorGUILayout.TextField("Runtime Path", "Resources/" + SWPlayerPrefsSettings.ResourceAssetName);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Find Or Create", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
            {
                LoadOrCreateSettings();
            }

            using (new SWEditorUtils.GUIEnabledScope(settings != null))
            {
                if (GUILayout.Button("Select Asset", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
                {
                    Selection.activeObject = settings;
                    EditorGUIUtility.PingObject(settings);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSaltSection()
        {
            SWEditorUtils.DrawHeader("Salt");

            salt = EditorGUILayout.TextField("Salt", salt);
            ivSalt = EditorGUILayout.TextField("Initialization Vector Salt", ivSalt);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Salt", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
            {
                salt = GenerateSalt();
            }

            if (GUILayout.Button("Generate Vector Salt", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
            {
                ivSalt = GenerateSalt();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Default", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
            {
                salt = SWPlayerPrefsSettings.DefaultSalt;
                ivSalt = SWPlayerPrefsSettings.DefaultIVSalt;
            }

            using (new SWEditorUtils.GUIEnabledScope(!string.IsNullOrWhiteSpace(salt) && !string.IsNullOrWhiteSpace(ivSalt)))
            {
                if (GUILayout.Button("Save Settings", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
                {
                    SaveSettings();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawWarningSection()
        {
            EditorGUILayout.HelpBox(
                "Salt 값을 변경하면 이전 salt로 저장된 SWPlayerPrefs 데이터는 읽을 수 없습니다. 변경 전에 데이터를 삭제하거나 별도 마이그레이션을 준비하세요.",
                MessageType.Warning);
        }

        private void LoadSettings()
        {
            settings = FindSettings();
            PullValuesFromSettings();
        }

        private void LoadOrCreateSettings()
        {
            settings = FindSettings();
            if (settings == null)
            {
                settings = CreateSettings();
                statusMessage = "Created SWPlayerPrefs settings asset.";
            }
            else
            {
                statusMessage = "Loaded existing SWPlayerPrefs settings asset.";
            }

            PullValuesFromSettings();
        }

        private void PullValuesFromSettings()
        {
            salt = settings != null ? settings.Salt : SWPlayerPrefsSettings.DefaultSalt;
            ivSalt = settings != null ? settings.IVSalt : SWPlayerPrefsSettings.DefaultIVSalt;
        }

        private void SaveSettings()
        {
            if (settings == null)
            {
                settings = CreateSettings();
            }

            Undo.RecordObject(settings, "Update SWPlayerPrefs Settings");
            settings.SetValues(salt, ivSalt);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SWPlayerPrefs.ReloadSettings();

            PullValuesFromSettings();
            statusMessage = "Saved SWPlayerPrefs settings.";
        }

        private static SWPlayerPrefsSettings FindSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:SWPlayerPrefsSettings");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsResourcesPath(path)) continue;

                var asset = AssetDatabase.LoadAssetAtPath<SWPlayerPrefsSettings>(path);
                if (asset != null) return asset;
            }

            return AssetDatabase.LoadAssetAtPath<SWPlayerPrefsSettings>(SettingsAssetPath);
        }

        private static SWPlayerPrefsSettings CreateSettings()
        {
            EnsureResourcesFolder();

            var asset = CreateInstance<SWPlayerPrefsSettings>();
            asset.SetValues(SWPlayerPrefsSettings.DefaultSalt, SWPlayerPrefsSettings.DefaultIVSalt);
            AssetDatabase.CreateAsset(asset, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }

        private static void EnsureResourcesFolder()
        {
            if (AssetDatabase.IsValidFolder(ResourcesFolderPath)) return;

            if (!AssetDatabase.IsValidFolder("Assets"))
            {
                Directory.CreateDirectory("Assets");
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        private static bool IsResourcesPath(string path)
        {
            string normalizedPath = path.Replace('\\', '/');
            return normalizedPath.IndexOf("/Resources/", StringComparison.Ordinal) >= 0
                || normalizedPath.StartsWith("Assets/Resources/", StringComparison.Ordinal);
        }

        private static string GenerateSalt()
        {
            return "SWUtils_" + Guid.NewGuid().ToString("N");
        }
    }
}
