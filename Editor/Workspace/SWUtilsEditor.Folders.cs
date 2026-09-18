using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using SW.EditorTools.Workspace;

namespace SW.EditorTools.Window
{
    public sealed partial class SWUtilsEditor
    {
        #region 폴더 설정 화면
        /// <summary>탐색 또는 제외 폴더를 여러 개 등록하는 화면을 구성합니다. 잘못된 입력은 기존 설정을 유지합니다.</summary>
        private void BuildFolderSettings(VisualElement parent, bool excluded = false)
        {
            List<string> folders = excluded ? settings.ExcludedFolders : settings.SearchFolders;
            Foldout section = new()
            {
                text = excluded ? "Excluded folders" : "Search folders",
                value = !excluded
            };
            section.AddToClassList("sw-section");
            parent.Add(section);
            Label description = new(excluded
                ? "탐색 폴더 안에서 제외할 폴더입니다. 하위 폴더도 함께 제외합니다."
                : "등록한 폴더와 하위 폴더에서만 검색합니다. SWUtils의 데이터 폴더가 기본값이며, 다른 폴더는 직접 추가하세요.");
            description.AddToClassList("sw-wrap");
            description.AddToClassList("sw-muted");
            section.Add(description);
            HelpBox notice = new("", HelpBoxMessageType.Warning);
            notice.AddToClassList("sw-hidden");
            section.Add(notice);
            ScrollView list = new(ScrollViewMode.Vertical);
            list.style.maxHeight = 150;
            section.Add(list);

            VisualElement addRow = Element("sw-row");
            TextField path = new();
            path.AddToClassList("sw-grow");
            path.textEdition.placeholder = "Assets/... 또는 Packages/...";
            addRow.Add(path);
            ObjectField picker = new()
            {
                objectType = typeof(DefaultAsset),
                allowSceneObjects = false,
                tooltip = "Project 창에서 폴더를 끌어 놓거나 선택하세요."
            };
            picker.style.width = 90;
            addRow.Add(picker);
            section.Add(addRow);

            void ShowNotice(string message)
            {
                notice.text = message;
                notice.EnableInClassList("sw-hidden", string.IsNullOrEmpty(message));
            }

            bool Validate(string input, out string normalized)
            {
                normalized = SWEditorSearchFolders.Normalize(input);
                if (string.IsNullOrEmpty(normalized) || !AssetDatabase.IsValidFolder(normalized))
                {
                    ShowNotice("Assets 또는 Packages 안에 존재하는 폴더 경로를 입력하세요.");
                    return false;
                }

                return true;
            }

            void PersistFolders()
            {
                // 이전 범위를 조회하던 검색이 변경된 설정에 결과를 덮어쓰지 않도록 중단합니다.
                catalog.CancelRefresh();
                searchWasRunning = false;
                settings.Persist();
                RequestRefresh();
                RenderFolders();
            }

            void RenderFolders()
            {
                list.Clear();
                foreach (string folder in folders.ToArray())
                {
                    VisualElement row = Element("sw-settings-category-row");
                    TextField field = new()
                    {
                        value = folder,
                        isDelayed = true,
                        tooltip = folder
                    };
                    field.AddToClassList("sw-grow");
                    field.RegisterValueChangedCallback(eventData =>
                    {
                        if (!Validate(eventData.newValue, out string changed))
                        {
                            field.SetValueWithoutNotify(folder);
                            return;
                        }

                        if (!string.Equals(changed, folder, StringComparison.OrdinalIgnoreCase) &&
                            folders.Any(existing => string.Equals(existing, changed, StringComparison.OrdinalIgnoreCase)))
                        {
                            ShowNotice("이미 등록한 폴더입니다.");
                            field.SetValueWithoutNotify(folder);
                            return;
                        }

                        int index = folders.IndexOf(folder);
                        if (index >= 0)
                        {
                            folders[index] = changed;
                            PersistFolders();
                        }
                    });
                    row.Add(field);
                    row.Add(new Button(() =>
                    {
                        folders.Remove(folder);
                        PersistFolders();
                    }) { text = "Remove" });
                    list.Add(row);
                }

                string[] missing = folders.Where(folder => !AssetDatabase.IsValidFolder(folder)).ToArray();
                ShowNotice(missing.Length > 0
                    ? "존재하지 않는 폴더는 검색하지 않습니다: " + string.Join(", ", missing)
                    : !excluded && folders.Count == 0 ? "탐색 폴더가 없습니다. 폴더를 추가해야 에셋이 표시됩니다." : "");
            }

            void AddFolder()
            {
                if (!Validate(path.value, out string folder))
                {
                    return;
                }

                if (folders.Any(existing => string.Equals(existing, folder, StringComparison.OrdinalIgnoreCase)))
                {
                    ShowNotice("이미 등록한 폴더입니다.");
                    return;
                }

                folders.Add(folder);
                path.SetValueWithoutNotify("");
                picker.SetValueWithoutNotify(null);
                PersistFolders();
            }

            picker.RegisterValueChangedCallback(eventData =>
            {
                if (eventData.newValue != null)
                {
                    path.SetValueWithoutNotify(AssetDatabase.GetAssetPath(eventData.newValue));
                }
            });
            addRow.Add(new Button(AddFolder) { text = "Add" });
            if (!excluded)
            {
                section.Add(new Button(() =>
                {
                    string[] defaults = SWEditorSearchFolders.GetDefaults();
                    if (defaults.Length == 0)
                    {
                        ShowNotice("SWUtils 데이터 폴더를 찾지 못했습니다. 탐색할 폴더를 직접 추가하세요.");
                        return;
                    }

                    foreach (string folder in defaults)
                    {
                        if (!folders.Any(existing => string.Equals(existing, folder, StringComparison.OrdinalIgnoreCase)))
                        {
                            folders.Add(folder);
                        }
                    }

                    PersistFolders();
                }) { text = "Add SWUtils data folders" });
            }

            RenderFolders();
        }
        #endregion // 폴더 설정 화면
    }
}
