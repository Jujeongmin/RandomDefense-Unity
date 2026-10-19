# 디펜스 배틀 (Defense Battle) - Unity

> 소환은 운, 승리는 판단.

유닛을 소환·합성해 몰려오는 몹을 막고, 대전에서는 몹 카드를 상대 전장에 던져 무너뜨리는 1:1 실시간 디펜스 대전 게임.
웹판 [디펜스 배틀](https://github.com/Jujeongmin/DefenseBattle)(Verse8)의 기획을 Unity(Android)로 옮겨 **새 앱으로** 출시하기 위한 저장소다.

> **지금 상태:** 게임 규칙(엔진·데이터·봇·경제)과 서버(UGS Cloud Code)는 이식·검증 완료. **화면(씬)은 아직 작업 전**이라
> `Assets/Scenes` 의 MainScene/GameScene 은 예전 '운빨 랜덤 디펜스' 그대로다. 진행 계획은
> [`docs/superpowers/specs/2026-10-19-defense-battle-port-design.md`](docs/superpowers/specs/2026-10-19-defense-battle-port-design.md).

| 항목 | 값 |
| --- | --- |
| 앱 | 디펜스 배틀 · `com.jeonja.defensebattle` · 1.0 (1) · Android API 25 ~ 36 |
| 엔진 | Unity 6000.4.0f1, 2D URP, IL2CPP, 세로 화면 |
| 서버 | Unity Gaming Services - Authentication(Google Play), Cloud Save, Cloud Code(C# 모듈), Leaderboards, Economy(영수증 검증), Relay |
| 수익화 | Google Play 결제(원화), AdMob 보상형 광고 3곳 (전면 광고 없음) |
| 언어 | 한국어 · English · 日本語 · 简体中文 · 繁體中文 |

## 게임

- **소환 → 합성 → 강화 → 배치.** 버튼 하나로 랜덤 소환, 같은 유닛·등급 3개를 합치면 한 등급 위로 (일반 → 태초 6등급).
- 사각 링 길을 도는 몹이 **100마리** 쌓이면 패배. 유닛은 자기 구역 앞만 지켜서, 닿지 않는 길(빨간 구멍)은 구역을 바꿔 메운다.
- **유닛 32종**(일반 8 · 희귀 10 · 영웅 8 · 전설 6), 직업마다 스킬이 다르고 상자·뽑기 카드로 레벨업.
- **PvP 실시간 1:1:** 같은 웨이브를 동시에 막으며 **몹 카드 16종** 중 4장을 상대에게 던진다. 트로피 → 6단계 아레나, 시즌 패스, 랭킹. 상대가 없으면 봇.
- **캠페인** 3챕터 × 10스테이지, **무한 모드** 50웨이브.

## 진행 상황

| 단계 | 내용 | 상태 |
| --- | --- | --- |
| 0 | 세팅 - 앱 정보, 에셋(픽셀아트·사운드·폰트) 복사·임포트 자동 설정, Unity MCP | 대부분 완료 (UGS 대시보드는 사용자 작업) |
| 1 | 게임 규칙 순수 C# 이식 (`Assets/Scripts/Core`) | **완료** - 테스트 61개, 원본 시뮬레이터와 밸런스 일치 |
| 2 | 전투 화면 (Battle 씬) | 다음 작업 |
| 3 | 로비·메타 (Boot / Lobby 씬, 덱·상자·상점 …) | |
| 4 | 봇 대전 화면 | |
| 5 | UGS 연동 | 서버 **완료** (`CloudCode/`, 테스트 33개) · 앱 쪽 연결 남음 |
| 6 | 실시간 PvP (Relay) | |
| 7 | 출시 준비 | |

## 구조

```
Assets/
├─ Scripts/Core/          게임 규칙 (DefenseBattle.Core 어셈블리, UnityEngine 참조 없음)
│  ├─ Config/             데이터 표: 유닛·등급·스킬·몹 카드·웨이브·캠페인·연구·상자·상점(원화)·시즌 패스·트로피 로드
│  ├─ Game/               엔진 Game (웨이브·전투·스킬 32종·합성·PvP 스냅샷), 몹·이펙트 오브젝트 풀링
│  ├─ Ai/                 봇 AI · 봇 대전 진행
│  ├─ Econ/               경제 규칙 (서버와 같은 코드)
│  └─ Net/                서버 함수 이름·주고받는 데이터 형식
├─ Tests/EditMode/        Core EditMode 테스트 (Unity Test Runner 와 .NET 에서 같이 돈다)
├─ Art/DefenseBattle/     유닛·몹 시트, 맵 타일, UI 프레임·아이콘·상점 그림
├─ Audio/DefenseBattle/   효과음 16 · BGM 4
├─ Fonts/DefenseBattle/   Galmuri11 (한·영), Fusion Pixel (일·중)
├─ CloudCode/             Cloud Code 모듈 배포 설정 (.ccmr)
├─ Scripts/Editor/        DefenseBattleArtImporter (픽셀아트 임포트 설정) 외 빌드 도구
└─ Scripts/{Manager,UI,…} 예전 '운빨 랜덤 디펜스' 코드 - 새 씬이 동작하면 정리
CloudCode/                UGS Cloud Code C# 모듈 (서버 로직 Logic + UGS 어댑터 Ugs + 테스트)
CoreTests/                Unity 없이 Core 를 컴파일·테스트·시뮬레이션하는 .NET 프로젝트
docs/                     설계·세팅 문서
```

## 개발

에디터 작업은 PC 의 Claude Code + Unity MCP 로 한다 ([`docs/unity-mcp-setup.md`](docs/unity-mcp-setup.md)). 작업 규칙은 [`CLAUDE.md`](CLAUDE.md).

Unity 없이 돌리는 검증 (.NET 8 SDK):

```bash
dotnet test CoreTests/Tests                                  # 게임 규칙 테스트
dotnet test CloudCode/Tests                                  # 서버 로직 테스트
dotnet run -c Release --project CoreTests/Sim -- 30          # 무한 모드 밸런스 (원본 simulate.ts)
dotnet run -c Release --project CoreTests/Sim -- stages 10   # 캠페인 클리어율
dotnet run -c Release --project CoreTests/Sim -- pvp 30      # PvP 승률·판 길이 (원본 pvpsim.ts)
dotnet run -c Release --project CoreTests/Sim -- bench 3 0   # 전투 1프레임 시간·할당량
dotnet run -c Release --project CoreTests/Sim -- prices      # 상점 가격·효율
```

- 밸런스: 원본과 같은 자동 플레이어로 비교 (무한 모드 연구 0 → MAX 평균 도달 웨이브 원본 39 / 44.5 / 46.6 / 49.3 / 50, 이식판 39.1 / 44.5 / 46.3 / 49.4 / 49.9).
- 성능: 전투 1프레임 약 4~6µs, 프레임당 할당 사실상 0 (몹·이펙트 풀링).
- 서버·스토어·광고 대시보드 세팅 순서와 원화 가격표: [`docs/ugs-setup.md`](docs/ugs-setup.md).
- Google Play 게임즈·AdMob ID(`Assets/GPGSIds.cs`, `ProjectSettings/GooglePlayGameSettings.txt`, `Assets/Scripts/Ads/*`)는 아직 예전 앱 값이다 - 새 앱을 등록한 뒤 바꾼다.

## 예전 앱 (운빨 랜덤 디펜스)

이 저장소는 원래 '운빨 랜덤 디펜스'(`com.junja.randomdefense`)였다. 이식 직전 상태는 보관 브랜치로 남아 있다.

- `archive/random-defense-v2.0` - 출시본 (버전 2.0, 코드 3)
- `archive/random-defense-v2.1` - 리텐션 업데이트 (출석·도감·튜토리얼, 코드 4)

예전 앱을 고쳐야 하면 보관 브랜치에서 새 브랜치를 만들어 작업한다 (보관 브랜치 자체는 그대로 둔다). 예전 씬은 서드파티 팩(`Assets/Down/` - Cainos Pixel Art Top Down Basic, JMO Cartoon FX Remaster, Clean Vector Icons, Universal Stylized UI)을 따로 임포트해야 열린다 (라이선스상 저장소에 없음).

## 에셋 출처

| 에셋 | 사용처 | 라이선스 |
| --- | --- | --- |
| [Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords) (Pixel Frog) | 맵 지형·장식, UI 버튼·종이·나무·리본 | 구버전 CC0 / Free Pack: 상업 이용 가능, 원본 팩 재배포 금지 |
| [Kenney 1-Bit Pack](https://kenney.nl/assets/1-bit-pack) | UI 아이콘 (색 입혀 가공) | CC0 |
| [Galmuri](https://github.com/quiple/galmuri) (Lee Minseo) | 한국어·영어 픽셀 폰트 | SIL OFL 1.1 |
| [Fusion Pixel Font](https://github.com/TakWolf/fusion-pixel-font) | 일본어·중국어 픽셀 폰트 | SIL OFL 1.1 |
| [ぴぽや Pipoya](https://pipoya.net/) 32x32 파츠 (CharaMEL) | 유닛·몹 캐릭터 | 상업 이용·수정 가능, 소재 재배포 금지 |
| [Mixkit](https://mixkit.co/) | 효과음, BGM | Mixkit Free License: 상업 이용 가능, 원본 단독 재판매 금지 |

가공본만 저장소에 넣고 원본 팩은 넣지 않는다. 폰트 라이선스 원문은 `Assets/Fonts/DefenseBattle/`.

## 개발자

- JeonJa
- 문의: anjshdkdl99@gmail.com
