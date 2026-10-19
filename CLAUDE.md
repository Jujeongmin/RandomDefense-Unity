# CLAUDE.md

이 프로젝트는 웹판 **디펜스 배틀**(`../DefenseBattle`)의 기획을 Unity 로 이식해 새 앱으로 내는 중이다.
작업 계획과 확정된 결정은 [`docs/superpowers/specs/2026-10-19-defense-battle-port-design.md`](docs/superpowers/specs/2026-10-19-defense-battle-port-design.md) 에 있다. 작업 전에 먼저 읽고, 단계 순서대로 진행한다.

## 참고 저장소

- 원본: `../DefenseBattle` (같은 부모 폴더에 클론). 수치·규칙·문구의 기준은 `../DefenseBattle/game/src` 와 `docs/DEV_NOTES.md`, `docs/GDD.md`.
- 원본에 없는 동작을 새로 만들거나 수치를 바꿔야 하면 사용자에게 먼저 묻는다.
- `../DefenseBattle` 이 없으면 사용자에게 클론을 부탁한다 (`docs/unity-mcp-setup.md` 2번).

## Unity 에디터 (MCP)

- Unity 6000.4.0f1, 2D URP, Android 세로 화면. 에디터는 `UnityMCP` MCP 서버로 조작한다 (세팅: `docs/unity-mcp-setup.md`).
- 스크립트를 고친 뒤에는 컴파일이 끝나길 기다렸다가 **콘솔 에러/경고를 확인**하고 고친 뒤 다음으로 넘어간다.
- 씬·프리팹은 `.unity`/`.prefab` YAML 을 손으로 고치지 말고 MCP 도구로 편집한다.
- 동작 확인은 플레이 모드에서 하고, 단계별 "완료 기준"을 확인한 뒤 커밋한다.
- MCP 가 연결돼 있지 않으면 작업을 멈추고 사용자에게 알린다 (추측으로 씬을 구성하지 않는다).

## 코드 구조와 규칙

- `Assets/Scripts/Core/` : 게임 규칙. 순수 C# (`UnityEngine` 참조 금지, asmdef `noEngineReferences: true`). 원본 `engine.ts`·`config.ts`·`ai.ts`·`econ/rules.ts` 대응.
- `Assets/Scripts/View/`, `Assets/Scripts/UI/` : Core 상태를 그리고 입력을 Core 명령으로 넘긴다. 게임 규칙을 여기 두지 않는다.
- 기존 코드 스타일을 따른다: 직렬화 필드 `m_` 접두사 + `[SerializeField]`, 한국어 `/// <summary>` 주석, 한 클래스 한 파일.
- 원본에서 옮긴 숫자에는 출처 주석을 단다 (예: `// config.ts ECONOMY.startGold`).
- 화면 문구는 한국어 원문을 키로 쓰는 다국어 함수로 감싼다 (원본 `tl()` 과 같은 방식, 5개 언어).
- 재화·보상·결제 계산은 서버(UGS Cloud Code) 권위. 클라이언트에서 재화를 직접 늘리는 코드를 만들지 않는다.

## 최적화 규칙 (모바일 - GC 스파이크 = 프레임 끊김)

- **매 프레임 도는 코드에서 할당 금지**: `new` 객체·배열, LINQ, 바깥 변수를 잡는 람다, `foreach` 로 박싱되는 컬렉션, 문자열 조합을 쓰지 않는다. 버퍼는 필드로 두고 `Clear()` 후 재사용한다.
- **오브젝트 풀링**: 자주 생기고 사라지는 것은 풀에서 꺼내 쓴다. Core 는 `ObjectPool<T>` (몹·이펙트·불 장판), View 는 스프라이트·투사체·데미지 숫자 GameObject 를 풀링한다 (기존 `PoolManager`/`EffectPool` 참고). `Instantiate`/`Destroy` 를 전투 중에 부르지 않는다.
- **Core 객체를 프레임 넘어 들고 있지 않는다**: `Mob`·`Effect` 는 엔진이 재사용한다. View 는 `Mob.Id` 로 스프라이트를 짝짓고, 매 프레임 `Game.Mobs`/`Game.Effects` 를 다시 읽는다.
- **거리 비교는 제곱으로** (`JsMath.DistSq`/`Within`), 고정된 값(길 표본 등)은 한 번만 계산해 둔다.
- Unity 쪽: `Update` 안 `GetComponent`·`Find` 금지 (캐싱), TMP 텍스트는 값이 바뀔 때만 갱신, 움직이는 UI 와 정적 UI 는 Canvas 를 나눈다, 스프라이트는 Sprite Atlas 로 묶는다.
- 최적화로 로직이 바뀌면 안 된다: 바꾼 뒤 `Sim fingerprint` 결과가 바꾸기 전과 **완전히 같아야** 한다 (아래 테스트).

## 테스트

- Core 는 EditMode 테스트(`Assets/Tests/EditMode/`)로 검증한다. MCP 로 Test Runner 를 실행해 결과를 확인한다 (Package Manager 에 Test Framework 가 있어야 테스트 어셈블리가 컴파일된다).
- Unity 없이도 같은 테스트를 돌릴 수 있다 (.NET 8 SDK):
  ```bash
  dotnet test CoreTests/Tests                                   # EditMode 테스트 전부
  dotnet run -c Release --project CoreTests/Sim -- 30           # 무한 모드 밸런스 (원본 simulate.ts)
  dotnet run -c Release --project CoreTests/Sim -- stages 10    # 캠페인 클리어율
  dotnet run -c Release --project CoreTests/Sim -- pvp 30       # PvP 승률·판 길이 (원본 pvpsim.ts)
  dotnet run -c Release --project CoreTests/Sim -- fingerprint  # 시드 고정 결과 (최적화 전후 비교용)
  dotnet run -c Release --project CoreTests/Sim -- bench 3 0    # 프레임당 시간·할당량
  ```
- 밸런스는 원본 시뮬레이터(`../DefenseBattle/tools/simulate.ts`, `pvpsim.ts`) 결과와 비교한다.

## 저장소

- 서드파티 원본 에셋 팩은 커밋하지 않는다 (가공본만). 키스토어·API 키·광고 ID 비밀값도 커밋하지 않는다.
- 커밋 메시지는 영어 한 줄 요약 + 필요하면 본문.
