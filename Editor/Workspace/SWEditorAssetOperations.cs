using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SW.EditorTools.Workspace
{
    /// <summary>탐색기의 파일 작업과 상태 변경을 담당합니다.</summary>
    public sealed class SWEditorAssetOperations
    {
        private readonly SWEditorWorkspaceSettings settings;
        private readonly SWEditorAssetCatalog catalog;
        private readonly Action refresh;
        private readonly Action<ScriptableObject> open;
        /// <summary>탐색기 작업 서비스를 생성합니다.</summary>
        public SWEditorAssetOperations(SWEditorWorkspaceSettings settings, SWEditorAssetCatalog catalog, Action refresh, Action<ScriptableObject> open)
        {
            this.settings = settings;
            this.catalog = catalog;
            this.refresh = refresh;
            this.open = open;
        }

        /// <summary>파일 자체를 변경할 수 있는 독립 프로젝트 에셋인지 검사합니다.</summary>
        public static bool CanChangeFile(SWEditorAssetEntry entry)
        {
            return entry != null && entry.Asset != null && entry.Path.StartsWith("Assets/", StringComparison.Ordinal) && AssetDatabase.IsMainAsset(entry.Asset) && AssetDatabase.IsOpenForEdit(entry.Path);
        }

        /// <summary>즐겨찾기 상태를 변경합니다.</summary>
        public void Favourite(SWEditorAssetEntry entry, bool enabled)
        {
            settings.Favourites.Remove(entry.Identifier);
            if (enabled)
                settings.Favourites.Add(entry.Identifier);
            Changed(entry, SWEditorAssetChangeKind.Favourited);
        }

        /// <summary>에셋별 분류를 변경합니다.</summary>
        public void Categorise(SWEditorAssetEntry entry, string categoryIdentifier)
        {
            string previous = catalog.Context(entry).CategoryIdentifier;
            settings.AssignCategory(entry.Identifier, categoryIdentifier);
            Changed(entry, SWEditorAssetChangeKind.Categorised, previousCategory: previous);
        }

        /// <summary>파일 이름을 변경합니다.</summary>
        public void Rename(SWEditorAssetEntry entry, string newName)
        {
            if (!CanChangeFile(entry) || string.IsNullOrWhiteSpace(newName) || newName == entry.Asset.name)
                return;
            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                EditorUtility.DisplayDialog("Rename asset", "파일 이름에 사용할 수 없는 문자가 있습니다.", "OK");
                return;
            }

            string previous = entry.Path;
            string error = AssetDatabase.RenameAsset(entry.Path, newName.Trim());
            if (!CheckError(error))
                return;
            entry.Path = AssetDatabase.GetAssetPath(entry.Asset);
            Changed(entry, SWEditorAssetChangeKind.Moved, previous);
        }

        /// <summary>원본 옆에 복사본을 만들고 엽니다.</summary>
        public void Duplicate(SWEditorAssetEntry entry)
        {
            string directory = entry.Path.StartsWith("Assets/", StringComparison.Ordinal) ? Path.GetDirectoryName(entry.Path).Replace('\\', '/') : "Assets";
            string destination = AssetDatabase.GenerateUniqueAssetPath(directory + "/" + entry.Asset.name + " Copy.asset");
            ScriptableObject copy;
            if (AssetDatabase.IsMainAsset(entry.Asset) && Path.GetExtension(entry.Path) == ".asset")
            {
                if (!AssetDatabase.CopyAsset(entry.Path, destination))
                    return;
                copy = AssetDatabase.LoadAssetAtPath<ScriptableObject>(destination);
            }
            else
            {
                copy = UnityEngine.Object.Instantiate(entry.Asset);
                copy.name = Path.GetFileNameWithoutExtension(destination);
                AssetDatabase.CreateAsset(copy, destination);
            }

            if (copy == null)
                return;
            settings.AssignCategory(SWEditorAssetCatalog.GetIdentifier(copy), catalog.Context(entry).CategoryIdentifier);
            AssetDatabase.SaveAssets();
            catalog.Refresh();
            open(copy);
            SWEditorAssetEntry created = catalog.Find(SWEditorAssetCatalog.GetIdentifier(copy));
            if (created != null)
                Changed(created, SWEditorAssetChangeKind.Duplicated);
        }

        /// <summary>저장 대화상자를 통해 파일을 이동하거나 이름을 변경합니다.</summary>
        public void Move(SWEditorAssetEntry entry)
        {
            if (!CanChangeFile(entry))
                return;
            string destination = EditorUtility.SaveFilePanelInProject("Move / Rename", entry.Asset.name, Path.GetExtension(entry.Path).TrimStart('.'), "새 위치를 선택하세요.", Path.GetDirectoryName(entry.Path));
            if (string.IsNullOrEmpty(destination) || destination == entry.Path)
                return;
            string previous = entry.Path;
            if (!CheckError(AssetDatabase.MoveAsset(previous, destination)))
                return;
            entry.Path = destination;
            Changed(entry, SWEditorAssetChangeKind.Moved, previous);
        }

        /// <summary>사용자가 확인한 에셋 파일만 삭제합니다.</summary>
        public void Delete(SWEditorAssetEntry entry)
        {
            if (!CanChangeFile(entry) || !EditorUtility.DisplayDialog("Delete asset", $"'{entry.Asset.name}' 파일을 삭제할까요?\n{entry.Path}\n파일에 포함된 하위 에셋도 함께 삭제됩니다.", "Delete", "Cancel"))
                return;
            SWEditorAssetContext context = catalog.Context(entry);
            if (!AssetDatabase.DeleteAsset(entry.Path))
                return;
            settings.Favourites.Remove(entry.Identifier);
            settings.Assignments.RemoveAll(item => item.AssetIdentifier == entry.Identifier);
            settings.OpenAssets.Remove(entry.Identifier);
            settings.Persist();
            SWEditorEvents.RaiseAssetChanged(new SWEditorAssetChange { Kind = SWEditorAssetChangeKind.Deleted, Context = context, PreviousAssetPath = entry.Path });
            refresh();
        }

        /// <summary>직렬화 변경이나 확장 작업 완료를 알립니다.</summary>
        public void Updated(SWEditorAssetEntry entry)
        {
            Changed(entry, SWEditorAssetChangeKind.Updated);
        }

        private void Changed(SWEditorAssetEntry entry, SWEditorAssetChangeKind kind, string previousPath = null, string previousCategory = null)
        {
            settings.Persist();
            SWEditorEvents.RaiseAssetChanged(new SWEditorAssetChange { Kind = kind, Context = catalog.Context(entry), PreviousAssetPath = previousPath, PreviousCategoryIdentifier = previousCategory });
            refresh();
        }

        private static bool CheckError(string error)
        {
            if (string.IsNullOrEmpty(error))
                return true;
            EditorUtility.DisplayDialog("Asset operation", error, "OK");
            return false;
        }
    }
}
