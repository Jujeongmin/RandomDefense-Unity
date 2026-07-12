using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameAudioManager : MonoBehaviour
{
    public enum Sfx { Click, Confirm, Error, Summon, RareSummon, Sell, Upgrade, MobDeath, BossDeath, Victory, Defeat, AttackWarrior, AttackArcher, AttackWizard }

    static GameAudioManager s_instance;
    GameAudioLibrary m_library;
    AudioSource m_bgm;
    AudioSource m_sfx;
    AudioClip m_currentBgm;
    Coroutine m_bgmFade;
    float m_lastMobDeathTime;
    float m_lastSellTime;
    float m_lastAttackTime;

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
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include))
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
        // 자동판매 등으로 판매음이 연속 재생될 때 겹쳐 커지는 것을 방지
        if (type == Sfx.Sell && Time.unscaledTime - s_instance.m_lastSellTime < 0.1f) return;
        if (type == Sfx.Sell) s_instance.m_lastSellTime = Time.unscaledTime;
        // 유닛 다수가 동시에 공격할 때 공격음이 겹쳐 시끄러워지는 것을 방지
        bool isAttack = type is Sfx.AttackWarrior or Sfx.AttackArcher or Sfx.AttackWizard;
        if (isAttack && Time.unscaledTime - s_instance.m_lastAttackTime < 0.03f) return;
        if (isAttack) s_instance.m_lastAttackTime = Time.unscaledTime;
        AudioClip clip = s_instance.GetClip(type);
        float volumeScale = type switch
        {
            Sfx.MobDeath or Sfx.BossDeath => 0.4f,
            Sfx.Sell => 0.7f,
            Sfx.AttackWarrior or Sfx.AttackArcher or Sfx.AttackWizard => 0.2f,
            Sfx.Victory or Sfx.Defeat => 0.5f,
            _ => 1f
        };
        if (clip != null) s_instance.m_sfx.PlayOneShot(clip, volumeScale);
    }

    AudioClip GetClip(Sfx type) => type switch
    {
        Sfx.Click => m_library.buttonClick,
        Sfx.Confirm => m_library.confirm,
        Sfx.Error => m_library.error,
        Sfx.Summon => m_library.summon,
        Sfx.RareSummon => m_library.rareSummon,
        Sfx.Sell => m_library.sell,
        Sfx.Upgrade => m_library.upgrade,
        Sfx.MobDeath => m_library.mobDeath,
        Sfx.BossDeath => m_library.bossDeath,
        Sfx.AttackWarrior => m_library.attackWarrior,
        Sfx.AttackArcher => m_library.attackArcher,
        Sfx.AttackWizard => m_library.attackWizard,
        Sfx.Victory => m_library.victory,
        Sfx.Defeat => m_library.defeat,
        _ => null
    };
}
