using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class AdMobBannerController : MonoBehaviour
{
    const string MainScene = "MainScene";

#if UNITY_ANDROID
    const string RealBannerId = "ca-app-pub-4673017826771391/8693910058";
    const string TestBannerId = "ca-app-pub-3940256099942544/6300978111"; // Google 공식 테스트 배너
#elif UNITY_IOS
    const string RealBannerId = "ca-app-pub-3940256099942544/2435281174";
    const string TestBannerId = "ca-app-pub-3940256099942544/2934735716";
#else
    const string RealBannerId = "unused";
    const string TestBannerId = "unused";
#endif

    // 개발 빌드(Development Build 체크)에선 테스트 광고, 정식 빌드에선 실 광고
    static string BannerId => Debug.isDebugBuild ? TestBannerId : RealBannerId;

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
        // 배너는 메인메뉴에서만 표시 (게임 중에는 하단 UI를 가리므로 끔)
        bool supportedScene = scene.name == MainScene;
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
