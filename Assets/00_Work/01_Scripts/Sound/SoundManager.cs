using System.Collections;
using System.Collections.Generic;
using _00_Work._01_Scripts.UI;
using _00_Work._01_Scripts.UI.UIs;
using UnityEngine;
using UnityEngine.Audio;

namespace _00_Work._01_Scripts.Sound
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance;

        [Header("설정")]
        [SerializeField] private UISettings uiSettings;
        
        [Header("Audio Mixer Groups")]
        [SerializeField] private AudioMixerGroup sfxMixerGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip[] bgmClips;
        [SerializeField] private SFXClipData[] sfxClips;

        [System.Serializable]
        public class SFXClipData
        {
            public AudioClip clip;
            public bool      isRandom;
            public float     minPitch        = 0.9f;
            public float     maxPitch        = 1.1f;
        }

        private Dictionary<string, AudioClip>  _bgmDict = new();
        private Dictionary<string, SFXClipData> _sfxDict = new();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitDictionaries();
        }
        
        void Start()
        {
            uiSettings.LoadSettingsOnStartup();
            if(!UIManager.Instance.isMainMenu)
                PlayBGM("Hexagon");
            else
                PlayBGM("Stellar");
        }

        void InitDictionaries()
        {
            foreach (var clip in bgmClips)
                if (clip != null) _bgmDict[clip.name] = clip;

            foreach (var data in sfxClips)
                if (data?.clip != null) _sfxDict[data.clip.name] = data;
        }

        public void PlayBGM(string clipName, bool loop = true)
        {
            if (!_bgmDict.TryGetValue(clipName, out var clip))
            {
                Debug.LogWarning($"BGM 없음: {clipName}");
                return;
            }

            if (bgmSource.clip == clip && bgmSource.isPlaying) return;

            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.Play();
        }

        public void StopBGM()   => bgmSource.Stop();
        public void PauseBGM()  => bgmSource.Pause();
        public void ResumeBGM() => bgmSource.UnPause();

        public void PlaySFX(string clipName, float volume = 1f)
        {
            PlaySFXInternal(clipName, Vector3.zero, volume, is2D: true);
        }

        public void PlaySFXAt(
            string    clipName,
            Transform target,
            float     volume  = 1f,
            float     minDist = 1f,
            float     maxDist = 30f)
        {
            if (target == null) { PlaySFX(clipName, volume); return; }
            PlaySFXInternal(clipName, target.position, volume,
                minDist: minDist, maxDist: maxDist);
        }

        void PlaySFXInternal(
            string  clipName,
            Vector3 position,
            float   volume  = 1f,
            float   minDist = 1f,
            float   maxDist = 30f,
            bool    is2D    = false)
        {
            if (!_sfxDict.TryGetValue(clipName, out var data))
            {
                Debug.LogWarning($"SFX 없음: {clipName}");
                return;
            }

            var obj = new GameObject($"SFX_{clipName}");
            obj.transform.position = position;

            var source = obj.AddComponent<AudioSource>();
            source.clip                  = data.clip;
            source.volume                = volume;
            source.spatialBlend          = is2D ? 0f : 1f;
            source.minDistance           = minDist;
            source.maxDistance           = maxDist;
            source.rolloffMode           = AudioRolloffMode.Linear;
            source.outputAudioMixerGroup = sfxMixerGroup;

            source.pitch = data.isRandom
                ? Random.Range(data.minPitch, data.maxPitch)
                : 1f;

            source.Play();
            Destroy(obj, data.clip.length / source.pitch + 0.1f);
        }

        public void RegisterBGM(AudioClip clip)
        {
            if (clip != null) _bgmDict[clip.name] = clip;
        }

        public void RegisterSFX(SFXClipData data)
        {
            if (data?.clip != null) _sfxDict[data.clip.name] = data;
        }

        public void LogClipNames()
        {
            Debug.Log("BGM: " + string.Join(", ", _bgmDict.Keys));
            Debug.Log("SFX: " + string.Join(", ", _sfxDict.Keys));
        }
        
        public void StopSFX(string clipName)
        {
            var objs = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (var src in objs)
                if (src.gameObject.name == $"SFX_{clipName}" && src.isPlaying)
                    Destroy(src.gameObject);
        }
        
        public void FadeOutBGM(float duration = 1f)
        {
            StartCoroutine(FadeOutBGMRoutine(duration));
        }

        private IEnumerator FadeOutBGMRoutine(float duration)
        {
            float startVolume = bgmSource.volume;
            float elapsed     = 0f;

            while (elapsed < duration)
            {
                elapsed           += Time.unscaledDeltaTime;
                bgmSource.volume   = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.volume = startVolume; // 볼륨 복원
        }
    }
}