using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

public sealed class AdMobRewardedAdService : IRewardedAdService
{
#if UNITY_ANDROID
    const string RewardedId = "ca-app-pub-4673017826771391/3563918015";
#elif UNITY_IOS
    const string RewardedId = "ca-app-pub-3940256099942544/1712485313";
#else
    const string RewardedId = "unused";
#endif

    public static AdMobRewardedAdService Shared { get; } = new AdMobRewardedAdService();

    RewardedAd m_ad;
    bool m_loading;
    bool m_showing;
    Action<bool> m_completion;
    bool m_rewardEarned;

    public bool IsReady => !AdsRemoved && !m_showing && m_ad != null && m_ad.CanShowAd();

    AdMobRewardedAdService()
    {
#if UNITY_ANDROID || UNITY_IOS
        MobileAds.Initialize(status =>
        {
            if (status != null) MobileAdsEventExecutor.ExecuteInUpdate(Load);
        });
#endif
    }

    public void Show(Action<bool> completed)
    {
        if (!IsReady)
        {
            completed?.Invoke(false);
            if (!AdsRemoved) Load();
            return;
        }

        m_showing = true;
        m_rewardEarned = false;
        m_completion = completed;
        RewardedAd ad = m_ad;
        m_ad = null;

        ad.OnAdFullScreenContentClosed += () =>
            MobileAdsEventExecutor.ExecuteInUpdate(() => Finish(ad, m_rewardEarned));
        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning($"AdMob rewarded ad failed to open: {error}");
            MobileAdsEventExecutor.ExecuteInUpdate(() => Finish(ad, false));
        };
        ad.Show(reward => m_rewardEarned = true);
    }

    void Load()
    {
        if (AdsRemoved || m_loading || m_showing || m_ad != null) return;
        m_loading = true;
        RewardedAd.Load(RewardedId, new AdRequest(), (ad, error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() => CompleteLoad(ad, error));
        });
    }

    void CompleteLoad(RewardedAd ad, LoadAdError error)
    {
            m_loading = false;
            if (error != null || ad == null)
            {
                Debug.LogWarning($"AdMob rewarded ad failed to load: {error}");
                return;
            }

            if (AdsRemoved)
            {
                ad.Destroy();
                return;
            }

            m_ad = ad;
    }

    void Finish(RewardedAd ad, bool success)
    {
        if (!m_showing)
        {
            ad.Destroy();
            return;
        }
        ad.Destroy();
        m_showing = false;
        Action<bool> completion = m_completion;
        m_completion = null;
        completion?.Invoke(success);
        if (!AdsRemoved) Load();
    }

    static bool AdsRemoved => GManager.Instance != null &&
                              GManager.Instance.IsProgress != null &&
                              GManager.Instance.IsProgress.AdsRemoved;
}
