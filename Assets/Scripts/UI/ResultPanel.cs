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
        m_rewardedAdService = new MockCommerceService();
        if (m_restartButton != null) m_restartButton.onClick.AddListener(ClaimBasicReward);
        if (m_exitButton != null) m_exitButton.onClick.AddListener(ClaimDoubleReward);
    }

    public void Setup(bool isClear, int finalWave)
    {
        gameObject.SetActive(true);
        m_claimingReward = false;
        m_rewardAmount = GManager.Instance != null ? GManager.Instance.GetPendingRunCrystalReward() : 0;

        SetButtonLabel(m_restartButton, "보상받기");
        SetButtonLabel(m_exitButton, "광고 보고 2배 받기", true);
        SetRewardAmount(m_basicRewardAmountText, m_rewardAmount);
        SetRewardAmount(m_doubleRewardAmountText, m_rewardAmount * 2);

        if (m_titleText != null)
        {
            m_titleText.text = isClear ? "게임 클리어" : "게임 오버";
            m_titleText.color = isClear ? m_clearColor : m_gameOverColor;
        }

        if (m_detailsText != null)
        {
            string result = isClear ? "50웨이브를 클리어했습니다." : $"{finalWave}웨이브에 도달했습니다.";
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
        if (m_claimingReward) return;
        m_claimingReward = true;
        m_rewardedAdService.Show(success =>
        {
            if (success) CompleteClaim(2);
            else
            {
                m_claimingReward = false;
                if (m_detailsText != null) m_detailsText.text = "광고를 불러오지 못했습니다. 다시 시도해 주세요.";
            }
        });
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
