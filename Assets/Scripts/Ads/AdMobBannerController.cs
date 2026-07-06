using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class AdMobBannerController : MonoBehaviour
{
    const string MainScene = "MainScene";
    const string GameScene = "GameScene";

#if UNITY_ANDROID
    const string BannerId = "ca-app-pub-4673017826771391/8693910058";
#elif UNITY_IOS
    const string BannerId = "ca-app-pub-3940256099942544/2435281174";
#else
    const string BannerId = "unused";
#endif

    static AdMobBannerController s_instance;
    BannerView m_banner;
    bool m_initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (s_instance != null) return;
        GameObject host = new GameObject(nameof(AdMobBannerController));
        DontDestroyOnLoad(host);
        s_instance = host.AddComponent<AdMobBannerController>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        InitializeAds();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        DestroyBanner();
    }

    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (m_banner != null && GManager.Instance != null &&
            GManager.Instance.IsProgress != null && GManager.Instance.IsProgress.AdsRemoved)
        {
            DestroyBanner();
        }
#endif
    }

    void InitializeAds()
    {
#if UNITY_ANDROID || UNITY_IOS
        MobileAds.Initialize(status =>
        {
            m_initialized = status != null;
            RefreshForScene(SceneManager.GetActiveScene());
        });
#endif
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RefreshForScene(scene);

    void RefreshForScene(Scene scene)
    {
#if UNITY_ANDROID || UNITY_IOS
        bool supportedScene = scene.name == MainScene || scene.name == GameScene;
        bool adsRemoved = GManager.Instance != null &&
                          GManager.Instance.IsProgress != null &&
                          GManager.Instance.IsProgress.AdsRemoved;

        if (!m_initialized || !supportedScene || adsRemoved)
        {
            DestroyBanner();
            return;
        }

        if (m_banner == null) LoadBanner();
#endif
    }

    void LoadBanner()
    {
        int safeWidth = MobileAds.Utils.GetDeviceSafeWidth();
        AdSize size = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(safeWidth);
        m_banner = new BannerView(BannerId, size, AdPosition.Bottom);
        m_banner.OnBannerAdLoadFailed += error =>
            Debug.LogWarning($"AdMob banner failed to load: {error}");
        m_banner.LoadAd(new AdRequest());
    }

    void DestroyBanner()
    {
        if (m_banner == null) return;
        m_banner.Destroy();
        m_banner = null;
    }
}
