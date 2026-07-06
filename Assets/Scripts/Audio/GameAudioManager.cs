using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameAudioManager : MonoBehaviour
{
    public enum Sfx { Click, Confirm, Error, Summon, Sell, Upgrade, MobDeath, BossDeath, Victory, Defeat }

    static GameAudioManager s_instance;
    GameAudioLibrary m_library;
    AudioSource m_bgm;
    AudioSource m_sfx;
    AudioClip m_currentBgm;
    Coroutine m_bgmFade;
    float m_lastMobDeathTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        if (s_instance != null) return;
        GameObject host = new GameObject(nameof(GameAudioManager));
        DontDestroyOnLoad(host);
        s_instance = host.AddComponent<GameAudioManager>();
    }

    void Awake()
    {
        m_library = Resources.Load<GameAudioLibrary>("GameAudioLibrary");
        m_bgm = gameObject.AddComponent<AudioSource>();
        m_bgm.loop = true;
        m_bgm.playOnAwake = false;
        m_bgm.volume = 0.42f;
        m_sfx = gameObject.AddComponent<AudioSource>();
        m_sfx.playOnAwake = false;
        m_sfx.volume = 0.75f;
        GameAudioSettings.Changed += ApplySettings;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplySettings();
    }

    void OnDestroy()
    {
        GameAudioSettings.Changed -= ApplySettings;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (m_library == null) return;
        PlayBgm(scene.name == GManager.SCENE_MAIN ? m_library.mainBgm : m_library.battleBgm);
        StartCoroutine(BindButtonSounds());
    }

    IEnumerator BindButtonSounds()
    {
        yield return null;
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            button.onClick.RemoveListener(PlayButtonClick);
            button.onClick.AddListener(PlayButtonClick);
        }
    }

    void PlayButtonClick() => Play(Sfx.Click);

    void ApplySettings()
    {
        if (m_bgm != null)
        {
            m_bgm.mute = !GameAudioSettings.BgmEnabled;
            if (GameAudioSettings.BgmEnabled && m_bgm.clip != null && !m_bgm.isPlaying) m_bgm.Play();
        }
        if (m_sfx != null) m_sfx.mute = !GameAudioSettings.SfxEnabled;
    }

    void PlayBgm(AudioClip clip)
    {
        if (clip == null || clip == m_currentBgm) return;
        m_currentBgm = clip;
        if (m_bgmFade != null) StopCoroutine(m_bgmFade);
        m_bgmFade = StartCoroutine(FadeTo(clip));
    }

    IEnumerator FadeTo(AudioClip clip)
    {
        float startVolume = m_bgm.volume;
        for (float time = 0f; time < 0.25f; time += Time.unscaledDeltaTime)
        {
            m_bgm.volume = Mathf.Lerp(startVolume, 0f, time / 0.25f);
            yield return null;
        }
        m_bgm.Stop();
        m_bgm.clip = clip;
        m_bgm.volume = 0f;
        if (GameAudioSettings.BgmEnabled) m_bgm.Play();
        for (float time = 0f; time < 0.35f; time += Time.unscaledDeltaTime)
        {
            m_bgm.volume = Mathf.Lerp(0f, 0.42f, time / 0.35f);
            yield return null;
        }
        m_bgm.volume = 0.42f;
        m_bgmFade = null;
    }

    public static void EnterBoss()
    {
        if (s_instance?.m_library != null) s_instance.PlayBgm(s_instance.m_library.bossBgm);
    }

    public static void ExitBoss()
    {
        if (s_instance?.m_library != null) s_instance.PlayBgm(s_instance.m_library.battleBgm);
    }

    public static void Play(Sfx type)
    {
        if (s_instance == null || s_instance.m_library == null || !GameAudioSettings.SfxEnabled) return;
        if (type == Sfx.MobDeath && Time.unscaledTime - s_instance.m_lastMobDeathTime < 0.08f) return;
        if (type == Sfx.MobDeath) s_instance.m_lastMobDeathTime = Time.unscaledTime;
        AudioClip clip = s_instance.GetClip(type);
        if (clip != null) s_instance.m_sfx.PlayOneShot(clip);
    }

    AudioClip GetClip(Sfx type) => type switch
    {
        Sfx.Click => m_library.buttonClick,
        Sfx.Confirm => m_library.confirm,
        Sfx.Error => m_library.error,
        Sfx.Summon => m_library.summon,
        Sfx.Sell => m_library.sell,
        Sfx.Upgrade => m_library.upgrade,
        Sfx.MobDeath => m_library.mobDeath,
        Sfx.BossDeath => m_library.bossDeath,
        Sfx.Victory => m_library.victory,
        Sfx.Defeat => m_library.defeat,
        _ => null
    };
}
