using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

using SW.Util;

namespace SW.Data
{
    /// <summary>
    /// 게임 진행 데이터, SWPlayerPrefs, 클라우드 백업을 한 번에 관리하는 저장 매니저.
    /// SetData로 저장할 데이터 인스턴스를 등록한 뒤 SaveAll/LoadAll을 사용합니다.
    /// </summary>
    public static class SWSaveDataManager
    {
        #region 중첩 타입
        /// <summary>
        /// 저장 슬롯 파일의 간단한 정보.
        /// </summary>
        public readonly struct SaveFileInfo
        {
            /// <summary>
            /// 저장 슬롯 파일 정보를 생성합니다.
            /// </summary>
            /// <param name="slot">저장 슬롯 이름</param>
            /// <param name="fileName">저장 파일 이름</param>
            /// <param name="path">저장 파일 전체 경로</param>
            /// <param name="exists">저장 파일 존재 여부</param>
            /// <param name="size">저장 파일 크기</param>
            /// <param name="lastWriteTimeUtc">마지막 수정 시각(UTC)</param>
            public SaveFileInfo(string slot, string fileName, string path, bool exists, long size, DateTime lastWriteTimeUtc)
            {
                Slot = slot;
                FileName = fileName;
                Path = path;
                Exists = exists;
                Size = size;
                LastWriteTimeUtc = lastWriteTimeUtc;
            }

            /// <summary>저장 슬롯 이름.</summary>
            public string Slot { get; }
            /// <summary>저장 파일 이름.</summary>
            public string FileName { get; }
            /// <summary>저장 파일 전체 경로.</summary>
            public string Path { get; }
            /// <summary>저장 파일 존재 여부.</summary>
            public bool Exists { get; }
            /// <summary>저장 파일 크기.</summary>
            public long Size { get; }
            /// <summary>마지막 수정 시각(UTC).</summary>
            public DateTime LastWriteTimeUtc { get; }
        }

        [Serializable]
        private class CloudBackupData
        {
            /// <summary>클라우드 백업 데이터 버전.</summary>
            public int version = 1;
            /// <summary>백업 대상 저장 슬롯 이름.</summary>
            public string slot;
            /// <summary>저장 데이터 JSON.</summary>
            public string saveDataJson;
            /// <summary>PlayerPrefs 백업 JSON.</summary>
            public string playerPrefsJson;
            /// <summary>백업 저장 시각(UTC).</summary>
            public string savedAtUtc;
            /// <summary>백업한 기기에서 저장한 횟수(충돌 안내용).</summary>
            public int revision;
        }

        /// <summary>
        /// 슬롯마다 기기에 따로 두는 동기화 기록(슬롯.json.sync)입니다. 클라우드에 올리지 않습니다.
        /// 마지막으로 기기와 클라우드가 같았던 내용의 지문을 기억해, 어느 쪽이 바뀌었는지 판단합니다.
        /// </summary>
        [Serializable]
        private class SyncState
        {
            /// <summary>이 기기에서 저장한 횟수.</summary>
            public int revision;
            /// <summary>이 기기에서 마지막으로 저장한 시각(UTC).</summary>
            public string savedAtUtc;
            /// <summary>마지막으로 기기와 클라우드가 같았던 내용의 지문. 비어 있으면 맞춘 적 없음.</summary>
            public string syncedFingerprint;
            /// <summary>그때의 클라우드 계정 ID.</summary>
            public string syncedAccount;
        }
        #endregion // 중첩 타입

        #region 필드
        /// <summary>
        /// 기본 저장 슬롯 이름.
        /// </summary>
        public const string DefaultSlotName = SWSaveSlot.Default;
        private const string SaveDirectoryName = "SaveData";
        private const string SaveExtension = ".json";
        private const string BackupExtension = ".bak";
        private const string TempExtension = ".tmp";
        private const string CloudFallbackKeyPrefix = "SwUtilsCloud_Local";
        private const string SyncExtension = ".sync";

        /// <summary>
        /// 저장 파일과 클라우드 백업을 AES로 암호화할지 여부입니다. 기본값은 true입니다.
        /// 끄더라도 암호화된 파일은 그대로 읽고, 켜더라도 이전 버전의 평문 파일은 그대로 읽습니다.
        /// </summary>
        public static bool EncryptFiles = true;

        /// <summary>
        /// LoadAll(cloudFirst)에서 기기와 클라우드가 둘 다 바뀌어 어느 쪽이 맞는지 알 수 없을 때 부릅니다.
        /// 게임은 충돌의 두 저장을 보여 주고 UseCloud 또는 KeepLocal을 부릅니다. 비워 두면 아무것도 덮어쓰지 않고 기기 저장을 유지합니다.
        /// </summary>
        public static Action<SWSaveConflict> ConflictHandler;
        private static string currentSlot = DefaultSlotName;
        private static object currentData;
        private static Type currentDataType;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// slot 인자를 생략했을 때 사용하는 현재 슬롯 이름.
        /// </summary>
        public static string CurrentSlot => currentSlot;

        /// <summary>
        /// SetData로 런타임 저장 데이터가 등록되어 있는지 여부.
        /// </summary>
        public static bool HasData => currentData != null;

        /// <summary>
        /// 현재 등록된 런타임 저장 데이터 타입.
        /// </summary>
        public static Type CurrentDataType => currentDataType;

        /// <summary>
        /// 기본 슬롯 이름. SWCloud의 기본 저장 이름과 동일하다.
        /// </summary>
        public static string DefaultSlot => DefaultSlotName;

        /// <summary>
        /// 저장 파일이 생성되는 폴더 경로.
        /// </summary>
        public static string SaveDirectoryPath => Path.Combine(Application.persistentDataPath, SaveDirectoryName);
        #endregion // 프로퍼티

        #region Slot
        /// <summary>
        /// 현재 기본 저장 슬롯을 변경합니다. PlayerPrefs 슬롯도 같은 이름으로 맞춥니다.
        /// </summary>
        /// <param name="slot">변경할 저장 슬롯 이름</param>
        public static void SetSlot(string slot)
        {
            currentSlot = NormalizeSlotName(slot);
            SWPlayerPrefs.SetSlot(currentSlot);
        }
        #endregion // Slot

        #region Data
        /// <summary>
        /// 런타임 저장 데이터를 등록합니다. 이후 SaveAll/LoadAll은 이 데이터 타입을 기준으로 동작합니다.
        /// </summary>
        /// <typeparam name="T">저장 데이터 타입</typeparam>
        /// <param name="data">등록할 저장 데이터 인스턴스</param>
        public static void SetData<T>(T data) where T : class
        {
            if (data == null)
            {
                SWLog.LogWarning("[SWSaveDataManager] SetData skipped. Data is null.");
                return;
            }

            currentData = data;
            currentDataType = data.GetType();
        }

        /// <summary>
        /// 등록된 런타임 저장 데이터를 반환합니다.
        /// </summary>
        /// <typeparam name="T">가져올 저장 데이터 타입</typeparam>
        /// <returns>등록된 저장 데이터 인스턴스. 타입이 다르거나 없으면 null</returns>
        public static T GetData<T>() where T : class
        {
            return TryGetData(out T data) ? data : null;
        }

        /// <summary>
        /// 등록된 런타임 저장 데이터를 가져옵니다.
        /// </summary>
        /// <typeparam name="T">가져올 저장 데이터 타입</typeparam>
        /// <param name="data">가져온 저장 데이터 인스턴스</param>
        /// <returns>저장 데이터를 가져왔으면 true</returns>
        public static bool TryGetData<T>(out T data) where T : class
        {
            data = currentData as T;
            return data != null;
        }

        /// <summary>
        /// 메모리에 등록된 런타임 저장 데이터를 제거합니다.
        /// </summary>
        public static void ClearData()
        {
            currentData = null;
            currentDataType = null;
        }
        #endregion // Data

        #region Save
        /// <summary>
        /// JSON 문자열을 선택된 슬롯의 로컬 저장 파일에 저장합니다.
        /// </summary>
        /// <param name="json">저장할 JSON 문자열</param>
        /// <param name="slot">저장할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        /// <param name="createBackup">기존 로컬 저장 파일을 백업 파일로 남길지 여부</param>
        /// <returns>저장에 성공했으면 true</returns>
        private static bool SaveJson(string json, string slot = null, bool createBackup = true)
        {
            if (string.IsNullOrEmpty(json))
            {
                SWLog.LogError("[SWSaveDataManager] SaveJson failed. JSON is empty.");
                return false;
            }

            string normalizedSlot = ResolveSlotName(slot);
            string path = GetSavePath(normalizedSlot);
            string backupPath = path + BackupExtension;

            string stored = EncryptFiles ? SWSaveCrypto.Encrypt(json) : json;
            if (string.IsNullOrEmpty(stored))
            {
                SWLog.LogError($"[SWSaveDataManager] SaveJson failed. Encryption failed. Slot: {normalizedSlot}");
                return false;
            }

            try
            {
                EnsureSaveDirectory();
                SWAtomicFile.WriteAllText(path, stored, createBackup ? backupPath : null);
                SWLog.Log($"[SWSaveDataManager] Save complete. Slot: {normalizedSlot}");
                return true;
            }
            catch (Exception exception)
            {
                SWLog.LogError($"[SWSaveDataManager] SaveJson failed. Slot: {normalizedSlot}, Error: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 등록된 런타임 저장 데이터를 로컬 저장 파일에 저장합니다.
        /// </summary>
        /// <param name="slot">저장할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        /// <param name="prettyPrint">저장 JSON을 보기 좋게 정렬할지 여부</param>
        /// <param name="createBackup">기존 로컬 저장 파일을 백업 파일로 남길지 여부</param>
        /// <returns>저장에 성공했으면 true</returns>
        private static bool SaveRegisteredData(string slot = null, bool prettyPrint = false, bool createBackup = true)
        {
            if (currentData == null)
            {
                SWLog.LogError("[SWSaveDataManager] Save failed. Runtime data is not set. Call SetData first.");
                return false;
            }

            string normalizedSlot = ResolveSlotName(slot);
            SetSharedSlot(normalizedSlot);

            try
            {
                string json = JsonUtility.ToJson(currentData, prettyPrint);
                bool saved = SaveJson(json, normalizedSlot, createBackup);
                if (saved)
                {
                    SWPlayerPrefs.Save();
                    SyncState state = ReadSyncState(normalizedSlot);
                    state.revision++;
                    state.savedAtUtc = DateTime.UtcNow.ToString("o");
                    WriteSyncState(normalizedSlot, state);
                }
                return saved;
            }
            catch (Exception exception)
            {
                SWLog.LogError($"[SWSaveDataManager] Save failed. Slot: {normalizedSlot}, Error: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 등록된 저장 데이터와 PlayerPrefs를 저장하고, 옵션에 따라 둘을 묶어 클라우드에 백업합니다.
        /// </summary>
        /// <param name="onCloudComplete">클라우드 백업 완료 시 호출할 콜백</param>
        /// <param name="slot">저장할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        /// <param name="prettyPrint">저장 JSON을 보기 좋게 정렬할지 여부</param>
        /// <param name="createBackup">기존 로컬 저장 파일을 백업 파일로 남길지 여부</param>
        /// <param name="backupToCloud">로컬 저장 후 클라우드 백업까지 수행할지 여부</param>
        /// <returns>로컬 저장에 성공했으면 true</returns>
        public static bool SaveAll(Action<bool> onCloudComplete = null, string slot = null,
            bool prettyPrint = false, bool createBackup = true, bool backupToCloud = true)
        {
            if (currentData == null)
            {
                SWLog.LogError("[SWSaveDataManager] SaveAll failed. Runtime data is not set. Call SetData first.");
                onCloudComplete?.Invoke(false);
                return false;
            }

            string normalizedSlot = ResolveSlotName(slot);
            SetSharedSlot(normalizedSlot);

            bool saved = SaveRegisteredData(normalizedSlot, prettyPrint, createBackup);
            if (!saved)
            {
                onCloudComplete?.Invoke(false);
                return false;
            }

            if (backupToCloud)
                BackupToCloud(onCloudComplete, normalizedSlot);
            else
                onCloudComplete?.Invoke(true);

            return true;
        }

        /// <summary>
        /// 등록된 저장 데이터와 PlayerPrefs를 저장하고 클라우드 백업까지 비동기로 수행합니다.
        /// </summary>
        /// <param name="slot">저장할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        /// <param name="prettyPrint">저장 JSON을 보기 좋게 정렬할지 여부</param>
        /// <param name="createBackup">기존 로컬 저장 파일을 백업 파일로 남길지 여부</param>
        /// <param name="backupToCloud">로컬 저장 후 클라우드 백업까지 수행할지 여부</param>
        /// <returns>저장과 선택된 클라우드 백업이 완료되면 결과를 반환하는 작업</returns>
        public static Task<bool> SaveAllAsync(string slot = null, bool prettyPrint = false,
            bool createBackup = true, bool backupToCloud = true)
        {
            if (!backupToCloud)
                return Task.FromResult(SaveAll(onCloudComplete: null, slot: slot,
                    prettyPrint: prettyPrint, createBackup: createBackup, backupToCloud: false));

            var taskCompletionSource = new TaskCompletionSource<bool>();
            bool localSaved = SaveAll(success => taskCompletionSource.SetResult(success),
                slot, prettyPrint, createBackup, true);

            if (!localSaved)
                taskCompletionSource.TrySetResult(false);

            return taskCompletionSource.Task;
        }
        #endregion // Save

        #region Load
        /// <summary>
        /// 선택된 슬롯의 로컬 저장 파일에서 JSON 문자열을 읽습니다.
        /// </summary>
        /// <param name="json">읽어온 JSON 문자열</param>
        /// <param name="slot">읽을 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        /// <returns>JSON 문자열을 읽었으면 true</returns>
        private static bool TryLoadJson(out string json, string slot = null)
        {
            json = string.Empty;
            string normalizedSlot = ResolveSlotName(slot);
            string path = GetSavePath(normalizedSlot);

            if (!File.Exists(path))
                return false;

            try
            {
                if (!SWSaveCrypto.TryDecrypt(File.ReadAllText(path), out json))
                {
                    json = string.Empty;
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                SWLog.LogError($"[SWSaveDataManager] TryLoadJson failed. Slot: {normalizedSlot}, Error: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 클라우드에서 저장 데이터와 PlayerPrefs를 먼저 복원한 뒤 로컬 저장 데이터를 메모리로 로드합니다.
        /// 저장 데이터 타입을 알 수 있도록 LoadAll 호출 전에 SetData&lt;T&gt;(data)로 저장 데이터를 먼저 등록해야 합니다.
        /// </summary>
        /// <param name="onComplete">로드 완료 시 호출할 콜백</param>
        /// <param name="slot">로드할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        /// <param name="cloudFirst">클라우드 데이터를 먼저 복원한 뒤 로컬 데이터를 로드할지 여부</param>
        /// <param name="createBackup">클라우드 복원 시 기존 로컬 저장 파일을 백업 파일로 남길지 여부</param>
        public static void LoadAll(Action<bool> onComplete = null, string slot = null,
            bool cloudFirst = true, bool createBackup = true)
        {
            string normalizedSlot = ResolveSlotName(slot);
            SetSharedSlot(normalizedSlot);

            if (!cloudFirst)
            {
                bool localSuccess = LoadRegisteredData(normalizedSlot);
                onComplete?.Invoke(localSuccess);
                return;
            }

            SyncWithCloud(normalizedSlot, createBackup, onComplete);
        }

        /// <summary>
        /// 저장 데이터와 PlayerPrefs를 클라우드에서 복원한 뒤 로컬 데이터를 메모리로 로드하는 과정을 비동기로 수행합니다.
        /// 저장 데이터 타입을 알 수 있도록 LoadAllAsync 호출 전에 SetData&lt;T&gt;(data)로 저장 데이터를 먼저 등록해야 합니다.
        /// </summary>
        /// <param name="slot">로드할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        /// <param name="cloudFirst">클라우드 데이터를 먼저 복원한 뒤 로컬 데이터를 로드할지 여부</param>
        /// <param name="createBackup">클라우드 복원 시 기존 로컬 저장 파일을 백업 파일로 남길지 여부</param>
        /// <returns>로드가 완료되면 결과를 반환하는 작업</returns>
        public static Task<bool> LoadAllAsync(string slot = null, bool cloudFirst = true, bool createBackup = true)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            LoadAll(success => taskCompletionSource.SetResult(success), slot, cloudFirst, createBackup);
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 등록된 저장 데이터 타입으로 로컬 저장 파일을 역직렬화해 메모리에 로드합니다.
        /// </summary>
        /// <param name="slot">로드할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        /// <returns>로드에 성공했으면 true</returns>
        private static bool LoadRegisteredData(string slot = null)
        {
            if (currentDataType == null)
            {
                SWLog.LogError("[SWSaveDataManager] Load failed. Runtime data type is not set. Call SetData first.");
                return false;
            }

            string normalizedSlot = ResolveSlotName(slot);
            SetSharedSlot(normalizedSlot);
            string path = GetSavePath(normalizedSlot);

            foreach (string candidatePath in new[] { path, path + BackupExtension })
            {
                if (!File.Exists(candidatePath)) continue;
                try
                {
                    string stored = File.ReadAllText(candidatePath);
                    if (!SWSaveCrypto.TryDecrypt(stored, out string json)) continue;
                    object loaded = JsonUtility.FromJson(json, currentDataType);
                    if (loaded == null) continue;
                    if (candidatePath != path)
                        SWAtomicFile.WriteAllText(path, stored);
                    currentData = loaded;
                    currentDataType = loaded.GetType();
                    return true;
                }
                catch (Exception exception)
                {
                    SWLog.LogWarning($"[SWSaveDataManager] 저장 파일 읽기 실패: {candidatePath}, {exception.Message}");
                }
            }
            return false;
        }
        #endregion // Load

        #region Management
        /// <summary>
        /// 선택된 슬롯의 로컬 저장 파일이 있는지 확인합니다.
        /// </summary>
        /// <param name="slot">확인할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        /// <returns>저장 파일이 있으면 true</returns>
        public static bool HasSave(string slot = null)
        {
            return File.Exists(GetSavePath(slot));
        }

        /// <summary>
        /// 선택된 슬롯의 로컬 저장 파일과 백업 파일을 삭제합니다.
        /// </summary>
        /// <param name="slot">삭제할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        /// <returns>로컬 저장 파일을 삭제했으면 true</returns>
        public static bool Delete(string slot = null)
        {
            string normalizedSlot = ResolveSlotName(slot);
            string path = GetSavePath(normalizedSlot);
            string backupPath = path + BackupExtension;
            bool deleted = false;

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    deleted = true;
                }

                if (File.Exists(backupPath))
                    File.Delete(backupPath);
                string syncPath = GetSyncPath(normalizedSlot);
                if (File.Exists(syncPath))
                    File.Delete(syncPath);
                SWLog.Log($"[SWSaveDataManager] Delete complete. Slot: {normalizedSlot}");
                return deleted;
            }
            catch (Exception exception)
            {
                SWLog.LogError($"[SWSaveDataManager] Delete failed. Slot: {normalizedSlot}, Error: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 한 슬롯의 로컬 저장 파일을 다른 슬롯으로 복사합니다.
        /// </summary>
        /// <param name="fromSlot">복사할 원본 슬롯 이름</param>
        /// <param name="toSlot">복사 대상 슬롯 이름</param>
        /// <param name="overwrite">대상 슬롯 파일이 있을 때 덮어쓸지 여부</param>
        /// <returns>복사에 성공했으면 true</returns>
        public static bool CopySlot(string fromSlot, string toSlot, bool overwrite = true)
        {
            string normalizedFromSlot = ResolveSlotName(fromSlot);
            string normalizedToSlot = ResolveSlotName(toSlot);
            string fromPath = GetSavePath(fromSlot);
            string toPath = GetSavePath(toSlot);

            if (!File.Exists(fromPath))
            {
                SWLog.LogWarning($"[SWSaveDataManager] CopySlot failed. Source does not exist. Slot: {normalizedFromSlot}");
                return false;
            }

            if (!overwrite && File.Exists(toPath))
            {
                SWLog.LogWarning($"[SWSaveDataManager] CopySlot failed. Target already exists. Slot: {normalizedToSlot}");
                return false;
            }

            try
            {
                EnsureSaveDirectory();
                File.Copy(fromPath, toPath, overwrite);
                return true;
            }
            catch (Exception exception)
            {
                SWLog.LogError($"[SWSaveDataManager] CopySlot failed. Error: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 저장 폴더 안의 모든 슬롯 저장 파일 정보를 반환합니다.
        /// </summary>
        /// <returns>저장 파일 정보 목록</returns>
        public static IReadOnlyList<SaveFileInfo> ListSaves()
        {
            var saves = new List<SaveFileInfo>();
            if (!Directory.Exists(SaveDirectoryPath))
                return saves;

            string[] files = Directory.GetFiles(SaveDirectoryPath, "*" + SaveExtension);
            for (int index = 0; index < files.Length; index++)
            {
                FileInfo fileInfo = new FileInfo(files[index]);
                string slot = Path.GetFileNameWithoutExtension(fileInfo.Name);
                saves.Add(new SaveFileInfo(
                    slot,
                    fileInfo.Name,
                    fileInfo.FullName,
                    fileInfo.Exists,
                    fileInfo.Exists ? fileInfo.Length : 0,
                    fileInfo.Exists ? fileInfo.LastWriteTimeUtc : DateTime.MinValue));
            }

            return saves;
        }

        /// <summary>
        /// 선택된 슬롯 저장 파일의 정보를 반환합니다.
        /// </summary>
        /// <param name="slot">정보를 확인할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        /// <returns>저장 파일 정보</returns>
        public static SaveFileInfo GetSaveInfo(string slot = null)
        {
            string normalizedSlot = ResolveSlotName(slot);
            string path = GetSavePath(normalizedSlot);
            FileInfo fileInfo = new FileInfo(path);

            return new SaveFileInfo(
                normalizedSlot,
                fileInfo.Name,
                fileInfo.FullName,
                fileInfo.Exists,
                fileInfo.Exists ? fileInfo.Length : 0,
                fileInfo.Exists ? fileInfo.LastWriteTimeUtc : DateTime.MinValue);
        }
        #endregion // Management

        #region Cloud
        /// <summary>
        /// 선택된 로컬 슬롯의 저장 데이터와 PlayerPrefs를 SWCloud에 백업합니다.
        /// </summary>
        /// <param name="onComplete">클라우드 백업 완료 시 호출할 콜백</param>
        /// <param name="slot">백업할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        public static void BackupToCloud(Action<bool> onComplete = null, string slot = null)
        {
            string normalizedSlot = ResolveSlotName(slot);
            SetSharedSlot(normalizedSlot);

            if (!TryLoadJson(out string saveDataJson, normalizedSlot))
            {
                SWLog.LogWarning($"[SWSaveDataManager] BackupToCloud failed. Local save does not exist. Slot: {normalizedSlot}");
                onComplete?.Invoke(false);
                return;
            }

            SWPlayerPrefs.Save();
            SyncState state = ReadSyncState(normalizedSlot);
            var backupData = new CloudBackupData
            {
                slot = normalizedSlot,
                saveDataJson = saveDataJson,
                playerPrefsJson = ExportPrefs(normalizedSlot),
                savedAtUtc = string.IsNullOrEmpty(state.savedAtUtc) ? DateTime.UtcNow.ToString("o") : state.savedAtUtc,
                revision = state.revision
            };
            string fingerprint = CloudFingerprint(backupData);
            string cloudJson = JsonUtility.ToJson(backupData);
            string stored = EncryptFiles ? SWSaveCrypto.Encrypt(cloudJson) : cloudJson;
            SWCloud.Save(stored, success =>
            {
                // 실제 클라우드에 올렸을 때만 맞춘 기록을 남깁니다. 로컬 대체 저장은 다른 기기와 공유되지 않으므로 기준이 될 수 없습니다.
                if (success && SWCloud.IsAvailable)
                {
                    MarkSynced(normalizedSlot, fingerprint);
                }

                onComplete?.Invoke(success);
            }, normalizedSlot);
        }

        /// <summary>
        /// SWCloud에서 저장 데이터를 내려받아 선택된 로컬 슬롯과 PlayerPrefs에 복원합니다.
        /// </summary>
        /// <param name="onComplete">클라우드 복원 완료 시 호출할 콜백</param>
        /// <param name="slot">복원할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        /// <param name="createBackup">기존 로컬 저장 파일을 백업 파일로 남길지 여부</param>
        public static void RestoreFromCloud(Action<bool> onComplete = null, string slot = null, bool createBackup = true)
        {
            string normalizedSlot = ResolveSlotName(slot);
            SWCloud.Load((success, json) =>
            {
                if (!success || string.IsNullOrEmpty(json))
                {
                    onComplete?.Invoke(false);
                    return;
                }

                if (!TryReadCloud(json, out CloudBackupData cloud))
                {
                    onComplete?.Invoke(false);
                    return;
                }

                bool restored = RestoreCloud(cloud, normalizedSlot, createBackup);
                if (restored && SWCloud.IsAvailable)
                {
                    MarkSynced(normalizedSlot, CloudFingerprint(cloud));
                }

                onComplete?.Invoke(restored);
            }, normalizedSlot);
        }

        /// <summary>
        /// 선택된 슬롯의 클라우드 저장 데이터를 삭제합니다.
        /// </summary>
        /// <param name="onComplete">클라우드 삭제 완료 시 호출할 콜백</param>
        /// <param name="slot">삭제할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        public static void DeleteCloud(Action<bool> onComplete = null, string slot = null)
        {
            SWCloud.Delete(onComplete, ResolveSlotName(slot));
        }

        /// <summary>
        /// 선택된 로컬 슬롯의 저장 데이터와 PlayerPrefs를 SWCloud에 비동기로 백업합니다.
        /// </summary>
        /// <param name="slot">백업할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        /// <returns>클라우드 백업이 완료되면 결과를 반환하는 작업</returns>
        public static Task<bool> BackupToCloudAsync(string slot = null)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            BackupToCloud(success => taskCompletionSource.SetResult(success), slot);
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// SWCloud에서 저장 데이터를 내려받아 선택된 로컬 슬롯과 PlayerPrefs에 비동기로 복원합니다.
        /// </summary>
        /// <param name="slot">복원할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        /// <param name="createBackup">기존 로컬 저장 파일을 백업 파일로 남길지 여부</param>
        /// <returns>클라우드 복원이 완료되면 결과를 반환하는 작업</returns>
        public static Task<bool> RestoreFromCloudAsync(string slot = null, bool createBackup = true)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            RestoreFromCloud(success => taskCompletionSource.SetResult(success), slot, createBackup);
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 선택된 슬롯의 클라우드 저장 데이터를 비동기로 삭제합니다.
        /// </summary>
        /// <param name="slot">삭제할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        /// <returns>클라우드 삭제가 완료되면 결과를 반환하는 작업</returns>
        public static Task<bool> DeleteCloudAsync(string slot = null)
        {
            return SWCloud.DeleteAsync(ResolveSlotName(slot));
        }

        /// <summary>
        /// 클라우드에서 읽은 내용을 평문으로 풀어 백업 데이터로 읽습니다. 예전 형식(저장 데이터 JSON만 올린 경우)도 받습니다.
        /// </summary>
        /// <param name="stored">클라우드에서 읽은 내용</param>
        /// <param name="cloud">읽은 백업 데이터</param>
        /// <returns>읽었으면 true</returns>
        private static bool TryReadCloud(string stored, out CloudBackupData cloud)
        {
            cloud = null;
            if (string.IsNullOrEmpty(stored) || !SWSaveCrypto.TryDecrypt(stored, out string json) || string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                CloudBackupData parsed = JsonUtility.FromJson<CloudBackupData>(json);
                if (parsed != null && (!string.IsNullOrEmpty(parsed.saveDataJson) || !string.IsNullOrEmpty(parsed.playerPrefsJson)))
                {
                    cloud = parsed;
                    return true;
                }
            }
            catch (Exception exception)
            {
                SWLog.LogWarning($"[SWSaveDataManager] 클라우드 데이터 읽기 실패: {exception.Message}");
            }

            cloud = new CloudBackupData { saveDataJson = json };
            return true;
        }

        /// <summary>
        /// 클라우드 백업을 요청 당시 슬롯에 적용합니다. 파일 저장에 실패하면 설정값을 이전 내용으로 돌립니다.
        /// </summary>
        /// <param name="backupData">적용할 백업 데이터</param>
        /// <param name="slot">적용할 슬롯</param>
        /// <param name="createBackup">기존 로컬 저장 파일을 백업 파일로 남길지 여부</param>
        /// <returns>적용했으면 true</returns>
        private static bool RestoreCloud(CloudBackupData backupData, string slot, bool createBackup)
        {
            try
            {
                bool hasSettings = !string.IsNullOrEmpty(backupData.playerPrefsJson);
                if (hasSettings && !SWPlayerPrefs.CanImportJson(backupData.playerPrefsJson)) return false;
                string previousSettings = hasSettings ? SWPlayerPrefs.ExportSlotToJson(slot) : null;
                if (hasSettings && !SWPlayerPrefs.ImportFromJson(backupData.playerPrefsJson, slot)) return false;
                bool saveDataRestored = string.IsNullOrEmpty(backupData.saveDataJson)
                    || SaveJson(backupData.saveDataJson, slot, createBackup);
                if (!saveDataRestored && hasSettings)
                    SWPlayerPrefs.ImportFromJson(previousSettings, slot);
                return saveDataRestored;
            }
            catch (Exception exception)
            {
                SWLog.LogWarning($"[SWSaveDataManager] 클라우드 복원 실패: {exception.Message}");
                return false;
            }
        }
        #endregion // Cloud

        #region Sync
        /// <summary>
        /// 클라우드와 기기 저장을 비교해 맞는 쪽을 쓰고 메모리로 읽습니다.
        /// 클라우드를 쓸 수 없거나 비어 있으면 기기 저장, 기기가 비어 있으면 클라우드,
        /// 마지막으로 맞춘 뒤 한쪽만 바뀌었으면 바뀐 쪽, 둘 다 바뀌었거나 계정이 달라졌으면 ConflictHandler에 묻습니다.
        /// </summary>
        /// <param name="slot">읽을 슬롯</param>
        /// <param name="createBackup">클라우드를 적용할 때 기존 기기 저장을 백업 파일로 남길지 여부</param>
        /// <param name="onComplete">메모리로 읽기를 마치면 부를 콜백</param>
        private static void SyncWithCloud(string slot, bool createBackup, Action<bool> onComplete)
        {
            if (!SWCloud.IsAvailable)
            {
                onComplete?.Invoke(LoadRegisteredData(slot));
                return;
            }

            SWCloud.LoadRemote((success, stored) =>
            {
                SetSharedSlot(slot);
                if (!success || !TryReadCloud(stored, out CloudBackupData cloud))
                {
                    SWLog.Log($"[SWSaveDataManager] 클라우드 저장이 없거나 읽지 못해 기기 저장을 씁니다. Slot: {slot}");
                    onComplete?.Invoke(LoadRegisteredData(slot));
                    return;
                }

                string cloudFingerprint = CloudFingerprint(cloud);
                if (!TryLoadJson(out string localJson, slot))
                {
                    SWLog.Log($"[SWSaveDataManager] 기기 저장이 없어 클라우드 저장을 씁니다. Slot: {slot}");
                    ApplyCloud(cloud, cloudFingerprint, slot, createBackup, onComplete);
                    return;
                }

                string localFingerprint = SWSaveCrypto.Fingerprint(localJson, ExportPrefs(slot));
                if (localFingerprint == cloudFingerprint)
                {
                    MarkSynced(slot, cloudFingerprint);
                    onComplete?.Invoke(LoadRegisteredData(slot));
                    return;
                }

                SyncState state = ReadSyncState(slot);
                string account = SWCloud.AccountId;
                bool accountChanged = !string.IsNullOrEmpty(state.syncedAccount) && !string.IsNullOrEmpty(account) && account != state.syncedAccount;
                bool hasRecord = !string.IsNullOrEmpty(state.syncedFingerprint);
                if (hasRecord && !accountChanged)
                {
                    if (cloudFingerprint == state.syncedFingerprint)
                    {
                        SWLog.Log($"[SWSaveDataManager] 기기에서만 바뀌어 기기 저장을 쓰고 클라우드에 올립니다. Slot: {slot}");
                        bool loaded = LoadRegisteredData(slot);
                        BackupToCloud(null, slot);
                        onComplete?.Invoke(loaded);
                        return;
                    }

                    if (localFingerprint == state.syncedFingerprint)
                    {
                        SWLog.Log($"[SWSaveDataManager] 클라우드에서만 바뀌어 클라우드 저장을 씁니다. Slot: {slot}");
                        ApplyCloud(cloud, cloudFingerprint, slot, createBackup, onComplete);
                        return;
                    }
                }

                ESaveConflictReason reason = accountChanged ? ESaveConflictReason.AccountChanged
                    : hasRecord ? ESaveConflictReason.BothChanged : ESaveConflictReason.NoSyncRecord;
                var conflict = new SWSaveConflict(slot, reason,
                    new SWSaveSnapshot(localJson, state.savedAtUtc, state.revision),
                    new SWSaveSnapshot(cloud.saveDataJson, cloud.savedAtUtc, cloud.revision),
                    useCloud =>
                    {
                        if (useCloud)
                        {
                            ApplyCloud(cloud, cloudFingerprint, slot, createBackup, onComplete);
                            return;
                        }

                        SetSharedSlot(slot);
                        bool loaded = LoadRegisteredData(slot);
                        BackupToCloud(null, slot);
                        onComplete?.Invoke(loaded);
                    });
                if (ConflictHandler == null)
                {
                    SWLog.LogWarning($"[SWSaveDataManager] 기기와 클라우드 저장이 충돌했습니다({reason}). ConflictHandler가 없어 덮어쓰지 않고 기기 저장을 씁니다. Slot: {slot}");
                    onComplete?.Invoke(LoadRegisteredData(slot));
                    return;
                }

                SWLog.Log($"[SWSaveDataManager] 기기와 클라우드 저장이 충돌했습니다({reason}). ConflictHandler에 묻습니다. Slot: {slot}");
                ConflictHandler(conflict);
            }, slot);
        }

        /// <summary>클라우드 저장을 기기에 적용하고 맞춘 기록을 남긴 뒤 메모리로 읽습니다. 적용하지 못하면 기기 저장을 읽습니다.</summary>
        private static void ApplyCloud(CloudBackupData cloud, string cloudFingerprint, string slot, bool createBackup, Action<bool> onComplete)
        {
            SetSharedSlot(slot);
            if (RestoreCloud(cloud, slot, createBackup))
            {
                SyncState state = ReadSyncState(slot);
                state.revision = Math.Max(state.revision, cloud.revision);
                state.savedAtUtc = cloud.savedAtUtc;
                WriteSyncState(slot, state);
                MarkSynced(slot, cloudFingerprint);
            }
            else
            {
                SWLog.LogWarning($"[SWSaveDataManager] 클라우드 저장을 적용하지 못해 기기 저장을 씁니다. Slot: {slot}");
            }

            onComplete?.Invoke(LoadRegisteredData(slot));
        }

        /// <summary>기기와 클라우드가 이 내용으로 같아졌음을 지금 계정과 함께 기록합니다.</summary>
        private static void MarkSynced(string slot, string fingerprint)
        {
            SyncState state = ReadSyncState(slot);
            state.syncedFingerprint = fingerprint;
            state.syncedAccount = SWCloud.AccountId;
            WriteSyncState(slot, state);
        }

        /// <summary>클라우드 백업 내용의 지문입니다. 기기 쪽은 같은 두 값(저장 데이터 JSON, PlayerPrefs JSON)으로 만듭니다.</summary>
        private static string CloudFingerprint(CloudBackupData cloud)
        {
            return SWSaveCrypto.Fingerprint(cloud.saveDataJson ?? string.Empty, cloud.playerPrefsJson ?? string.Empty);
        }

        /// <summary>클라우드에 함께 올리는 PlayerPrefs JSON입니다(로컬 대체 저장 키 제외, 키 순서 정렬).</summary>
        private static string ExportPrefs(string slot)
        {
            return SWPlayerPrefs.ExportSlotToJson(slot, IsCloudBackupKey);
        }

        /// <summary>동기화 기록 파일 경로입니다.</summary>
        private static string GetSyncPath(string slot)
        {
            return GetSavePath(slot) + SyncExtension;
        }

        /// <summary>동기화 기록을 읽습니다. 없거나 읽지 못하면 빈 기록입니다.</summary>
        private static SyncState ReadSyncState(string slot)
        {
            string path = GetSyncPath(slot);
            try
            {
                if (File.Exists(path))
                {
                    SyncState state = JsonUtility.FromJson<SyncState>(File.ReadAllText(path));
                    if (state != null)
                        return state;
                }
            }
            catch (Exception exception)
            {
                SWLog.LogWarning($"[SWSaveDataManager] 동기화 기록 읽기 실패: {exception.Message}");
            }

            return new SyncState();
        }

        /// <summary>동기화 기록을 씁니다.</summary>
        private static void WriteSyncState(string slot, SyncState state)
        {
            try
            {
                EnsureSaveDirectory();
                SWAtomicFile.WriteAllText(GetSyncPath(slot), JsonUtility.ToJson(state));
            }
            catch (Exception exception)
            {
                SWLog.LogWarning($"[SWSaveDataManager] 동기화 기록 쓰기 실패: {exception.Message}");
            }
        }
        #endregion // Sync

        #region Path
        /// <summary>
        /// 선택된 슬롯의 로컬 저장 파일 전체 경로를 반환합니다.
        /// </summary>
        /// <param name="slot">경로를 확인할 슬롯 이름. null이면 현재 슬롯을 사용합니다.</param>
        /// <returns>로컬 저장 파일 전체 경로</returns>
        public static string GetSavePath(string slot = null)
        {
            return Path.Combine(SaveDirectoryPath, ResolveSlotName(slot) + SaveExtension);
        }

        /// <summary>
        /// 로컬 저장 폴더가 없으면 생성합니다.
        /// </summary>
        private static void EnsureSaveDirectory()
        {
            if (!Directory.Exists(SaveDirectoryPath))
                Directory.CreateDirectory(SaveDirectoryPath);
        }

        /// <summary>
        /// 슬롯 이름을 로컬 파일명과 클라우드 저장 이름으로 사용할 수 있게 정리합니다.
        /// </summary>
        /// <param name="slot">정리할 슬롯 이름</param>
        /// <returns>파일명으로 사용할 수 있게 정리된 슬롯 이름</returns>
        public static string NormalizeSlotName(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot))
                return DefaultSlotName;

            string normalized = slot.Trim();
            char[] invalidChars = Path.GetInvalidFileNameChars();

            for (int index = 0; index < invalidChars.Length; index++)
                normalized = normalized.Replace(invalidChars[index], '_');

            return string.IsNullOrWhiteSpace(normalized) ? DefaultSlotName : normalized;
        }

        /// <summary>
        /// 슬롯 이름이 비어 있으면 현재 슬롯을 반환하고, 값이 있으면 정리된 슬롯 이름을 반환합니다.
        /// </summary>
        /// <param name="slot">확인할 슬롯 이름</param>
        /// <returns>사용할 슬롯 이름</returns>
        private static string ResolveSlotName(string slot)
        {
            return string.IsNullOrWhiteSpace(slot) ? currentSlot : NormalizeSlotName(slot);
        }

        /// <summary>
        /// 저장 매니저와 SWPlayerPrefs가 같은 슬롯을 사용하도록 설정합니다.
        /// </summary>
        /// <param name="slot">설정할 슬롯 이름</param>
        private static void SetSharedSlot(string slot)
        {
            string normalizedSlot = ResolveSlotName(slot);
            SetSlot(normalizedSlot);
        }

        /// <summary>
        /// 클라우드 백업에 포함할 PlayerPrefs 키인지 확인합니다.
        /// </summary>
        /// <param name="key">확인할 PlayerPrefs 키</param>
        /// <returns>클라우드 백업에 포함할 키이면 true</returns>
        private static bool IsCloudBackupKey(string key)
        {
            return !key.StartsWith(CloudFallbackKeyPrefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// 파일이 있으면 삭제합니다. 삭제에 실패하면 경고를 기록하고 예외를 전파하지 않습니다.
        /// </summary>
        /// <param name="path">삭제할 파일 경로</param>
        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception exception)
            {
                SWLog.LogWarning($"[SWSaveDataManager] Failed to delete temp file. Error: {exception.Message}");
            }
        }
        #endregion // Path
    }
}
