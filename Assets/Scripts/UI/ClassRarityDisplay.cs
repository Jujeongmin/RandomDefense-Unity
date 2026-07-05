using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClassRarityDisplay : MonoBehaviour
{
    [SerializeField] Image m_commonImg = null;
    [SerializeField] Image m_rareImg = null;
    [SerializeField] Image m_eliteImg = null;
    [SerializeField] Image m_legendaryImg = null;
    [SerializeField] Image m_mythicImg = null;
    [SerializeField] Image m_eternalImg = null;

    [Header("Count Texts (optional)")]
    [SerializeField] TextMeshProUGUI m_commonCount = null;
    [SerializeField] TextMeshProUGUI m_rareCount = null;
    [SerializeField] TextMeshProUGUI m_eliteCount = null;
    [SerializeField] TextMeshProUGUI m_legendaryCount = null;
    [SerializeField] TextMeshProUGUI m_mythicCount = null;
    [SerializeField] TextMeshProUGUI m_eternalCount = null;

    private void Start()
    {
        if (GManager.Instance != null) GManager.Instance.RegisterClassRarityDisplay(this);
    }

    private void OnDestroy()
    {
        if (GManager.Instance != null && GManager.Instance.IsClassRarityDisplay == this)
            GManager.Instance.RegisterClassRarityDisplay(null);
    }

    public void UpdateRarityImages(EntityType.TYPE classType)
    {
        // Guard
        if (GManager.Instance == null) return;
        var gm = GManager.Instance;

        if (m_commonImg != null) m_commonImg.sprite = gm.GetSprite(classType, RarityType.TYPE.Common);
        if (m_rareImg != null) m_rareImg.sprite = gm.GetSprite(classType, RarityType.TYPE.Rare);
        if (m_eliteImg != null) m_eliteImg.sprite = gm.GetSprite(classType, RarityType.TYPE.Elite);
        if (m_legendaryImg != null) m_legendaryImg.sprite = gm.GetSprite(classType, RarityType.TYPE.Legendary);
        if (m_mythicImg != null) m_mythicImg.sprite = gm.GetSprite(classType, RarityType.TYPE.Mythic);
        if (m_eternalImg != null) m_eternalImg.sprite = gm.GetSprite(classType, RarityType.TYPE.Eternal);

        // Update optional count texts if region manager exists
        if (gm.IsRegion != null)
        {
            UpdateCountText(m_commonCount, gm.IsRegion.GetCountByClassAndRarity(classType, RarityType.TYPE.Common));
            UpdateCountText(m_rareCount, gm.IsRegion.GetCountByClassAndRarity(classType, RarityType.TYPE.Rare));
            UpdateCountText(m_eliteCount, gm.IsRegion.GetCountByClassAndRarity(classType, RarityType.TYPE.Elite));
            UpdateCountText(m_legendaryCount, gm.IsRegion.GetCountByClassAndRarity(classType, RarityType.TYPE.Legendary));
            UpdateCountText(m_mythicCount, gm.IsRegion.GetCountByClassAndRarity(classType, RarityType.TYPE.Mythic));
            UpdateCountText(m_eternalCount, gm.IsRegion.GetCountByClassAndRarity(classType, RarityType.TYPE.Eternal));
        }
    }

    void UpdateCountText(TextMeshProUGUI textComp, int value)
    {
        if (textComp == null) return;
        textComp.text = value.ToString();
    }
}
