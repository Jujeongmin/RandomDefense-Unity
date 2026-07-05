using UnityEngine;
using UnityEngine.U2D.Animation;

public class ParentsController : MonoBehaviour
{
    /// <summary>
    /// 독립체 데이터
    /// </summary>
    public EntityData IsData { get; private set; } = null;

    /// <summary>
    /// 스프라이트 라이브러리
    /// </summary>
    public SpriteLibrary IsSpLib { get; private set; } = null;

    /// <summary>
    /// 스프라이트 리졸버
    /// </summary>
    public SpriteResolver IsSpResolver { get; private set; } = null;

    /// <summary>
    /// 이동 타입
    /// </summary>
    public MoveType.TYPE IsMoveType { get; set; } = MoveType.TYPE.Idle;

    /// <summary>
    /// 방향 타입
    /// </summary>
    public DirType.TYPE IsDirType { get; set; } = DirType.TYPE.Down;

    /// <summary>
    /// 애니메이션 인덱스
    /// </summary>
    public int IsAniIndex { get; set; } = 0;

    /// <summary>
    /// 애니메이션 카테고리
    /// </summary>
    public string IsAniCategory { get; set; } = string.Empty;

    /// <summary>
    /// 현재의 애니메이션 스케일
    /// </summary>
    public float IsNowAniScale { get; set; } = 0.0f;

    /// <summary>
    /// 셋팅 플래그
    /// </summary>
    public bool IsSettingFlag { get; set; } = false;

    /// <summary>
    /// 이동 플래그
    /// </summary>
    public bool IsMoveFlag { get; set; } = false;

    /// <summary>
    /// 유닛 등급
    /// </summary>
    public RarityType.TYPE IsRarity { get; set; } = RarityType.TYPE.Common;

    public virtual void ResetState()
    {
        IsMoveType = MoveType.TYPE.Idle;
        IsDirType = DirType.TYPE.Down;
        IsAniIndex = 0;
        IsAniCategory = string.Empty;
        IsNowAniScale = 0.0f;
        IsSettingFlag = false;
        IsMoveFlag = false;
        IsRarity = RarityType.TYPE.Common;
        m_targetMob = null;
        m_lastCategory = string.Empty;
        m_lastLabel = string.Empty;
        m_aniTimer = 0f;
        
        if (m_atkEffectObj != null)
        {
            m_atkEffectObj.SetActive(false);
        }
    }

    public virtual void Setting(EntityType.TYPE argEntityType, int argEntityIndex)
    {
        ResetState();
        IsSpLib = GetComponent<SpriteLibrary>();
        IsSpResolver = GetComponent<SpriteResolver>();
        IsData = GManager.Instance.IsUnitData.Get(argEntityType, argEntityIndex);
        IsSpLib.spriteLibraryAsset = IsData.IsSpLibAsset;
        IsSpResolver.SetCategoryAndLabel("IdleDown", "0");
        IsSettingFlag = true;
        // Set HP from data
        if (IsData != null) SetMaxHp(IsData.IsHp);
    }

    void OnTransformParentChanged()
    {
    }

    // Called every frame by Character.Update
    public virtual void Tick()
    {
        UpdateAttackTarget();
        UpdateAnimationIndex();
        UpdateAnimationResolver();
    }

    void UpdateAnimationIndex()
    {
        if (IsMoveType == MoveType.TYPE.Attack || IsMoveType == MoveType.TYPE.Walk)
        {
            float speed = (IsData != null && IsData.IsAniScale > 0) ? IsData.IsAniScale : 1.0f;
            m_aniTimer += Time.deltaTime * speed;
            if (m_aniTimer >= 0.1f)
            {
                int oldIndex = IsAniIndex;

                int maxFrames = (IsMoveType == MoveType.TYPE.Walk) ? 4 : 2;
                IsAniIndex = (IsAniIndex + 1) % maxFrames;

                m_aniTimer -= 0.1f;

                if (IsMoveType == MoveType.TYPE.Attack && IsAniIndex == 1 && oldIndex == 0)
                {
                    ExecuteAttack();
                }
            }
        }
        else
        {
            // Reset for Idle, etc.
            IsAniIndex = 0;
            m_aniTimer = 0f;
        }
    }

    void UpdateAttackTarget()
    {
        if (IsData == null || IsData.IsEntityType == EntityType.TYPE.Mob || IsData.IsEntityType == EntityType.TYPE.Boss) return;

        if (IsMoveType == MoveType.TYPE.Walk) return;

        var mobMgr = GManager.Instance != null ? GManager.Instance.IsMob : null;
        if (mobMgr == null) return;

        MobController closestMob = null;
        float searchRange = IsData.IsSearchLength;
        float minSqrDistance = searchRange * searchRange;

        foreach (var mob in mobMgr.ActiveMobs)
        {
            if (mob == null) continue;
            float sqrDist = (transform.position - mob.transform.position).sqrMagnitude;
            if (sqrDist < minSqrDistance)
            {
                minSqrDistance = sqrDist;
                closestMob = mob;
            }
        }

        if (closestMob != null)
        {
            m_targetMob = closestMob;
            IsMoveType = MoveType.TYPE.Attack;
            IsDirType = GetDirFromVector(m_targetMob.transform.position - transform.position);
        }
        else
        {
            m_targetMob = null;
            if (IsMoveType == MoveType.TYPE.Attack) IsMoveType = MoveType.TYPE.Idle;
        }
    }

    void ExecuteAttack()
    {
        if (m_targetMob == null) return;

        IsDirType = GetDirFromVector(m_targetMob.transform.position - transform.position);

        int baseDamage = IsData != null ? IsData.IsDamage : 10;
        int level = (GManager.Instance != null && GManager.Instance.IsUpgrade != null) ? GManager.Instance.IsUpgrade.GetClassLevel(IsData.IsEntityType) : 0;
        
        float multiplier = GetDamageMultiplier(IsData.IsEntityType, m_targetMob.IsData.IsSpeciesType);
        int finalDamage = Mathf.RoundToInt(baseDamage * (1f + (level - 1) * 0.1f) * multiplier);

        m_targetMob.TakeDamage(finalDamage);

        if (IsData != null)
        {
            if (m_atkEffectObj == null)
            {
                m_atkEffectObj = IsData.CreateEffect(IsAniIndex, transform);
            }
            else
            {
                m_atkEffectObj.SetActive(false);
                m_atkEffectObj.SetActive(true);
            }
        }
    }

    float GetDamageMultiplier(EntityType.TYPE attacker, SpeciesType.TYPE target)
    {
        if (target == SpeciesType.TYPE.None) return 1.0f;

        switch (attacker)
        {
            case EntityType.TYPE.Wizard:
                if (target == SpeciesType.TYPE.Undead) return 1.0f;
                if (target == SpeciesType.TYPE.Orc) return 0.8f;
                if (target == SpeciesType.TYPE.Troll) return 0.6f;
                break;
            case EntityType.TYPE.Archer:
                if (target == SpeciesType.TYPE.Orc) return 1.0f;
                if (target == SpeciesType.TYPE.Undead) return 0.8f;
                if (target == SpeciesType.TYPE.Troll) return 0.8f;
                break;
            case EntityType.TYPE.Warrior:
                if (target == SpeciesType.TYPE.Troll) return 1.0f;
                if (target == SpeciesType.TYPE.Orc) return 0.8f;
                if (target == SpeciesType.TYPE.Undead) return 0.6f;
                break;
        }
        return 1.0f;
    }

    public virtual void TakeDamage(int damage)
    {
        m_nowHp -= damage;

        ShowDamageText(damage);

        if (m_nowHp <= 0)
        {
            m_nowHp = 0;
            OnDie();
        }
    }

    protected virtual void OnDie()
    {
        bool isMobOrBoss = IsData != null && (IsData.IsEntityType == EntityType.TYPE.Mob || IsData.IsEntityType == EntityType.TYPE.Boss);
        if (isMobOrBoss)
        {
            if (GManager.Instance != null && GManager.Instance.IsMob != null)
            {
                GManager.Instance.IsMob.OnMobDestroyed(true);
            }
        }

        if (GManager.Instance != null && GManager.Instance.IsPool != null)
        {
            if (isMobOrBoss)
            {
                // If this was the active boss, notify MobManager so it can clear its reference
                if (IsData != null && IsData.IsEntityType == EntityType.TYPE.Boss && GManager.Instance != null && GManager.Instance.IsMob != null)
                {
                    GManager.Instance.IsMob.NotifyBossDefeated(GetComponent<MobController>());
                }
                GManager.Instance.IsPool.ReturnMob(gameObject);
            }
            else
            {
                GManager.Instance.IsPool.ReturnUnit(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected void ShowDamageText(int damage)
    {
        GameObject go = null;
        if (GManager.Instance != null && GManager.Instance.IsPool != null)
        {
            var dtParent = GManager.Instance.DamageTextParent != null ? GManager.Instance.DamageTextParent : transform.parent;
            go = GManager.Instance.IsPool.GetDamageText(dtParent);
        }
        else
        {
            go = new GameObject("DamageText");
            go.AddComponent<DamageText>();
        }
        go.transform.position = transform.position + Vector3.up * 0.5f;
        DamageText dt = go.GetComponent<DamageText>();
        dt.Setup(damage);
    }

    protected DirType.TYPE GetDirFromVector(Vector3 v)
    {
        if (v.magnitude < 0.001f) return DirType.TYPE.Down;
        Vector2 d = new Vector2(v.x, v.y).normalized;
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) return d.x > 0 ? DirType.TYPE.Right : DirType.TYPE.Left;
        return d.y > 0 ? DirType.TYPE.Up : DirType.TYPE.Down;
    }

    MobController m_targetMob;
    int m_maxHp = 100;
    int m_nowHp = 100;

    public void SetMaxHp(int hp)
    {
        m_maxHp = hp;
        m_nowHp = hp;
    }

    void UpdateAnimationResolver()
    {
        if (IsSpResolver == null) return;
        string category = GetAnimationCategory();
        string label = GetAnimationLabel();

        // call SetCategoryAndLabel only when category or label changed
        if (category == m_lastCategory && label == m_lastLabel) return;
        m_lastCategory = category;
        m_lastLabel = label;
        try
        {
            IsSpResolver.SetCategoryAndLabel(category, label);
        }
        catch { }
    }

    string GetAnimationCategory()
    {
        // categories follow pattern: IdleDown, WalkLeft, AttackUp etc. Use IsMoveType and IsDirType
        string move = "Idle";
        switch (IsMoveType)
        {
            case MoveType.TYPE.Walk:
                move = "Walk";
                break;
            case MoveType.TYPE.Attack:
                move = "Attack";
                break;
            default:
                move = "Idle";
                break;
        }
        string dir = IsDirType.ToString();
        return move + dir;
    }

    string GetAnimationLabel()
    {
        // Use IsAniIndex or rarity as label fallback
        return IsAniIndex.ToString();
    }

    // cached last applied category/label to avoid redundant Set calls
    string m_lastCategory = string.Empty;
    string m_lastLabel = string.Empty;
    float m_aniTimer = 0f;
    GameObject m_atkEffectObj = null;

    /// <summary>
    /// Sets localScale so that the object's world scale equals the given desiredWorldScale.
    /// </summary>
    public void SetWorldScale(Vector3 desiredWorldScale)
    {
        var parent = transform.parent;
        if (parent == null)
        {
            transform.localScale = desiredWorldScale;
            return;
        }

        Vector3 pLossy = parent.lossyScale;
        transform.localScale = new Vector3(SafeDivide(desiredWorldScale.x, pLossy.x), SafeDivide(desiredWorldScale.y, pLossy.y), SafeDivide(desiredWorldScale.z, pLossy.z));
    }

    static float SafeDivide(float a, float b)
    {
        if (Mathf.Approximately(b, 0f)) return a;
        return a / b;
    }
}
