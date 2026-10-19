# 디펜스 배틀 Unity 이식 설계 (2026-10-19)

RandomDefense-Unity 를 웹판 **디펜스 배틀**(`../DefenseBattle`, TypeScript + Vite + Verse8 서버) 기획으로 최신화한다.
디펜스 배틀은 이 Unity 판에서 출발해 기획을 발전시킨 버전이므로, 이번 작업은 "웹판의 현재 기획 전체를 Unity로 가져오기"다.

## 확정된 결정 (사용자 승인)

| 항목 | 결정 |
| --- | --- |
| 범위 | **전부**: 전투 코어 + 캠페인/무한 + 유닛 32종 컬렉션 + 몹 카드 16종 + 상자·뽑기·트로피 로드·퀘스트·시즌 패스·상점 + **실시간 1:1 PvP** |
| 서버 | **Unity Gaming Services (UGS)** — 호스팅 없이 Authentication · Cloud Save · Cloud Code · Leaderboards · Multiplayer(Matchmaker + Relay) |
| 에셋 | **디펜스 배틀 픽셀아트로 교체** (Pipoya 유닛/몹, Tiny Swords 맵·UI, Kenney 아이콘, Galmuri/Fusion Pixel 폰트, Mixkit 사운드) |
| 앱 | **새 앱으로 출시** — 새 패키지명, 기존 '운빨 랜덤 디펜스' 저장 호환 불필요 |
| 작업 방식 | 사용자 PC에서 **Claude Code + Unity MCP** (`docs/unity-mcp-setup.md`) |

### 아직 정할 것 (0단계에서 사용자에게 확인)
- 새 패키지명 (제안: `com.junja.defensebattle`) · 앱 이름 표기 (제안: 디펜스 배틀 / Defense Battle)
- UGS 프로젝트 생성·연결 (Unity Cloud 대시보드, 사용자 계정 필요)
- 새 AdMob 앱/광고 단위 ID, Play Console 새 앱과 인앱 상품 등록
- 결제 가격: 웹판 VX 가격(100 VX = $1)을 Play 가격대로 그대로 옮길지

## 원칙

1. **원본이 기준이다.** 수치·규칙·문구는 `../DefenseBattle/game/src` 를 그대로 옮긴다. 밸런스를 새로 만들지 않는다.
   원본에 근거한 숫자는 주석에 원본 위치를 남긴다 (예: `// config.ts ECONOMY`).
2. **게임 규칙은 순수 C#.** 엔진·데이터·경제 규칙·봇 AI는 `UnityEngine` 을 참조하지 않는 어셈블리(`DefenseBattle.Core`,
   asmdef `noEngineReferences: true`)에 둔다. 원본 `engine.ts` 가 DOM 없이 돌아가는 구조 그대로이고,
   EditMode 테스트와 헤드리스 시뮬레이터로 검증할 수 있다.
3. **Unity 쪽은 보여 주기만.** MonoBehaviour 는 Core 의 상태를 읽어 그리고(스프라이트·투사체·이펙트·HUD),
   입력을 Core 명령(`Summon`, `Merge`, `SwapZones`, `SendAttack` …)으로 넘긴다.
4. **경제는 서버 권위.** 원본처럼 보석·카드·상자·결제·대전 보상은 서버(Cloud Code)가 계산하고 저장한다.
   규칙은 `econ/rules.ts` 하나를 Cloud Code(JS)와 C#(오프라인/표시용)이 같이 쓰는 구조를 유지한다.
5. **단계마다 에디터에서 확인.** 각 단계 끝에 MCP로 콘솔 에러 0개 + 플레이 모드에서 완료 기준 확인 후 커밋.

## 원본 → Unity 대응표

### 게임 로직 (`Assets/Scripts/Core/`, 순수 C#)

| 원본 (DefenseBattle/game/src) | Unity | 비고 |
| --- | --- | --- |
| `types.ts` | `Core/Types.cs` | Job·Grade·Race·Unit·Mob·Zone·GameOptions 등 |
| `config.ts` | `Core/Config/*.cs` (Units, Grades, Skills, Economy, Wave, Stages, Attacks, Pvp, Research, Products, SeasonPass, TrophyRoad …) | 정적 테이블. 인스펙터 조정이 필요하면 나중에 SO로 감싼다 |
| `config.ts makeRng` | `Core/Rng.cs` | mulberry32 를 **비트 단위로 똑같이** (uint 연산). PvP 양쪽 웨이브 구성이 같아야 함 |
| `engine.ts` (`Game`) | `Core/Game.cs` (+ `Game.Combat.cs`, `Game.Waves.cs`, `Game.Pvp.cs` partial) | 사각 링 경로(`PATH`, `pathAt`), 구역 4개(`SEG_ZONE`), 사거리·빨간 구멍(`pathGaps`), 소환·3합성·강화·판매, 스킬 32종, 몹 종류·카드 몹 특성, 보스, 부활, 스냅샷 |
| `ai.ts` | `Core/BotPlayer.cs` | 봇 실력 0.05~0.85, 덱·카드 선택, 약한 구역 노리기 |
| `econ/rules.ts` | `Core/Econ/Rules.cs` + Cloud Code JS 모듈 (원본 그대로 번들) | 둘의 결과가 같아야 함 → 테스트 |
| `meta.ts`, `progress.ts` | `Core/Meta/*.cs` | 저장 데이터 구조, 캠페인 진행·해금·덱 |
| `chests.ts`, `offers.ts`, `shop.ts` | `Core/Meta/Chests.cs`, `Offers.cs`, `Shop.cs` | 표시 로직은 UI 쪽 |
| `tools/simulate.ts`, `pvpsim.ts` | `Tests/EditMode/BalanceSimTests.cs` | 원본 시뮬 결과와 평균 도달 웨이브·클리어율 비교 |

원본 엔진은 `Math.random` 을 22곳에서 쓰므로 판 단위 결과가 원본과 똑같을 수는 없다.
**웨이브 구성(시드 RNG)은 비트 단위로 일치**, 나머지는 **시뮬레이터 통계로 일치**(원본 `DEV_NOTES.md` 의 밸런스 수치 ±오차)를 기준으로 한다.

### 화면 (`Assets/Scripts/View/`, `Assets/Scripts/UI/`)

| 원본 | Unity |
| --- | --- |
| `render.ts` (캔버스 720×720 전장, 투사체·파티클·흔들림·보스바) | `View/BoardView.cs` 등. 월드 좌표 = 원본 픽셀 / PPU, Pixel Perfect Camera, 스프라이트 풀링 |
| `oppRender.ts` (상대 전장 미니맵) | `View/OpponentBoardView.cs` (RenderTexture 또는 축소 루트), 길게 눌러 크게 보기 |
| `assets.ts` (96×128 RPG Maker 시트, 3열×4행) | `Editor/PixelArtImporter.cs` (AssetPostprocessor: Point 필터·무압축·PPU·시트 자동 슬라이스) |
| `ui.ts` (전투 HUD·영웅 관리·PvP HUD·결과·튜토리얼) | uGUI + TextMeshPro, Tiny Swords 9-slice 프레임 |
| `menu.ts` (로비·덱·연구·퀘스트·로드·랭킹·상점·시즌패스) | 같은 구성의 uGUI 패널 |
| `dragdeck.ts`, `tooltip.ts` | 덱 드래그 편성, 말풍선 |
| `tutorial.ts` | 1-1 단계별 튜토리얼 |
| `audio.ts` | 기존 `GameAudioManager`/`GameAudioLibrary` 재사용, 클립만 Mixkit 으로 교체, BGM 4트랙 |
| `i18n.ts` + `i18n/*.ts` (5개 언어) | `Localization/` — 한국어 원문 키 그대로 유지, 원본 번역 표를 변환 스크립트로 JSON/CSV 화. 폰트: Galmuri11(ko/en), Fusion Pixel(ja/zh) TTF → TMP Font Asset |

### 플랫폼 / 서버 (Verse8 → UGS)

| 원본 (`server/src/server.ts`, `platform.ts`, `ads.ts`) | UGS / Unity |
| --- | --- |
| Verse8 계정 | **Authentication** (익명 → Google Play Games 연동) |
| `loadProfile` / `saveProfile` | **Cloud Save** (플레이어 데이터). 경제 필드는 클라가 못 씀 |
| `econ`, `econSync`, `runStart`/`runEnd`, `claimPurchases` | **Cloud Code** 모듈 (JS). `econ/rules.ts` 재사용. 서버 전용 상태는 Cloud Save 의 서버 쓰기 전용 키(Protected/Private data) |
| `$onItemPurchased` (VX Shop) | Unity IAP(Google Play Billing) 구매 → 영수증을 Cloud Code 로 보내 검증·지급 (purchaseId 1회) |
| `pvpQueue`/`pvpPoll`/`pvpCancel` | **Matchmaker** (트로피 근접 규칙, 대기 시간에 따라 폭 확대) → **Relay** 세션 (`com.unity.services.multiplayer`) |
| `pvpReady`/`pvpSnap`/`pvpAttack`/`pvpDefeat` | Relay 위 메시지 (Netcode for GameObjects 의 Named Message 또는 Unity Transport 직접). 스냅샷 0.25초 주기·~2.5KB, 공격·패배 이벤트 |
| `finishMatch`, `onRoomLeave`, `pvpResume`/`pvpClaimAway` | Cloud Code `pvpReport` — 양쪽 결과 보고·이탈 처리 후 트로피·보상 지급 |
| `pvpBotStart` / `pvpBotResult` | Cloud Code 같은 이름 함수 (봇전도 서버가 트로피 관리) |
| `pvpLeaderboard`, 무한 모드 랭킹 | **Leaderboards** (트로피, 무한 모드 최고 웨이브). 점수 제출은 Cloud Code 에서만 |
| `setNick` + `nickFilter.ts` | Cloud Code 에서 금칙어 검사 후 Authentication 플레이어 이름 설정 |
| `@verse8/ads` (보상형 3곳) | 기존 `AdMobRewardedAdService` 재사용. 플레이스먼트 `reward_double` / `reward_revive` / `reward_ticket`. 전면 광고 없음 |
| VX Shop 상품 (README 표) | Play Console 인앱 상품. productId 는 원본 이름 유지 (`starter_pack`, `crystal_100` …) |

PvP 동기화 모델은 원본과 같다: **각 클라이언트가 자기 전장만 시뮬레이션**하고, 상대에게는 스냅샷·공격만 보낸다.
같은 시드로 웨이브 구성이 같다. Relay 는 판정을 하지 않으므로, 결과 판정과 보상은 Cloud Code 가 양쪽 보고를 맞춰 본 뒤 한다.

## 기존 Unity 코드 처리

| 기존 | 처리 |
| --- | --- |
| `Manager/*` (GManager, MobManager, RegionManager, UnitSpawner, UpgradeManager, EconomyManager …), `Controller/*` | Core + View 로 대체 후 삭제 |
| `Data/EntityData`, `GameBalanceData` 및 `GData/Data/*.asset` | Core 테이블로 대체 후 삭제 |
| 3직업(마법사·궁수·전사) 고정 구역, 판매 위주 진행, 크리스탈 연구 | 원본 규칙(덱 4슬롯, 3합성, 연구 5종)으로 대체 |
| `Ads/*` (AdMob, UMP 동의, IAP, Play Store) | **재사용**. ID·상품만 새 앱 값으로 |
| `Ads/LeaderboardService` (GPGS 리더보드) | UGS Leaderboards 로 대체 (GPGS 는 로그인용으로만) |
| `Audio/*` | 재사용, 클립 교체 |
| `UI/MobileSafeAreaLayout`, `DamageText`, `GameFont` | 재사용 가능하면 유지 |
| `Editor/ReleaseBuilder`, `FixAdaptiveIcon` | 유지 (패키지명·아이콘만 갱신) |
| `Assets/Down/` 서드파티 팩 의존 | 픽셀아트 교체 후 의존 제거 → 클론만으로 열리는 프로젝트가 목표 |

삭제는 대체물이 플레이 모드에서 동작을 확인한 뒤에 한다.

## 진행 상황

| 단계 | 상태 |
| --- | --- |
| 0. 세팅 | 일부 - MCP 연결 완료. 패키지명·UGS·에셋 복사는 남음 |
| 1. Core 이식 | **완료 (2026-10-19)** - 아래 검증 결과 |
| 2 ~ 7 | 시작 전 |

### 1단계 결과 (`Assets/Scripts/Core`, 순수 C#, `DefenseBattle.Core` 어셈블리)
- 옮긴 것: 데이터 표 전부(유닛 32·등급·스킬 32·몹 카드 16·웨이브·캠페인 30·연구·상자·뽑기·상점·시즌 패스·아레나·트로피 로드·퀘스트·업적), 엔진(`Game`), 봇 AI(`BotPlayer`·`BotProfile`·`BotMatch`), 경제 규칙(`EconRules`).
- 원본과 다르게 한 것: 판 안 무작위도 시드(`GameOptions.RandomSeed`)를 줄 수 있다(테스트·재현용). 상자 카드 분배·봇 덱 섞기는 원본의 치우친 `sort(random)` 대신 고르게 섞는다. 엔진은 메타(저장본)를 직접 만지지 않고 `Progress`·`AchievementReached` 이벤트를 낸다.
- 검증: EditMode 테스트 60개 통과(.NET 8). RNG·웨이브 종족 순서·스테이지 체력 곡선·길 좌표는 원본을 실행해 뽑은 값과 비트 단위 일치.
  밸런스 시뮬(각 100판, 연구 0 → MAX 평균 도달 웨이브): 원본 39 / 44.5 / 46.6 / 49.3 / 50, C# 39.1 / 44.5 / 46.3 / 49.4 / 49.9. 캠페인 30스테이지 클리어율도 같은 결과(거의 전부 100%). PvP 한 판 길이 2.4~4.2분으로 원본과 같음.
- 최적화: 몹·이펙트·불 장판 오브젝트 풀링, 전투 루프의 LINQ·임시 컬렉션·람다 캡처 제거, 표적 탐색·범위 판정을 제곱 거리로, 길 표본·공격 카드 목록 미리 계산.
  결과: 전투 1프레임 약 4~6µs, 할당 프레임당 0.2바이트(사실상 0). 최적화 전후 시드 고정 40판 결과가 완전히 같음(`Sim fingerprint`).
- Unity 확인 필요(PC): 에디터에서 컴파일 에러 0, Package Manager 에 Test Framework 추가 후 Test Runner 에서 EditMode 테스트 통과.

## 단계

각 단계는 "완료 기준"을 에디터에서 확인하고 커밋한다. 단계 안에서도 작게 나눠 자주 커밋한다.

### 0. 세팅
- 위 "아직 정할 것" 확인. Player Settings: 새 패키지명·제품명, 버전 1.0 / 버전코드 1, 아이콘은 원본 `launch/` 키아트에서.
- 패키지 추가: `com.unity.services.authentication`, `com.unity.services.cloudsave`, `com.unity.services.cloudcode`,
  `com.unity.services.leaderboards`, `com.unity.services.multiplayer`, `com.unity.netcode.gameobjects`, `com.unity.test-framework`.
- 에셋 복사: `../DefenseBattle/game/public/{sprites,tiles,ui,sfx,bgm}` → `Assets/Art/…`, `Assets/Audio/…`.
  폰트는 woff2 라서 Galmuri·Fusion Pixel 원본 저장소 릴리스의 TTF 를 받는다 (OFL 라이선스 파일 같이).
- `Editor/PixelArtImporter.cs` 로 임포트 설정 자동화.
- **완료 기준:** 콘솔 에러 0, 스프라이트가 번지지 않게 보임, 빈 씬에서 UGS 익명 로그인 성공 로그.

### 1. Core 이식 (순수 C#)
- Types → Config → Rng → Game(웨이브·경로·몹) → 유닛·소환·합성·강화·판매 → 스킬 32종 → 몹 카드 16종·PvP 송수신·스냅샷 → BotPlayer → Econ Rules.
- EditMode 테스트: Rng 값이 원본과 일치(원본에서 `npx tsx` 로 뽑은 기대값), 3합성, 사거리 구멍, 각 스킬, 카드 몹 특성.
- 시뮬레이터: 무한 모드 연구 0 → 평균 ~38.8웨이브, 캠페인 챕터1 100% 등 `DEV_NOTES.md` "밸런스" 수치 재현.
- **완료 기준:** EditMode 테스트 전부 통과, 시뮬 결과가 원본 수치와 오차 범위 안.

### 2. 전투 화면 (싱글)
- 전장 그리기(Tiny Swords 지형, 사각 링 길, 4구역), 유닛·몹 스프라이트 애니메이션·방향, 투사체·스킬 이펙트·데미지 숫자·화면 흔들림·보스바.
- HUD: 골드·웨이브·필드 몹 수(100 패배)·배속(기본 PLAY_SPEED 2), 소환 버튼, 영웅 관리(합성·강화·판매), 구역 드래그 교체, 빨간 구멍 표시.
- 결과 화면(별, 보상, 2배 광고), 부활 광고.
- **완료 기준:** 1-1 과 무한 모드를 처음부터 끝까지 플레이 가능, 콘솔 에러 0.

### 3. 메타 (오프라인 저장으로 먼저)
- 로비(아레나 카드·트로피 로드 바·시즌 패스·캠페인·무한, 하단 메뉴와 NEW 해금 순서 `FEATURE_UNLOCK`)
- 덱(유닛 4 + 몹 카드 4, 드래그 편성, 추천 덱), 유닛 카드 레벨업, 연구, 상자 슬롯·열기 연출, 뽑기, 일일 퀘스트, 업적, 트로피 로드, 시즌 패스, 상점·한정 특가 팝업, 설정(언어·사운드), 닉네임.
- 1-1 튜토리얼, 처음 3판 약한 봇, 인게임 1회성 팁.
- 5개 언어.
- **완료 기준:** 새 계정으로 튜토리얼 → 캠페인 → 덱 편성 → 상자 열기 → 레벨업까지 흐름이 막힘 없음.

### 4. 봇 대전 (PvP 화면)
- 대전 화면(상대 전장 미니맵·상대 카드/덱 표시·카드 끌어 던지기·포물선·경고 고리·타이머), 봇 상대, 트로피·아레나·상자 보상.
- **완료 기준:** 봇 대전 한 판 3~4분, 승패·트로피 반영.

### 5. UGS 연동
- Authentication(익명 + GPGS), Cloud Save, Cloud Code 경제 모듈(원본 `econ/rules.ts` 번들) + 원본 서버 테스트 14개를 Cloud Code 테스트로 이식.
- IAP 영수증 검증·지급, 광고 보상 횟수 제한(서버), 랭킹·닉네임.
- **완료 기준:** 저장값 조작이 반영되지 않음, 결제 중복 지급 없음, 기기 바꿔도 진행 유지.

### 6. 실시간 PvP
- Matchmaker(트로피 ±200, 대기할수록 확대, 8초 후 봇 전환) → Relay 세션 → 동시 시작 → 스냅샷/공격 중계 → 결과 보고 → Cloud Code 판정.
- 이탈·재접속(`pvpResume`), 백그라운드 처리.
- **완료 기준:** 에디터 + 기기(또는 ParrelSync/Multiplayer Play Mode 2인스턴스)로 한 판 완주, 이탈 시 상대 승리.

### 7. 출시 준비
- 새 AdMob/Play Console/IAP ID, 개인정보처리방침·스토어 문구(원본 `launch/store` 참고), 아이콘·스크린샷, 릴리스 AAB 빌드.
- README·이 문서 갱신, 기존 `Assets/Down/` 의존 제거 확인.

## 에셋 라이선스 메모

원본 README "에셋 출처" 그대로. 가공본만 커밋하고 원본 팩은 커밋하지 않는다.
Pipoya(소재 재배포 금지)·Tiny Swords Free Pack(원본 팩 재배포 금지)은 게임에 넣어 배포하는 것은 가능하다.
