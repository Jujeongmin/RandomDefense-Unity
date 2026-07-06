using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI m_titleText = null;
    [SerializeField] TextMeshProUGUI m_detailsText = null;
    [SerializeField] Button m_restartButton = null;
    [SerializeField] Button m_exitButton = null;
    [SerializeField] TextMeshProUGUI m_basicRewardAmountText = null;
    [SerializeField] TextMeshProUGUI m_doubleRewardAmountText = null;

    [Header("Color Settings")]
    [SerializeField] Color m_clearColor = Color.green;
    [SerializeField] Color m_gameOverColor = Color.red;

    IRewardedAdService m_rewardedAdService;
    int m_rewardAmount;
    bool m_claimingReward;

    void Start()
    {
        if (GManager.Instance != null && GManager.Instance.IsResultPanel != this)
            GManager.Instance.RegisterResultPanel(this);
#if UNITY_EDITOR
        m_rewardedAdService = new MockCommerceService();
#else
        m_rewardedAdService = AdMobRewardedAdService.Shared;
#endif
        if (m_restartButton != null) m_restartButton.onClick.AddListener(ClaimBasicReward);
        if (m_exitButton != null) m_exitButton.onClick.AddListener(ClaimDoubleReward);
    }

    public void Setup(bool isClear, int finalWave)
    {
        gameObject.SetActive(true);
        m_claimingReward = false;
        m_rewardAmount = GManager.Instance != null ? GManager.Instance.GetPendingRunCrystalReward() : 0;

        SetButtonLabel(m_restartButton, GameLanguage.Choose("보상받기", "CLAIM"));
        SetButtonLabel(m_exitButton, GameLanguage.Choose("광고 보고 2배 받기", "WATCH AD · CLAIM X2"), true);
        SetRewardAmount(m_basicRewardAmountText, m_rewardAmount);
        SetRewardAmount(m_doubleRewardAmountText, m_rewardAmount * 2);
        RefreshRewardedButton();

        if (m_titleText != null)
        {
            m_titleText.text = isClear
                ? GameLanguage.Choose("게임 클리어", "VICTORY")
                : GameLanguage.Choose("게임 오버", "GAME OVER");
            m_titleText.color = isClear ? m_clearColor : m_gameOverColor;
        }

        if (m_detailsText != null)
        {
            string result = isClear
                ? GameLanguage.Choose("50웨이브를 클리어했습니다.", "YOU CLEARED WAVE 50!")
                : GameLanguage.Choose($"{finalWave}웨이브에 도달했습니다.", $"YOU REACHED WAVE {finalWave}.");
            m_detailsText.text = result;
        }
    }

    void ClaimBasicReward()
    {
        if (m_claimingReward) return;
        m_claimingReward = true;
        CompleteClaim(1);
    }

    void ClaimDoubleReward()
    {
        if (m_claimingReward || m_rewardedAdService == null || !m_rewardedAdService.IsReady) return;
        m_claimingReward = true;
        m_rewardedAdService.Show(success =>
        {
            if (success) CompleteClaim(2);
            else
            {
                m_claimingReward = false;
                if (m_detailsText != null)
                    m_detailsText.text = GameLanguage.Choose(
                        "광고를 불러오지 못했습니다. 다시 시도해 주세요.",
                        "THE AD COULD NOT BE LOADED. PLEASE TRY AGAIN.");
            }
        });
    }

    void Update()
    {
        if (gameObject.activeInHierarchy) RefreshRewardedButton();
    }

    void RefreshRewardedButton()
    {
        if (m_exitButton == null) return;
        PlayerProgressManager progress = GManager.Instance != null ? GManager.Instance.IsProgress : null;
        bool adsAllowed = progress != null && !progress.AdsRemoved;
        m_exitButton.gameObject.SetActive(adsAllowed);
        if (adsAllowed) m_exitButton.interactable = !m_claimingReward && m_rewardedAdService != null && m_rewardedAdService.IsReady;
    }

    void CompleteClaim(int multiplier)
    {
        if (GManager.Instance != null) GManager.Instance.ClaimRunCrystalReward(multiplier);
        Time.timeScale = 1f;
        SceneManager.LoadScene(GManager.SCENE_MAIN);
    }

    static void SetButtonLabel(Button button, string label, bool fitSingleLine = false)
    {
        if (button == null) return;
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text == null) return;

        text.text = label;
        if (!fitSingleLine) return;

        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = 24f;
    }

    static void SetRewardAmount(TextMeshProUGUI text, int amount)
    {
        if (text != null) text.text = $"+{amount:N0}";
    }
}
