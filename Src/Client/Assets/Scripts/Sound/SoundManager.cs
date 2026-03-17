using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 音频中控：
/// 1) 管理 BGM / SFX 两条通道（两个 AudioSource）
/// 2) 通过 AudioMixer 的 Exposed Parameter 控制音量/静音
/// 3) 提供 PlayMusic / PlaySound 的统一播放入口
///
/// 重要约定：
/// - UI 的 Slider 用 0~100 的 int 作为音量值
/// - Mixer 里暴露两个参数名：MusicVolume、SoundVolume
/// </summary>
public class SoundManager : MonoSingleton<SoundManager>
{
    [Header("Mixer & Sources")]
    public AudioMixer audioMixer;          // Unity 的“调音台”，用它控制总线音量
    public AudioSource musicAudioSource;   // BGM 专用播放器（通常单曲循环）
    public AudioSource soundAudioSource;   // SFX 专用播放器（用 PlayOneShot 叠加短音效）

    private readonly Dictionary<string, AudioClip> _clipCache
    = new Dictionary<string, AudioClip>();  // 首次加载后缓存

    // Resources 下的路径前缀（用的是 Resloader.Load，底层也会走 Resources）
    private const string MusicPath = "Music/";
    private const string SoundPath = "Sound/";

    // Mixer 暴露参数名（必须和 AudioMixer 面板 Exposed Parameters 完全一致）
    private const string MixerMusicVolume = "MusicVolume";
    private const string MixerSoundVolume = "SoundVolume";

    // 静音用的最小 dB（Unity Mixer 面板最小通常就是 -80）
    private const float MinDb = -80f;   // Unity Mixer 常用“完全静音”值
    private const float MaxDb = 0f;     // 0dB = 原始音量

    #region Volume

    // 0~100：用于 UI 与存档，属于“玩家输入的音量值”
    private int musicVolume;
    public int MusicVolume
    {
        get => musicVolume;
        set
        {
            value = Mathf.Clamp(value, 0, 100);
            if (musicVolume == value) return;

            musicVolume = value;

            // 注意：如果音乐当前是开启状态，实时写入 Mixer
            if (musicOn)
                SetVolume01ToMixerDb(MixerMusicVolume, musicVolume);
        }
    }

    private int soundVolume;
    public int SoundVolume
    {
        get => soundVolume;
        set
        {
            value = Mathf.Clamp(value, 0, 100);
            if (soundVolume == value) return;

            soundVolume = value;

            if (soundOn)
                SetVolume01ToMixerDb(MixerSoundVolume, soundVolume);
        }
    }
    /*private int musicVolume;
    public int MusicVolume
    {
        get { return musicVolume; }
        set
        {
            if(musicVolume != value)
            {
                musicVolume = value;
                if (musicOn)
                    this.SetVolume("MusicVolume", musicVolume);
            }
        }
    }
    private int soundVolume;
    public int SoundVolume
    {
        get { return soundVolume; }
        set
        {
            if (soundVolume != value)
            {
                soundVolume = value;
                if (soundOn)
                    this.SetVolume("SoundVolume", soundVolume);
            }
        }
    }*/

    #endregion

    #region On/Off

    private bool musicOn;
    public bool MusicOn
    {
        get => musicOn;
        set
        {
            musicOn = value;
            MusicMute(!musicOn); // 关闭=静音，开启=恢复音量
        }
    }

    private bool soundOn;
    public bool SoundOn
    {
        get => soundOn;
        set
        {
            soundOn = value;
            SoundMute(!soundOn);
        }
    }

    #endregion

    void Start()
    {
        // 启动时从 Config 还原玩家设置（Config 内部读 PlayerPrefs）
        // 先还原数值，再还原开关（开关会触发 Mute/恢复，从而把值写进 Mixer）
        MusicVolume = Config.MusicVolume;
        SoundVolume = Config.SoundVolume;
        MusicOn = Config.MusicOn;
        SoundOn = Config.SoundOn;
    }


    /// <summary>
    /// BGM 静音/恢复：
    /// - 静音：写入 MinDb（-80dB）
    /// - 恢复：写入当前 musicVolume（0~100）
    /// </summary>
    public void MusicMute(bool mute)
    {
        if (mute)
            SetMixerDb(MixerMusicVolume, MinDb);    // -80dB = 静音
        else
            SetVolume01ToMixerDb(MixerMusicVolume, musicVolume);    // 恢复
    }

    public void SoundMute(bool mute)
    {
        if (mute)
            SetMixerDb(MixerSoundVolume, MinDb);
        else
            SetVolume01ToMixerDb(MixerSoundVolume, soundVolume);
    }

    /// <summary>
    /// 把“0~100 的玩家音量值”转换成 Mixer 的 dB：
    /// - 先归一化到 0~1
    /// - 再用 20*log10(x) 映射到 dB（符合人耳对响度的对数感知）
    ///
    /// 映射效果大概是：
    /// - 100 -> 0 dB（原始音量）
    /// - 50  -> -6 dB（听感大约“减半”）
    /// - 10  -> -20 dB
    /// - 0   -> -80 dB（接近听不见）
    /// </summary>
    private void SetVolume01ToMixerDb(string paramName, int volume0To100)
    {
        float t = Mathf.Clamp01(volume0To100 / 100f);     // 0~100 -> 0~1
        float db = (t <= 0f) ? MinDb : Mathf.Log10(t) * 20f;

        SetMixerDb(paramName, db);
    }

    /// <summary>
    /// 真正写入 Mixer 的地方。
    /// 如果参数名没暴露/拼错，SetFloat 会返回 false，这里直接报错方便你定位。
    /// </summary>
    private void SetMixerDb(string paramName, float db)
    {
        Debug.Log($"[SoundManager] {gameObject.scene.name} id={GetInstanceID()} Set {paramName}={db}");

        if (audioMixer == null)
        {
            Debug.LogError("[SoundManager] audioMixer is null.");
            return;
        }

        if (!audioMixer.SetFloat(paramName, db))
            Debug.LogError($"[SoundManager] SetFloat failed: '{paramName}' not exposed?");
    }
    private AudioClip GetClip(string fullPath)
    {
        if (_clipCache.TryGetValue(fullPath, out AudioClip clip))
            return clip; // 命中缓存，零开销

        clip = Resources.Load<AudioClip>(fullPath);
        if (clip != null)
            _clipCache[fullPath] = clip;
        else
            Debug.LogError($"[SoundManager] Clip not found: {fullPath}");
        return clip;
    }
    // BGM
    public void PlayMusic(string name)
    {
        // 从资源加载 BGM：Resources/Music/name
        AudioClip clip = GetClip(MusicPath + name);
        if (clip == null) return;
        if (musicAudioSource.clip == clip) return; // 已经在播，不重复切换

        musicAudioSource.Stop();
        musicAudioSource.clip = clip;
        musicAudioSource.Play();
    }
    // SFX：PlayOneShot 允许同一个 AudioSource 叠加多个短音效
    // 比如连续点击按钮，声音不会被截断
    public void PlaySound(string name)
    {
        // 从资源加载 SFX：Resources/Sound/name
        AudioClip clip = GetClip(SoundPath + name);
        if (clip != null)
            soundAudioSource.PlayOneShot(clip);
    }
    /*
     * Play()：打断当前正在播的 clip，只能同时播一个
     * PlayOneShot(clip)：叠加播放，不打断其他声音，适合 SFX 密集触发
     */
}
