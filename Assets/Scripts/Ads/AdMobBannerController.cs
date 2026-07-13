using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_ANDROID || UNITY_IOS
using GoogleMobileAds.Ump.Api;
#endif

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

    const float RetryInterval = 30f;

    static AdMobBannerController s_instance;
    BannerView m_banner;
    bool m_initialized;
    volatile bool m_loadFailed; // SDK 콜백은 다른 스레드일 수 있어 Update에서 처리
    float m_retryAt = -1f;
    volatile string m_status = "시작 전"; // 개발 빌드 화면 오버레이용
    float m_initStartedAt = -1f;
    int m_initAttempts;

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

        // 로드 실패한 배너는 정리하고 일정 시간 뒤 재시도
        if (m_loadFailed)
        {
            m_loadFailed = false;
            DestroyBanner();
            m_retryAt = Time.unscaledTime + RetryInterval;
        }
        if (m_banner == null && m_retryAt > 0f && Time.unscaledTime >= m_retryAt)
        {
            m_retryAt = -1f;
            RefreshForScene(SceneManager.GetActiveScene());
        }

        // 초기화 콜백이 15초 넘게 안 오면 재시도 (최대 3회)
        if (!m_initialized && m_initStartedAt > 0f && m_initAttempts < 3 &&
            Time.realtimeSinceStartup - m_initStartedAt > 15f)
        {
            InitializeAds();
        }
#endif
    }

    void InitializeAds()
    {
#if UNITY_ANDROID || UNITY_IOS
        // SDK 콜백을 유니티 메인 스레드로 받아 스레드 문제 방지
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        m_initAttempts++;
        m_initStartedAt = Time.realtimeSinceStartup;
        m_status = $"SDK 초기화 중... (시도 {m_initAttempts})";
        MobileAds.Initialize(status =>
        {
            m_initialized = status != null;
            m_initStartedAt = -1f;
            m_status = m_initialized ? "SDK 초기화 완료" : "SDK 초기화 실패";
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
        m_banner.OnBannerAdLoaded += () => m_status = "배너 로드 성공";
        m_banner.OnBannerAdLoadFailed += error =>
        {
            Debug.LogWarning($"AdMob banner failed to load: {error}");
            m_status = $"배너 로드 실패: {error}";
            m_loadFailed = true;
        };
        m_status = $"배너 요청 중... ({(Debug.isDebugBuild ? "테스트 ID" : "실제 ID")})";
        m_banner.LoadAd(new AdRequest());
    }

    void DestroyBanner()
    {
        if (m_banner == null) return;
        m_banner.Destroy();
        m_banner = null;
    }

    // 개발 빌드 전용: USB 로그 없이도 배너 상태를 화면에서 바로 확인 (정식 빌드에는 표시 안 됨)
    void OnGUI()
    {
        if (!Debug.isDebugBuild) return;

        string info = m_status;
        if (!m_initialized && m_initStartedAt > 0f)
            info += $" ({Time.realtimeSinceStartup - m_initStartedAt:F0}초 경과)";
#if UNITY_ANDROID || UNITY_IOS
        try
        {
            info += $"\n동의상태: {ConsentInformation.ConsentStatus} / 광고요청가능: {ConsentInformation.CanRequestAds()}";
        }
        catch { }
#endif

        var style = new GUIStyle(GUI.skin.box)
        {
            fontSize = Mathf.Max(20, Screen.height / 50),
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        GUI.Box(new Rect(10, 10, Screen.width - 20, Screen.height / 7f), $"[광고 디버그] {info}", style);
    }
}
