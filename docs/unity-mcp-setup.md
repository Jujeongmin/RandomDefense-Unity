# 내 PC에서 Claude Code + Unity MCP 세팅

Claude Code가 Unity 에디터를 직접 조작(오브젝트 생성·씬 편집·콘솔 에러 확인·플레이 모드 실행)할 수 있게 연결하는 방법.
클라우드 세션에서는 Unity를 설치할 수 없어서, 실제 이식 작업은 이 구성으로 PC에서 진행한다.

## 1. 준비물

| 항목 | 버전 / 비고 |
| --- | --- |
| Unity | 6000.4.0f1 (이 프로젝트 버전, Unity Hub로 설치) |
| Claude Code | 데스크톱 앱 또는 CLI (`npm install -g @anthropic-ai/claude-code`) |
| Python | 3.10 이상 |
| uv | Python 패키지 매니저. Windows PowerShell: `powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 \| iex"` · macOS: `curl -LsSf https://astral.sh/uv/install.sh \| sh` |
| Git | 두 저장소 클론용 |

## 2. 저장소 두 개를 나란히 클론

이식 작업 중 Defense Battle 원본 코드를 계속 참고하므로 **같은 부모 폴더**에 둔다.

```bash
mkdir JunJa && cd JunJa
git clone https://github.com/Jujeongmin/RandomDefense-Unity.git
git clone https://github.com/Jujeongmin/DefenseBattle.git
cd RandomDefense-Unity
git checkout claude/peaceful-fermat-lqq9mg   # 이식 작업 브랜치
```

`Assets/Down/` 서드파티 팩(README의 "클론 시 주의")은 Defense Battle 픽셀아트로 교체할 예정이라
새 앱 작업에는 필요 없다. 다만 교체가 끝나기 전까지 기존 씬을 열면 참조가 깨진 채로 보일 수 있다.

## 3. Unity에 MCP for Unity 패키지 설치

1. Unity Hub로 `RandomDefense-Unity` 프로젝트를 연다.
2. **Window → Package Manager → + → Add package from git URL...**
3. 아래 주소 입력:
   ```
   https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main
   ```
4. 설치되면 **Window → MCP for Unity** 창이 생긴다.

## 4. Claude Code 연결

**자동:** `Window → MCP for Unity` 창에서 **Configure All Detected Clients** 클릭 → Claude Code가 감지되면 자동 등록.

**수동 (자동이 안 될 때):** 프로젝트 폴더에서 터미널을 열고, MCP for Unity 창에 표시되는 명령을 그대로 실행한다.
HTTP 모드 기본값이면 보통 다음과 같다.

```bash
claude mcp add --scope local --transport http UnityMCP http://127.0.0.1:8080/mcp
claude mcp list        # UnityMCP 가 connected 로 보이면 성공
```

## 5. 확인

1. Unity 에디터를 켜 둔 채로, `RandomDefense-Unity` 폴더에서 Claude Code를 실행한다.
2. `/mcp` 를 입력해 `UnityMCP` 가 연결돼 있는지 본다.
3. 시험 삼아 이렇게 말해 본다: *"빈 씬에 큐브를 하나 만들고, 확인했으면 지워 줘."*
   에디터에 큐브가 생겼다 사라지면 끝.

## 6. 이식 작업 시작

Claude Code에 이렇게 말하면 된다.

> `docs/superpowers/specs/2026-10-19-defense-battle-port-design.md` 의 0단계부터 진행해 줘.

저장소 루트의 `CLAUDE.md` 에 작업 규칙이 있어서 Claude Code가 자동으로 읽는다.

## 문제 해결

- **연결이 안 됨:** Unity 에디터가 켜져 있는지, MCP for Unity 창에서 서버가 실행 중인지 확인 → Claude Code 재시작.
- **uv 를 못 찾음:** 터미널을 새로 열거나 PC 재부팅 후 `uv --version` 확인. MCP for Unity 창의 Advanced Settings에서 uvx 경로 지정 가능.
- **WSL에서 Claude Code를 쓰는 경우:** `127.0.0.1` 대신 Windows 호스트 IP로 등록해야 한다 (MCP for Unity 문서의 troubleshooting 참고).
- 더 자세한 내용: <https://github.com/CoplayDev/unity-mcp>
