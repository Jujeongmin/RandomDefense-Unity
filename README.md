# 랜덤 디펜스 (Random Defense)

세로 화면 모바일 **랜덤 타워 디펜스** 게임. 버튼 하나로 유닛을 랜덤 소환하고, 팔고, 강화하며 몰려오는 웨이브를 막아냅니다.

> Unity 2D (URP) · Android · 한국어/English

---

## 게임 소개

- **랜덤 소환**: 마법사·궁수·전사 3직업 × 6등급(일반~태초) 랜덤 뽑기. 태초는 0.1%!
- **종족 상성**: 궁수→오크, 마법사→언데드, 전사→트롤에 강함. 웨이브마다 종족이 바뀌므로 배치 전략이 필요
- **지역 시스템**: 4개 지역에 직업별 부대 배치, 드래그로 지역 간 스왑
- **경제 관리**: 소환할수록 비용 증가, 낮은 등급 판매(고급 이하 자동판매 토글 지원)
- **보스전**: 10웨이브마다 제한시간 보스. 못 잡으면 게임 오버
- **메타 성장(연구소)**: 판에서 얻은 크리스탈로 공격력/골드/소환 확률 영구 강화
- **모드**: 일반(50웨이브 클리어) / 무한(글로벌 리더보드 경쟁)
- **확률 투명 공개**: 게임 내 [확률 정보] 화면에서 실시간 소환 확률 확인 가능

### 소환 확률 (기본)

| 일반 | 고급 | 정예 | 전설 | 신화 | 태초 |
|---|---|---|---|---|---|
| 50% | 33% | 10% | 6.5% | 0.4% | 0.1% |

※ 희귀 소환 연구 시 일반↓ 고급↑ (게임 내 확률 정보 화면에 실시간 반영)

---

## 기술 스택 / 연동

| 영역 | 사용 기술 |
|---|---|
| 엔진 | Unity 6 (2D URP), IL2CPP, ARMv7+ARM64 |
| 광고 | Google AdMob (배너·보상형) + UMP 동의(GDPR) |
| 결제 | Unity IAP → Google Play Billing (크리스탈 패키지, 광고 제거) |
| 랭킹 | Google Play Games Services 리더보드 (무한모드, `GPGS_ENABLED` 심볼) |
| 폰트 | Paperlogy (한글), TextMesh Pro |
| 사운드 | Kenney 오디오 팩 (Interface / RPG / Impact) |

---

## 프로젝트 구조

```
Assets/
├─ Scenes/            MainScene(메인메뉴) · GameScene(전투)
├─ Scripts/
│  ├─ Manager/        GManager(전역) · MobManager(웨이브) · EconomyManager ·
│  │                  UpgradeManager · RegionManager · UnitSpawner …
│  ├─ Controller/     ParentsController(유닛 공통) · MobController · 직업별 컨트롤러
│  ├─ Data/           GameBalanceData(밸런스 SO) · EntityData · GameModeSettings
│  ├─ UI/             SellPanelUI · RarityOddsPanel(확률표기) · MobHealthBar …
│  ├─ Ads/            AdMob 배너/보상형 · UMP 동의 · IAP · 리더보드
│  ├─ Audio/          GameAudioManager · GameAudioLibrary(SO)
│  └─ Editor/         빌드용 에디터 툴 (Tools > Random Defense)
├─ GData/             유닛/몹 데이터 에셋 · 폰트 · 오디오
└─ Resources/         GameAudioLibrary.asset
docs/                 개인정보처리방침 · 스토어 등록 문구 초안
```

### 밸런스 조정
수치는 코드 수정 없이 **`Assets/GData/Data/GameBalanceData.asset`** 인스펙터에서 조절합니다
(소환 비용/증가폭, 몹·보스 체력 곡선, 웨이브 설정, 판매가, 연구 비용 등).

### 에디터 툴 (Tools > Random Defense)
| 메뉴 | 용도 |
|---|---|
| Build Audio Library | 사운드 클립을 오디오 라이브러리에 연결 (클립 교체 후 실행) |
| Apply Shop Prices | 상점 표시 가격 일괄 적용 (`Editor/ShopPrices.cs`에서 가격 수정) |
| Place Main Menu Extra UI | 메인메뉴 모드/확률/랭킹 버튼 재배치 ⚠️ 수동 배치 후엔 실행 금지 |

---

## 빌드

1. 플랫폼: **Android** (File > Build Settings)
2. 테스트: APK + **Development Build** 체크 → 테스트 광고 송출
3. 출시: **Build App Bundle(.aab)** 체크 + Development Build 해제 → 실광고
4. 서명: Publishing Settings에 키스토어 등록 필요 (저장소에 포함되지 않음)

### 출시 전 체크
- [ ] `LeaderboardService.cs`의 리더보드 ID를 Play Console 발급값으로 교체
- [ ] Play Console: 인앱 상품 5종 등록 (`crystal_500/1200/3000/6500`, `remove_ads`)
- [ ] AdMob 콘솔: GDPR 동의 메시지 생성
- [ ] 개인정보처리방침 URL 등록 (`docs/privacy-policy.md` 초안 참고)
- [ ] 앱 아이콘 / 스토어 그래픽 (`docs/store-listing.md` 참고)

---

## 클론 시 주의

`Assets/Down/` 폴더(서드파티 에셋 팩)는 라이선스상 저장소에 포함되지 않습니다.
클론 후 아래 에셋을 별도로 임포트해야 프로젝트가 정상 동작합니다:

- Cainos — Pixel Art Top Down Basic
- JMO Assets — Cartoon FX Remaster (Free)
- Clean Vector Icons
- Universal Stylized UI

---

## 개발

- 개발: **JunJa/JM**
- 문의: anjshdkdl99@gmail.com
