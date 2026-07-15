#if GPGS_ENABLED && UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
using UnityEngine;

/// <summary>
/// Google Play Games 리더보드(무한모드 도달 웨이브) 래퍼.
/// 플러그인이 없어도 컴파일되도록 GPGS_ENABLED 심볼로 감쌉니다.
/// 활성화하려면 Project Settings > Player > Scripting Define Symbols(Android)에
/// GPGS_ENABLED를 추가하세요. 플러그인 설치와 리더보드 ID 연결은 이미 돼 있습니다.
/// </summary>
public static class LeaderboardService
{
    const string EndlessLeaderboardId = "CgkInLfH0NESEAIQAA";

    public static bool IsAvailable
    {
#if GPGS_ENABLED && UNITY_ANDROID
        get => PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.IsAuthenticated();
#else
        get => false;
#endif
    }

    /// <summary>앱 시작 시 1회 호출. 구글 계정으로 자동 로그인 시도.</summary>
    public static void Initialize()
    {
#if GPGS_ENABLED && UNITY_ANDROID
        PlayGamesPlatform.Activate();
        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            Debug.Log($"[Leaderboard] 로그인 결과: {status}");
        });
#endif
    }

    /// <summary>무한모드 도달 웨이브를 리더보드에 제출.</summary>
    public static void SubmitEndlessWave(int wave)
    {
#if GPGS_ENABLED && UNITY_ANDROID
        if (!IsAvailable) return;
        PlayGamesPlatform.Instance.ReportScore(wave, EndlessLeaderboardId, success =>
        {
            Debug.Log($"[Leaderboard] 웨이브 {wave} 제출: {success}");
        });
#endif
    }

    /// <summary>구글 기본 리더보드 화면을 띄웁니다.</summary>
    public static void ShowLeaderboard()
    {
#if GPGS_ENABLED && UNITY_ANDROID
        if (IsAvailable)
        {
            PlayGamesPlatform.Instance.ShowLeaderboardUI(EndlessLeaderboardId);
        }
        else
        {
            // 미로그인 상태면 로그인 후 표시 시도
            PlayGamesPlatform.Instance.Authenticate(status =>
            {
                if (status == SignInStatus.Success)
                    PlayGamesPlatform.Instance.ShowLeaderboardUI(EndlessLeaderboardId);
            });
        }
#else
        Debug.LogWarning("[Leaderboard] GPGS 미설치 상태입니다. 플러그인 설치 + GPGS_ENABLED 심볼 추가 후 사용 가능.");
#endif
    }
}
