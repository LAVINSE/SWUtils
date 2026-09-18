using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SW.EditorTools.Workspace
{
    /// <summary>사용자 생성 흐름, Unity 생성 메뉴, 직접 생성 순서로 에셋을 생성합니다.</summary>
    [InitializeOnLoad]
    public static class SWEditorAssetCreation
    {
        private static SWEditorAssetType pendingType;
        private static SWEditorCreationWorkflow pendingWorkflow;
        private static string pendingPath;
        private static string pendingCategory;
        private static HashSet<string> previousAssets;
        private static double startedAt;
        private static double nextObservationTime;
        private static Action<ScriptableObject> completed;
        /// <summary>Unity 기본 생성 메뉴가 완료되기를 기다리는지 나타냅니다.</summary>
        public static bool IsPending => pendingType != null;

        static SWEditorAssetCreation()
        {
            EditorApplication.update += ObserveNativeCreation;
        }

        /// <summary>저장 위치를 선택하고 에셋 생성을 시작합니다.</summary>
        public static void Create(SWEditorAssetType type, Action<ScriptableObject> onCreated)
        {
            if (IsPending)
                return;
            SWEditorCreationWorkflow workflow = SWEditorRegistry.GetCreationWorkflow(type.Type);
            string defaultName = SWEditorRegistry.Protect(() => workflow?.DefaultFileName?.Invoke(type.Type)) ?? type.FileName;
            string initialFolder = SWEditorSearchFolders.GetValidFolders(SWEditorWorkspaceSettings.instance.SearchFolders)
                .FirstOrDefault(folder => folder == "Assets" || folder.StartsWith("Assets/", StringComparison.Ordinal)) ?? "Assets";
            string path = EditorUtility.SaveFilePanelInProject("Create asset", Path.GetFileNameWithoutExtension(defaultName),
                "asset", "새 에셋을 저장할 위치를 선택하세요.", initialFolder);
            if (string.IsNullOrEmpty(path))
                return;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                EditorUtility.DisplayDialog("Create asset", "기존 파일을 덮어쓰지 않도록 다른 이름을 선택하세요.", "OK");
                return;
            }

            pendingType = type;
            pendingWorkflow = workflow;
            pendingPath = path;
            completed = onCreated;
            pendingCategory = SWEditorWorkspaceSettings.instance.SelectedCategory;
            try
            {
                ScriptableObject custom = workflow?.Create?.Invoke(path);
                if (custom != null)
                {
                    if (!type.Type.IsInstanceOfType(custom) || !AssetDatabase.Contains(custom))
                        throw new InvalidOperationException("생성 흐름은 요청한 유형의 저장된 에셋을 반환해야 합니다.");
                    Complete(custom);
                    return;
                }

                if (!string.IsNullOrEmpty(type.CreationMenu))
                {
                    previousAssets = new HashSet<string>(FindPendingFolderAssets());
                    startedAt = EditorApplication.timeSinceStartup;
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(Path.GetDirectoryName(path).Replace('\\', '/'));
                    if (EditorApplication.ExecuteMenuItem(type.CreationMenu))
                        return;
                }

                ScriptableObject asset = ScriptableObject.CreateInstance(type.Type);
                try
                {
                    SWEditorRegistry.Protect(() => SWEditorRegistry.GetCreator(type.Type)?.Invoke(asset));
                    foreach (SWEditorCreateAction action in SWEditorRegistry.RegisteredCreateActions.Where(item => item.AssetType == type.Type))
                        SWEditorRegistry.Protect(() => action.Configure?.Invoke(asset, path));
                    AssetDatabase.CreateAsset(asset, path);
                    Complete(asset);
                }
                catch
                {
                    if (!AssetDatabase.Contains(asset))
                        UnityEngine.Object.DestroyImmediate(asset);
                    throw;
                }
            }
            catch (Exception exception)
            {
                Cancel();
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Create asset", exception.Message, "OK");
            }
        }

        /// <summary>생성 완료 감시를 해제하며 이미 만들어진 파일은 유지합니다.</summary>
        public static void Cancel()
        {
            pendingType = null;
            pendingWorkflow = null;
            previousAssets = null;
            completed = null;
        }

        private static void ObserveNativeCreation()
        {
            if (pendingType == null || previousAssets == null)
                return;
            if (EditorApplication.timeSinceStartup - startedAt > 120)
            {
                Cancel();
                return;
            }

            if (EditorApplication.timeSinceStartup < nextObservationTime)
                return;
            nextObservationTime = EditorApplication.timeSinceStartup + 0.3;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorGUIUtility.editingTextField)
                return;
            foreach (string identifier in FindPendingFolderAssets())
            {
                if (previousAssets.Contains(identifier))
                    continue;
                string path = AssetDatabase.GUIDToAssetPath(identifier);
                if (!string.Equals(Path.GetDirectoryName(path), Path.GetDirectoryName(pendingPath), StringComparison.OrdinalIgnoreCase))
                    continue;
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null || asset.GetType() != pendingType.Type)
                    continue;
                if (path != pendingPath && AssetDatabase.LoadMainAssetAtPath(pendingPath) == null)
                {
                    string error = AssetDatabase.MoveAsset(path, pendingPath);
                    if (!string.IsNullOrEmpty(error))
                        Debug.LogWarning(error);
                }

                Complete(asset);
                return;
            }
        }

        /// <summary>생성을 요청한 폴더에서만 새 에셋을 감시합니다. 폴더가 사라졌으면 검색하지 않습니다.</summary>
        private static string[] FindPendingFolderAssets()
        {
            string folder = Path.GetDirectoryName(pendingPath)?.Replace('\\', '/');
            if (pendingType == null || !AssetDatabase.IsValidFolder(folder))
            {
                return Array.Empty<string>();
            }

            return AssetDatabase.FindAssets("t:" + pendingType.Type.Name, new[] { folder });
        }

        private static void Complete(ScriptableObject asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            SWEditorRegistry.Protect(() => pendingWorkflow?.OnCreated?.Invoke(asset, path));
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            SWEditorWorkspaceSettings settings = SWEditorWorkspaceSettings.instance;
            settings.RecentTypes.Remove(pendingType.Settings.TypeName);
            settings.RecentTypes.Insert(0, pendingType.Settings.TypeName);
            if (settings.RecentTypes.Count > 20)
                settings.RecentTypes.RemoveRange(20, settings.RecentTypes.Count - 20);
            string identifier = SWEditorAssetCatalog.GetIdentifier(asset);
            if (settings.HasCategory(pendingCategory))
                settings.AssignCategory(identifier, pendingCategory);
            string category = settings.ResolveCategory(identifier, pendingType.Settings, path);
            settings.Persist();
            Action<ScriptableObject> callback = completed;
            Cancel();
            SWEditorRegistry.Protect(() => callback?.Invoke(asset));
            SWEditorEvents.RaiseAssetChanged(new SWEditorAssetChange { Kind = SWEditorAssetChangeKind.Created, Context = new SWEditorAssetContext { Asset = asset, AssetType = asset.GetType(), AssetPath = path, AssetIdentifier = identifier, CategoryIdentifier = category, IsOpen = settings.OpenAssets.Contains(identifier), IsActive = settings.ActiveAsset == identifier, IsFavourite = settings.Favourites.Contains(identifier) } });
        }
    }
}
