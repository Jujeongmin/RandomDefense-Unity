using UnityEngine;
#if UNITY_ANDROID || UNITY_IOS
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif
#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

/// <summary>
/// 앱 시작 시 개인정보 동의를 처리합니다.
/// - iOS: ATT(App Tracking Transparency) 권한 요청 (iOS 14.5+ 필수)
/// - GDPR(유럽): Google UMP 동의 폼 표시
/// UMP가 저장한 동의를 GoogleMobileAds SDK가 자동으로 반영하므로,
/// 기존 광고 로딩 코드는 수정하지 않아도 됩니다.
/// </summary>
public static class AdConsentManager
{
    static bool s_started;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (s_started) return;
        s_started = true;

#if UNITY_ANDROID || UNITY_IOS
        // 전체이용가 게임에 맞게 성인용 광고 차단: T(Teen) 이하 등급의 광고만 허용
        MobileAds.SetRequestConfiguration(new RequestConfiguration
        {
            MaxAdContentRating = MaxAdContentRating.T
        });
#endif

#if UNITY_IOS
        RequestAppTrackingTransparency();
#endif
        GatherConsent();
    }

#if UNITY_IOS
    static void RequestAppTrackingTransparency()
    {
        if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus()
            == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            ATTrackingStatusBinding.RequestAuthorizationTracking();
        }
    }
#endif

    static void GatherConsent()
    {
#if UNITY_ANDROID || UNITY_IOS
        ConsentRequestParameters request = new ConsentRequestParameters();

        // ── 개발 중 동의 폼 강제 테스트가 필요하면 아래 주석을 해제하세요 ──
        // request.ConsentDebugSettings = new ConsentDebugSettings
        // {
        //     DebugGeography = DebugGeography.EEA,
        //     TestDeviceHashedIds = new System.Collections.Generic.List<string> { "여기에-테스트-기기-해시-ID" }
        // };

        ConsentInformation.Update(request, updateError =>
        {
            if (updateError != null)
            {
                Debug.LogWarning($"[Consent] 동의 정보 갱신 실패: {updateError.Message}");
                return;
            }

            ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            {
                if (formError != null)
                    Debug.LogWarning($"[Consent] 동의 폼 표시 실패: {formError.Message}");
            });
        });
#endif
    }
}
