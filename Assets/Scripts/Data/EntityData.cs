using UnityEngine;
using UnityEngine.U2D.Animation;

[CreateAssetMenu(fileName = "EntityData", menuName = "JM/EntityData", order = 1)]
public class EntityData : ScriptableObject
{
    /// <summary>
    /// 독립체 타입
    /// </summary>
    [SerializeField] EntityType.TYPE m_entityType = EntityType.TYPE.Wizard;

    /// <summary>
    /// 독립체 인덱스
    /// </summary>
    [SerializeField] int m_entityIndex = 0;

    /// <summary>
    ///  이미지 라이브러리
    /// </summary>
    [SerializeField] SpriteLibraryAsset m_spLibAsset = null;

    /// <summary>
    /// 애니메이션 스케일
    /// 1일경우 1초에 한 번 애니메이션 재생
    /// </summary>
    [SerializeField] float m_aniScale = 0.0f;

    /// <summary>
    /// 이동속도
    /// </summary>
    [SerializeField] float m_speed = 0.0f;

    /// <summary>
    /// 탐색 범위
    /// </summary>
    [SerializeField] float m_searchLength = 0.0f;

    /// <summary>
    /// 공격력
    /// x: 최소, y: 최대
    /// </summary>
    [SerializeField] Vector2Int m_atk = Vector2Int.zero;
    
    /// <summary>
    /// 체력
    /// </summary>
    [SerializeField] int m_hp = 100;
    
    /// <summary>
    /// 종족 타입
    /// </summary>
    [SerializeField] SpeciesType.TYPE m_speciesType = SpeciesType.TYPE.None;

    /// <summary>
    /// 이펙트
    /// </summary>
    [SerializeField] GameObject m_effect = null;

    /// <summary>
    /// 공격 시 몹 이동경로 전체에 터지는 추가 이펙트 (태초 등급 전용, 없으면 null)
    /// </summary>
    [SerializeField] GameObject m_pathEffect = null;

    /// <summary>
    /// UI용 아이콘
    /// </summary>
    [SerializeField] Sprite m_icon = null;

    /// <summary>
    /// 독립체 타입
    /// </summary>
    public EntityType.TYPE IsEntityType { get { return m_entityType; } }
    
    /// <summary>
    /// 종족 타입
    /// </summary>
    public SpeciesType.TYPE IsSpeciesType { get { return m_speciesType; } }

    /// <summary>
    /// UI용 아이콘
    /// </summary>
    public Sprite IsIcon => m_icon;

    /// <summary>
    /// 독립체 인덱스
    /// </summary>
    public int IsEntityIndex { get { return m_entityIndex; } }

    /// <summary>
    /// 스프라이트 라이브러리 에셋
    /// </summary>
    public SpriteLibraryAsset IsSpLibAsset { get { return m_spLibAsset; } }

    /// <summary>
    /// 애니메이션 스케일
    /// </summary>
    public float IsAniScale { get { return m_aniScale; } }

    /// <summary>
    /// 이동속도
    /// </summary>
    public float IsSpeed { get { return m_speed; } }

    /// <summary>
    /// 탐색 범위
    /// </summary>
    public float IsSearchLength { get { return m_searchLength; } }

    /// <summary>
    /// 데미지
    /// </summary>
    public int IsDamage { get { return Random.Range(m_atk.x, m_atk.y + 1); } }

    /// <summary>
    /// 최대 체력
    /// </summary>
    public int IsHp { get { return m_hp; } }

    /// <summary>
    /// 공격 이펙트(발사체) 프리팹 — EffectPool을 통해 생성/재사용
    /// </summary>
    public GameObject IsEffect => m_effect;

    /// <summary>
    /// 몹 이동경로 전체에 터지는 추가 이펙트 프리팹 (태초 등급 전용)
    /// </summary>
    public GameObject IsPathEffect => m_pathEffect;
}
