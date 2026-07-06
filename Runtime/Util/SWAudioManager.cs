using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SW.Attribute;

using SW.Data;

namespace SW.Util
{
    /// <summary>
    /// BGM/SFX 재생, 볼륨 조절, 페이드, AudioSource 재사용을 처리하는 오디오 매니저.
    /// SWAudioManager.Instance로 전역 접근하거나 씬에 직접 배치해서 사용합니다.
    /// </summary>
    /// <remarks>
    /// 확장 사항 (모두 저비용: 최대 maxSfxSources개 선형 스캔 + 딕셔너리 1회 조회 수준):
    /// - 동일 클립 최소 재생 간격(sameClipMinInterval): 같은 SFX가 한 프레임에 수십 번 겹쳐 터지는 것을 방지.
    /// - 동일 클립 동시 보이스 제한(maxVoicesPerClip): 같은 클립의 동시 재생 수 상한.
    /// - PlaySfxRandomPitch: 반복 재생 시 단조로움을 줄이는 랜덤 피치 재생.
    /// 두 제한 모두 기본값 0(비활성)이라 기존 동작과 완전히 동일하게 시작합니다.
    /// </remarks>
    public class SWAudioManager : SWSingleton<SWAudioManager>
    {
        #region 상수
        private const string MasterVolumeKey = "SWAudioManager_MasterVolume";
        private const string MusicVolumeKey = "SWAudioManager_MusicVolume";
        private const string SfxVolumeKey = "SWAudioManager_SfxVolume";
        #endregion // 상수

        #region 필드
        [Header("=====> 라이브러리 <=====")]
        [SerializeField] private SWAudioLibrary audioLibrary;

        [Header("=====> 음악 <=====")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private bool playMusicOnStart;
        [SerializeField, SWCondition("playMusicOnStart", true)] private string startMusicKey;

        [Header("=====> 효과음 <=====")]
        [SerializeField] private AudioSource sfxSourcePrefab;
        [SerializeField] private int initialSfxSources = 8;
        [SerializeField] private int maxSfxSources = 32;

        [Header("=====> 효과음 폭주 방지 <=====")]
        [Tooltip("같은 클립의 최소 재생 간격(초)입니다. 0이면 제한하지 않습니다.")]
        [SerializeField, Range(0f, 0.5f)] private float sameClipMinInterval = 0f;
        [Tooltip("같은 클립의 동시 재생 수 상한입니다. 0이면 제한하지 않습니다.")]
        [SerializeField, Range(0, 32)] private int maxVoicesPerClip = 0;

        [Header("=====> 설정 <=====")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1f;

        private readonly List<AudioSource> sfxSources = new();
        /// <summary>클립별 마지막 재생 시각(Time.unscaledTime)입니다. 재생 간격 제한에 사용합니다.</summary>
        private readonly Dictionary<AudioClip, float> lastPlayTimeDictionary = new();
        private Coroutine musicFadeRoutine;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>키 기반 재생에 사용하는 사운드 라이브러리.</summary>
        public SWAudioLibrary AudioLibrary => audioLibrary;
        /// <summary>음악 재생에 사용하는 AudioSource.</summary>
        public AudioSource MusicSource => musicSource;
        /// <summary>전체 볼륨.</summary>
        public float MasterVolume => masterVolume;
        /// <summary>음악 볼륨.</summary>
        public float MusicVolume => musicVolume;
        /// <summary>효과음 볼륨.</summary>
        public float SfxVolume => sfxVolume;
        #endregion // 프로퍼티

        #region 초기화
        /// <inheritdoc/>
        /// <inheritdoc />
        public override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            EnsureMusicSource();
            PrewarmSfxSources(initialSfxSources);
            ApplyVolumes();

            SWLog.Log($"[SWAudioManager] Initialized. SFX Sources: {sfxSources.Count}");
        }

        private void Start()
        {
            if (!playMusicOnStart) return;
            PlayMusic(startMusicKey);
        }

        /// <inheritdoc/>
        /// <inheritdoc />
        public override void OnDestroy()
        {
            StopAllCoroutines();
            sfxSources.Clear();
            lastPlayTimeDictionary.Clear();
            SWLog.Log("[SWAudioManager] Destroyed.");
            base.OnDestroy();
        }
        #endregion // 초기화

        #region 라이브러리
        /// <summary>
        /// 런타임에 사용할 사운드 라이브러리를 지정합니다.
        /// </summary>
        /// <param name="library">BGM/SFX 키가 등록된 라이브러리.</param>
        public void SetAudioLibrary(SWAudioLibrary library)
        {
            audioLibrary = library;
        }
        #endregion // 라이브러리

        #region 음악
        /// <summary>
        /// 라이브러리에 등록된 BGM을 키로 찾아 재생합니다.
        /// </summary>
        /// <param name="key">라이브러리에 등록된 BGM 키.</param>
        /// <param name="loop">반복 재생 여부.</param>
        /// <param name="fadeDuration">페이드 전환 시간.</param>
        public void PlayMusic(string key, bool loop = true, float fadeDuration = 0f)
        {
            if (!TryGetMusicClip(key, out AudioClip clip))
                return;

            PlayMusicClip(clip, loop, fadeDuration);
        }

        /// <summary>
        /// 현재 BGM을 정지합니다.
        /// </summary>
        /// <param name="fadeDuration">페이드아웃 시간.</param>
        public void StopMusic(float fadeDuration = 0f)
        {
            EnsureMusicSource();

            if (musicFadeRoutine != null)
                StopCoroutine(musicFadeRoutine);

            if (fadeDuration > 0f)
            {
                musicFadeRoutine = StartCoroutine(FadeOutMusicRoutine(fadeDuration));
                SWLog.Log("[SWAudioManager] Fade out music.");
                return;
            }

            musicSource.Stop();
            SWLog.Log("[SWAudioManager] Stop music.");
        }

        /// <summary>
        /// 현재 재생 중인 BGM을 일시정지합니다.
        /// </summary>
        public void PauseMusic()
        {
            EnsureMusicSource();
            musicSource.Pause();
            SWLog.Log("[SWAudioManager] Pause music.");
        }

        /// <summary>
        /// 일시정지된 BGM 재생을 재개합니다.
        /// </summary>
        public void ResumeMusic()
        {
            EnsureMusicSource();
            musicSource.UnPause();
            SWLog.Log("[SWAudioManager] Resume music.");
        }
        #endregion // 음악

        #region 효과음
        /// <summary>
        /// 라이브러리에 등록된 2D 효과음을 키로 찾아 재생합니다.
        /// </summary>
        /// <param name="key">라이브러리에 등록된 SFX 키.</param>
        /// <param name="volumeScale">효과음 볼륨 배율.</param>
        /// <param name="pitch">재생 피치.</param>
        /// <returns>재생에 사용된 AudioSource.</returns>
        public AudioSource PlaySfx(string key, float volumeScale = 1f, float pitch = 1f)
        {
            if (!TryGetSfxClip(key, out AudioClip clip))
                return null;

            return PlaySfxClip(clip, volumeScale, pitch);
        }

        /// <summary>
        /// 라이브러리에 등록된 효과음을 랜덤 피치로 재생합니다.
        /// 발소리, 타격음처럼 반복되는 효과음의 단조로움을 줄일 때 사용합니다.
        /// </summary>
        /// <param name="key">라이브러리에 등록된 SFX 키.</param>
        /// <param name="minPitch">최소 피치.</param>
        /// <param name="maxPitch">최대 피치.</param>
        /// <param name="volumeScale">효과음 볼륨 배율.</param>
        /// <returns>재생에 사용된 AudioSource.</returns>
        public AudioSource PlaySfxRandomPitch(string key, float minPitch = 0.95f, float maxPitch = 1.05f, float volumeScale = 1f)
        {
            if (!TryGetSfxClip(key, out AudioClip clip))
                return null;

            float pitch = Random.Range(Mathf.Min(minPitch, maxPitch), Mathf.Max(minPitch, maxPitch));
            return PlaySfxClip(clip, volumeScale, pitch);
        }

        /// <summary>
        /// 라이브러리에 등록된 효과음을 키로 찾아 지정한 월드 위치에서 3D 재생합니다.
        /// </summary>
        /// <param name="key">라이브러리에 등록된 SFX 키.</param>
        /// <param name="position">재생 위치.</param>
        /// <param name="volumeScale">효과음 볼륨 배율.</param>
        /// <param name="pitch">재생 피치.</param>
        /// <returns>재생에 사용된 AudioSource.</returns>
        public AudioSource PlaySfxAtPoint(string key, Vector3 position, float volumeScale = 1f, float pitch = 1f)
        {
            if (!TryGetSfxClip(key, out AudioClip clip))
                return null;

            AudioSource source = PlaySfxClip(clip, volumeScale, pitch);
            if (source == null) return null;

            source.transform.position = position;
            source.spatialBlend = 1f;
            return source;
        }

        /// <summary>
        /// 현재 재생 중인 모든 SFX를 정지합니다.
        /// </summary>
        public void StopAllSfx()
        {
            foreach (AudioSource source in sfxSources)
            {
                if (source != null)
                    source.Stop();
            }

            SWLog.Log("[SWAudioManager] Stop all SFX.");
        }
        #endregion // 효과음

        #region 볼륨
        /// <summary>
        /// 전체 볼륨을 설정합니다.
        /// </summary>
        /// <param name="volume">0~1 사이의 볼륨 값.</param>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            SWLog.Log($"[SWAudioManager] Master volume: {masterVolume}");
        }

        /// <summary>
        /// 음악 볼륨을 설정합니다.
        /// </summary>
        /// <param name="volume">0~1 사이의 볼륨 값.</param>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            SWLog.Log($"[SWAudioManager] Music volume: {musicVolume}");
        }

        /// <summary>
        /// 효과음 볼륨을 설정합니다.
        /// </summary>
        /// <param name="volume">0~1 사이의 볼륨 값.</param>
        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            SWLog.Log($"[SWAudioManager] SFX volume: {sfxVolume}");
        }

        /// <summary>
        /// 현재 볼륨 설정을 SWPlayerPrefs에 저장합니다.
        /// </summary>
        public void SaveVolumes()
        {
            SWPlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            SWPlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            SWPlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            SWPlayerPrefs.Save();
        }

        /// <summary>
        /// SWPlayerPrefs에 저장된 볼륨 설정을 불러와 적용합니다.
        /// </summary>
        public void LoadVolumes()
        {
            masterVolume = Mathf.Clamp01(SWPlayerPrefs.GetFloat(MasterVolumeKey, masterVolume));
            musicVolume = Mathf.Clamp01(SWPlayerPrefs.GetFloat(MusicVolumeKey, musicVolume));
            sfxVolume = Mathf.Clamp01(SWPlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume));
            ApplyVolumes();
        }
        #endregion // 볼륨

        #region 내부
        /// <summary>
        /// BGM 키에 해당하는 AudioClip을 찾습니다. 실패 사유를 경고 로그로 남긴다.
        /// </summary>
        /// <param name="key">찾을 BGM 키.</param>
        /// <param name="clip">찾은 AudioClip.</param>
        /// <returns>유효한 클립을 찾았으면 true.</returns>
        private bool TryGetMusicClip(string key, out AudioClip clip)
        {
            clip = null;

            if (audioLibrary == null)
            {
                SWLog.LogWarning("[SWAudioManager] PlayMusic failed. AudioLibrary is null.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                SWLog.LogWarning("[SWAudioManager] PlayMusic failed. Key is empty.");
                return false;
            }

            if (!audioLibrary.TryGetMusicClip(key, out clip))
            {
                SWLog.LogWarning($"[SWAudioManager] PlayMusic failed. Music key not found: {key}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// SFX 키에 해당하는 AudioClip을 찾습니다. 실패 사유를 경고 로그로 남긴다.
        /// </summary>
        /// <param name="key">찾을 SFX 키.</param>
        /// <param name="clip">찾은 AudioClip.</param>
        /// <returns>유효한 클립을 찾았으면 true.</returns>
        private bool TryGetSfxClip(string key, out AudioClip clip)
        {
            clip = null;

            if (audioLibrary == null)
            {
                SWLog.LogWarning("[SWAudioManager] PlaySfx failed. AudioLibrary is null.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                SWLog.LogWarning("[SWAudioManager] PlaySfx failed. Key is empty.");
                return false;
            }

            if (!audioLibrary.TryGetSfxClip(key, out clip))
            {
                SWLog.LogWarning($"[SWAudioManager] PlaySfx failed. SFX key not found: {key}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// BGM 클립을 재생합니다. 페이드 시간이 지정되면 크로스 페이드를 수행합니다.
        /// </summary>
        /// <param name="clip">재생할 클립.</param>
        /// <param name="loop">반복 재생 여부.</param>
        /// <param name="fadeDuration">페이드 전환 시간.</param>
        private void PlayMusicClip(AudioClip clip, bool loop, float fadeDuration)
        {
            if (clip == null)
            {
                SWLog.LogWarning("[SWAudioManager] PlayMusic failed. Clip is null.");
                return;
            }

            EnsureMusicSource();

            if (musicFadeRoutine != null)
                StopCoroutine(musicFadeRoutine);

            if (fadeDuration > 0f && musicSource.isPlaying)
            {
                musicFadeRoutine = StartCoroutine(FadeToNewMusicRoutine(clip, loop, fadeDuration));
                SWLog.Log($"[SWAudioManager] Fade music to: {clip.name}");
                return;
            }

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = masterVolume * musicVolume;
            musicSource.Play();

            SWLog.Log($"[SWAudioManager] Play music: {clip.name}");
        }

        /// <summary>
        /// SFX 클립을 재생합니다. 폭주 방지 옵션(재생 간격, 동시 보이스 제한)을 통과해야 재생됩니다.
        /// </summary>
        /// <param name="clip">재생할 클립.</param>
        /// <param name="volumeScale">효과음 볼륨 배율.</param>
        /// <param name="pitch">재생 피치.</param>
        /// <returns>재생에 사용된 AudioSource. 제한에 걸리면 null.</returns>
        private AudioSource PlaySfxClip(AudioClip clip, float volumeScale, float pitch)
        {
            if (clip == null)
            {
                SWLog.LogWarning("[SWAudioManager] PlaySfx failed. Clip is null.");
                return null;
            }

            if (!CanPlayClip(clip))
                return null;

            AudioSource source = GetAvailableSfxSource();
            if (source == null)
            {
                SWLog.LogWarning("[SWAudioManager] PlaySfx failed. No available SFX source.");
                return null;
            }

            source.clip = clip;
            source.loop = false;
            source.pitch = pitch;
            source.volume = masterVolume * sfxVolume * Mathf.Clamp01(volumeScale);
            source.spatialBlend = 0f;
            source.transform.SetParent(transform, false);
            source.Play();

            lastPlayTimeDictionary[clip] = Time.unscaledTime;

            SWLog.Log($"[SWAudioManager] Play SFX: {clip.name}");
            return source;
        }

        /// <summary>
        /// 폭주 방지 옵션을 검사합니다. 두 옵션 모두 0이면 항상 true를 반환합니다.
        /// </summary>
        /// <param name="clip">검사할 클립.</param>
        /// <returns>재생 가능하면 true.</returns>
        private bool CanPlayClip(AudioClip clip)
        {
            // 같은 클립 최소 재생 간격 검사
            if (sameClipMinInterval > 0f
                && lastPlayTimeDictionary.TryGetValue(clip, out float lastPlayTime)
                && Time.unscaledTime - lastPlayTime < sameClipMinInterval)
            {
                return false;
            }

            // 같은 클립 동시 보이스 제한 검사
            if (maxVoicesPerClip > 0 && CountPlayingVoices(clip) >= maxVoicesPerClip)
                return false;

            return true;
        }

        /// <summary>
        /// 지정 클립을 현재 재생 중인 SFX 소스 수를 센다.
        /// </summary>
        /// <param name="clip">확인할 클립.</param>
        /// <returns>재생 중인 보이스 수.</returns>
        private int CountPlayingVoices(AudioClip clip)
        {
            int count = 0;
            for (int index = 0; index < sfxSources.Count; index++)
            {
                AudioSource source = sfxSources[index];
                if (source != null && source.isPlaying && source.clip == clip)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// 음악용 AudioSource가 없으면 자동으로 생성합니다.
        /// </summary>
        private void EnsureMusicSource()
        {
            if (musicSource != null) return;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            SWLog.Log("[SWAudioManager] MusicSource auto-created.");
        }

        /// <summary>
        /// 효과음 AudioSource를 미리 생성합니다.
        /// </summary>
        /// <param name="count">생성할 AudioSource 개수.</param>
        private void PrewarmSfxSources(int count)
        {
            int safeCount = Mathf.Max(0, count);
            for (int index = sfxSources.Count; index < safeCount; index++)
                CreateSfxSource();
        }

        /// <summary>
        /// 사용 가능한 효과음 AudioSource를 반환합니다.
        /// </summary>
        /// <returns>사용 가능한 AudioSource.</returns>
        private AudioSource GetAvailableSfxSource()
        {
            foreach (AudioSource source in sfxSources)
            {
                if (source != null && !source.isPlaying)
                    return source;
            }

            if (sfxSources.Count >= maxSfxSources)
                return null;

            return CreateSfxSource();
        }

        /// <summary>
        /// 효과음 AudioSource를 생성합니다.
        /// </summary>
        /// <returns>생성된 AudioSource.</returns>
        private AudioSource CreateSfxSource()
        {
            AudioSource source;
            if (sfxSourcePrefab != null)
                source = Instantiate(sfxSourcePrefab, transform);
            else
            {
                GameObject sourceObject = new("SFX Source");
                sourceObject.transform.SetParent(transform, false);
                source = sourceObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            sfxSources.Add(source);
            return source;
        }

        /// <summary>
        /// 현재 볼륨 설정을 AudioSource에 적용합니다.
        /// </summary>
        private void ApplyVolumes()
        {
            if (musicSource != null)
                musicSource.volume = masterVolume * musicVolume;

            foreach (AudioSource source in sfxSources)
            {
                if (source != null && !source.isPlaying)
                    source.volume = masterVolume * sfxVolume;
            }
        }

        /// <summary>
        /// 기존 음악을 페이드아웃한 뒤 새 음악으로 교체합니다.
        /// </summary>
        private IEnumerator FadeToNewMusicRoutine(AudioClip clip, bool loop, float duration)
        {
            yield return FadeMusicVolumeRoutine(musicSource.volume, 0f, duration * 0.5f);

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();

            yield return FadeMusicVolumeRoutine(0f, masterVolume * musicVolume, duration * 0.5f);
            musicFadeRoutine = null;
            SWLog.Log($"[SWAudioManager] Fade music complete: {clip.name}");
        }

        /// <summary>
        /// 음악을 페이드아웃하고 정지합니다.
        /// </summary>
        private IEnumerator FadeOutMusicRoutine(float duration)
        {
            yield return FadeMusicVolumeRoutine(musicSource.volume, 0f, duration);

            musicSource.Stop();
            musicSource.volume = masterVolume * musicVolume;
            musicFadeRoutine = null;
        }

        /// <summary>
        /// 음악 볼륨을 지정 시간 동안 보간합니다.
        /// </summary>
        private IEnumerator FadeMusicVolumeRoutine(float fromVolume, float toVolume, float duration)
        {
            if (duration <= 0f)
            {
                musicSource.volume = toVolume;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(fromVolume, toVolume, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            musicSource.volume = toVolume;
        }
        #endregion // 내부
    }
}
