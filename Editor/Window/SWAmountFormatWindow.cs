using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

using SW.EditorTools.Util;

using SW.Util;

namespace SW.EditorTools.Window
{
    /// <summary>
    /// SWAmountFormatProfile을 생성하고 편집하는 에디터 윈도우입니다.
    /// </summary>
    public class SWAmountFormatWindow : EditorWindow
    {
        #region 필드
        private const string ResourcesFolderPath = "Assets/Resources";
        private const string DefaultProfilePath = ResourcesFolderPath + "/" + SWAmountFormatProfile.ResourceAssetName + ".asset";
        private const string EditorPrefsProfileGuidKey = "SWUtils.AmountFormatWindow.ProfileGuid";
        private const string SampleValues = "999, 1000, 15300, 1250000, -9876543210";

        private readonly List<SWAmountFormatProfile> profiles = new List<SWAmountFormatProfile>();
        private readonly List<string> profileNames = new List<string>();

        private SWAmountFormatProfile activeProfile;
        private SerializedObject activeSerializedObject;
        private Vector2 scrollPosition;
        private string amountText = "1234567";
        #endregion // 필드

        #region 열기
        /// <summary>
        /// Amount Format 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Utils/Data/Amount Format Window")]
        public static void OpenWindow()
        {
            var window = GetWindow<SWAmountFormatWindow>();
            SWEditorUtils.SetupWindow(window, "Amount Format", "d_FilterByLabel", 420, 460);
            window.Show();
        }
        #endregion // 열기

        #region 생명주기
        private void OnEnable()
        {
            RefreshProfiles();
            SelectStoredProfileOrDefault();
        }
        #endregion // 생명주기

        #region GUI
        private void OnGUI()
        {
            DrawProfileSection();

            if (activeProfile == null || activeSerializedObject == null)
            {
                EditorGUILayout.HelpBox("사용할 Amount Format Profile을 생성하세요.", MessageType.Info);
                if (GUILayout.Button("기본 프리셋 생성", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
                {
                    CreateDefaultProfile();
                }
                return;
            }

            activeSerializedObject.Update();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawPreviewSection();
            EditorGUILayout.Space(8f);
            DrawSettingSection();
            EditorGUILayout.Space(8f);
            DrawUnitSection();
            EditorGUILayout.Space(8f);
            DrawSampleSection();

            EditorGUILayout.EndScrollView();

            if (activeSerializedObject.ApplyModifiedProperties())
            {
                activeProfile.MarkDirty();
                EditorUtility.SetDirty(activeProfile);
            }
        }

        private void DrawProfileSection()
        {
            SWEditorUtils.DrawHeader("프리셋");

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(profiles.Count == 0))
                {
                    int selectedIndex = Mathf.Max(0, profiles.IndexOf(activeProfile));
                    int newIndex = EditorGUILayout.Popup(
                        new GUIContent("활성 프리셋", "Resources 폴더 안에서 찾은 숫자 포맷 프리셋입니다."),
                        selectedIndex,
                        profileNames.ToArray());
                    if (profiles.Count > 0 && newIndex >= 0 && newIndex < profiles.Count && profiles[newIndex] != activeProfile)
                    {
                        SetActiveProfile(profiles[newIndex]);
                    }
                }

                if (GUILayout.Button(new GUIContent("새로고침", "Resources 폴더의 프리셋 목록을 다시 찾습니다."), GUILayout.Width(70f)))
                {
                    RefreshProfiles();
                    SelectStoredProfileOrDefault();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("기본 프리셋 생성", "Assets/Resources 폴더에 기본 프리셋을 생성하거나 불러옵니다."), GUILayout.Height(SWEditorUtils.SmallButtonHeight)))
                {
                    CreateDefaultProfile();
                }

                using (new EditorGUI.DisabledScope(activeProfile == null))
                {
                    if (GUILayout.Button(new GUIContent("프리셋 찾기", "Project 창에서 현재 프리셋 위치를 표시합니다."), GUILayout.Height(SWEditorUtils.SmallButtonHeight)))
                    {
                        EditorGUIUtility.PingObject(activeProfile);
                    }
                }
            }
        }

        private void DrawPreviewSection()
        {
            SWEditorUtils.DrawHeader("미리보기");

            amountText = EditorGUILayout.TextField(new GUIContent("숫자", "포맷 결과를 확인할 숫자입니다."), amountText);
            if (TryParseAmount(amountText, out decimal amount))
            {
                EditorGUILayout.SelectableLabel(
                    activeProfile.Format(amount),
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
            else
            {
                EditorGUILayout.HelpBox("숫자를 입력하세요.", MessageType.Warning);
            }
        }

        private void DrawSettingSection()
        {
            SWEditorUtils.DrawHeader("설정");

            EditorGUILayout.PropertyField(
                activeSerializedObject.FindProperty("decimalPlaces"),
                new GUIContent("소수점 자리수", "단위가 적용된 숫자에 표시할 소수점 자리수입니다."));
            EditorGUILayout.PropertyField(
                activeSerializedObject.FindProperty("keepTrailingZeros"),
                new GUIContent("끝자리 0 유지", "1.0K처럼 남는 소수점 0을 유지합니다."));
            EditorGUILayout.PropertyField(
                activeSerializedObject.FindProperty("useGroupSeparator"),
                new GUIContent("천 단위 구분자", "단위가 적용되지 않은 숫자에 1,000 형식의 구분자를 사용합니다."));
            EditorGUILayout.PropertyField(
                activeSerializedObject.FindProperty("roundingMode"),
                new GUIContent("소수점 처리", "버림, 반올림, 내림, 올림 중 포맷 방식을 선택합니다."));

            if (GUILayout.Button(new GUIContent("기본값으로 초기화", "현재 프리셋을 기본 단위와 설정으로 되돌립니다."), GUILayout.Height(SWEditorUtils.SmallButtonHeight)))
            {
                Undo.RecordObject(activeProfile, "Reset Amount Format Profile");
                activeProfile.ResetProfile();
                EditorUtility.SetDirty(activeProfile);
                activeSerializedObject.Update();
            }
        }

        private void DrawUnitSection()
        {
            SWEditorUtils.DrawHeader("단위");

            SerializedProperty unitsProperty = activeSerializedObject.FindProperty("units");
            for (int i = 0; i < unitsProperty.arraySize; i++)
            {
                SerializedProperty unitProperty = unitsProperty.GetArrayElementAtIndex(i);
                SerializedProperty thresholdProperty = unitProperty.FindPropertyRelative("thresholdText");
                SerializedProperty suffixProperty = unitProperty.FindPropertyRelative("suffix");

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(
                        thresholdProperty,
                        new GUIContent(string.Empty, "이 숫자 이상이면 해당 단위를 적용합니다."));
                    EditorGUILayout.PropertyField(
                        suffixProperty,
                        new GUIContent(string.Empty, "숫자 뒤에 붙일 단위 문자열입니다."),
                        GUILayout.Width(64f));

                    using (new EditorGUI.DisabledScope(i <= 0))
                    {
                        if (GUILayout.Button("▲", GUILayout.Width(24f)))
                        {
                            unitsProperty.MoveArrayElement(i, i - 1);
                        }
                    }

                    using (new EditorGUI.DisabledScope(i >= unitsProperty.arraySize - 1))
                    {
                        if (GUILayout.Button("▼", GUILayout.Width(24f)))
                        {
                            unitsProperty.MoveArrayElement(i, i + 1);
                        }
                    }

                    if (GUILayout.Button("X", GUILayout.Width(24f)))
                    {
                        unitsProperty.DeleteArrayElementAtIndex(i);
                        GUIUtility.ExitGUI();
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("단위 추가", "새 숫자 단위를 목록에 추가합니다."), GUILayout.Height(SWEditorUtils.SmallButtonHeight)))
                {
                    int index = unitsProperty.arraySize;
                    unitsProperty.InsertArrayElementAtIndex(index);
                    SerializedProperty unitProperty = unitsProperty.GetArrayElementAtIndex(index);
                    unitProperty.FindPropertyRelative("thresholdText").stringValue = "1000";
                    unitProperty.FindPropertyRelative("suffix").stringValue = "K";
                }

                if (GUILayout.Button(new GUIContent("큰 단위순 정렬", "기준값이 큰 단위부터 작은 단위 순서로 정렬합니다."), GUILayout.Height(SWEditorUtils.SmallButtonHeight)))
                {
                    SortUnitsDescending(unitsProperty);
                }
            }
        }

        private void DrawSampleSection()
        {
            SWEditorUtils.DrawHeader("샘플");

            string[] samples = SampleValues.Split(',');
            for (int i = 0; i < samples.Length; i++)
            {
                string sample = samples[i].Trim();
                if (!TryParseAmount(sample, out decimal amount)) continue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(sample, GUILayout.Width(120f));
                    EditorGUILayout.SelectableLabel(
                        activeProfile.Format(amount),
                        EditorStyles.textField,
                        GUILayout.Height(EditorGUIUtility.singleLineHeight));
                }
            }
        }
        #endregion // GUI

        #region 프로필
        private void RefreshProfiles()
        {
            profiles.Clear();
            profileNames.Clear();

            string[] guids = AssetDatabase.FindAssets("t:SWAmountFormatProfile");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!IsResourcesPath(path)) continue;

                var profile = AssetDatabase.LoadAssetAtPath<SWAmountFormatProfile>(path);
                if (profile == null) continue;

                profiles.Add(profile);
                profileNames.Add($"{profile.name} ({path})");
            }
        }

        private void SelectStoredProfileOrDefault()
        {
            string storedGuid = EditorPrefs.GetString(EditorPrefsProfileGuidKey, string.Empty);
            SWAmountFormatProfile storedProfile = LoadProfileFromGuid(storedGuid);

            if (storedProfile != null)
            {
                SetActiveProfile(storedProfile);
                return;
            }

            if (profiles.Count > 0)
            {
                SetActiveProfile(profiles[0]);
                return;
            }

            CreateDefaultProfile();
        }

        private SWAmountFormatProfile LoadProfileFromGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
            if (!IsResourcesPath(path)) return null;

            return AssetDatabase.LoadAssetAtPath<SWAmountFormatProfile>(path);
        }

        private void SetActiveProfile(SWAmountFormatProfile profile)
        {
            activeProfile = profile;
            activeSerializedObject = activeProfile != null ? new SerializedObject(activeProfile) : null;

            string path = activeProfile != null ? AssetDatabase.GetAssetPath(activeProfile) : string.Empty;
            string guid = !string.IsNullOrEmpty(path) ? AssetDatabase.AssetPathToGUID(path) : string.Empty;
            EditorPrefs.SetString(EditorPrefsProfileGuidKey, guid);
        }

        private void CreateDefaultProfile()
        {
            EnsureResourcesFolder();

            var profile = AssetDatabase.LoadAssetAtPath<SWAmountFormatProfile>(DefaultProfilePath);
            if (profile == null)
            {
                profile = CreateInstance<SWAmountFormatProfile>();
                profile.ResetProfile();
                AssetDatabase.CreateAsset(profile, DefaultProfilePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            RefreshProfiles();
            SetActiveProfile(profile);
            EditorGUIUtility.PingObject(profile);
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
        #endregion // 프로필

        #region 유틸리티
        private static bool TryParseAmount(string text, out decimal amount)
        {
            return decimal.TryParse(
                text,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out amount);
        }

        private static void SortUnitsDescending(SerializedProperty unitsProperty)
        {
            var entries = new List<UnitSortEntry>();
            for (int i = 0; i < unitsProperty.arraySize; i++)
            {
                SerializedProperty unitProperty = unitsProperty.GetArrayElementAtIndex(i);
                string thresholdText = unitProperty.FindPropertyRelative("thresholdText").stringValue;
                string suffix = unitProperty.FindPropertyRelative("suffix").stringValue;

                TryParseAmount(thresholdText, out decimal threshold);
                entries.Add(new UnitSortEntry(thresholdText, suffix, threshold));
            }

            entries.Sort((left, right) => right.threshold.CompareTo(left.threshold));
            unitsProperty.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                SerializedProperty unitProperty = unitsProperty.GetArrayElementAtIndex(i);
                unitProperty.FindPropertyRelative("thresholdText").stringValue = entries[i].thresholdText;
                unitProperty.FindPropertyRelative("suffix").stringValue = entries[i].suffix;
            }
        }

        /// <summary>
        /// 단위 정렬에 사용하는 임시 데이터입니다.
        /// </summary>
        private readonly struct UnitSortEntry
        {
            /// <summary>파싱 전 기준값 문자열입니다.</summary>
            public readonly string thresholdText;
            /// <summary>숫자 뒤에 표시할 단위입니다.</summary>
            public readonly string suffix;
            /// <summary>정렬에 사용할 기준값입니다.</summary>
            public readonly decimal threshold;

            /// <summary>
            /// 단위 문자열과 정렬용 기준값을 설정합니다.
            /// </summary>
            /// <param name="thresholdText">파싱 전 기준값 문자열입니다.</param>
            /// <param name="suffix">숫자 뒤에 표시할 단위입니다.</param>
            /// <param name="threshold">정렬에 사용할 기준값입니다.</param>
            public UnitSortEntry(string thresholdText, string suffix, decimal threshold)
            {
                this.thresholdText = thresholdText;
                this.suffix = suffix;
                this.threshold = threshold;
            }
        }
        #endregion // 유틸리티
    }
}
