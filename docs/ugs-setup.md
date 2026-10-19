# 서버·스토어 세팅 (사용자가 대시보드에서 할 일)

코드로는 할 수 없고 계정이 필요한 작업만 모았다. 순서대로 하면 된다. 각 단계에서 나오는 ID·키는 채팅에 붙이지 말고,
아래 "어디에 넣나" 칸의 위치에 직접 넣는다 (비밀값은 저장소에 커밋하지 않는다).

## 0. 기본 정보 (확정)

| 항목 | 값 |
| --- | --- |
| 회사명 | JeonJa |
| 앱 이름 | 디펜스 배틀 (Defense Battle) |
| 패키지명 | `com.jeonja.defensebattle` (출시 후 변경 불가) |
| 로그인 | Google Play 로그인 필수 |
| 인터넷 | 필수 (오프라인이면 연결 안내 화면) |

## 1. Google Play Console - 새 앱
1. 앱 만들기 → 이름 "디펜스 배틀", 패키지 `com.jeonja.defensebattle`.
2. **수익 창출 → 인앱 상품**: 아래 상품 ID 로 "관리형 상품"을 만든다. 가격은 원화 가격표 확정 후 (아래 6번).
   `starter_pack`, `monthly_pass`, `ad_remove`, `season_premium`, `summon_ticket_pack`, `gold_booster_pack`,
   `offer_comeback`, `offer_arena`, `offer_chapter`, `unit_ranger`, `unit_frost`, `unit_paladin`, `unit_bomber`, `unit_assassin`,
   `chest_gold`, `chest_magic`, `chest_legend`, `crystal_100`, `crystal_550`, `crystal_1200`, `crystal_2600`, `crystal_7000`, `crystal_15000`
   (`mythic_ticket` 은 판매 중단 상품이라 만들지 않는다)
3. **수익 창출 설정 → 라이선스**: "Base64 인코딩 RSA 공개 키"를 복사해 둔다 → 4-5 Economy 에 넣는다.
4. **Play 게임즈 서비스 → 설정**: 새 게임 만들기 → OAuth 동의 화면 → 사용자 인증 정보:
   - Android 클라이언트 (패키지명 + 업로드 키 SHA-1)
   - **웹 애플리케이션** 클라이언트 → 클라이언트 ID·보안 비밀 → 4-2 UGS 에 넣는다

## 2. AdMob - 새 앱
1. 앱 추가 → Android, Play 스토어 등록 전이면 "아니요"로 만들고 나중에 연결.
2. **보상형 광고 단위 3개**: `reward_double`(결과 보상 2배), `reward_revive`(실패 시 부활), `reward_ticket`(하루 3회 뽑기권).
3. 앱 ID → Unity `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings`, 광고 단위 ID → `Assets/Scripts/Ads/AdMobRewardedAdService.cs` (PC 작업 때 같이 바꾼다 - 지금 값은 예전 앱 것).
4. 개인정보·동의 → GDPR 동의 메시지 만들기 (기존 앱과 같은 방식).

## 3. Unity Cloud - 프로젝트 연결
1. <https://cloud.unity.com> → 새 프로젝트 "Defense Battle".
2. Unity 에디터 **Edit → Project Settings → Services** 에서 그 프로젝트로 연결 (조직·프로젝트 선택).

## 4. Unity Gaming Services (대시보드)
1. **Cloud Save / Relay**: 켜기만 하면 된다 (별도 설정 없음).
2. **Authentication → Identity providers → Google Play Games**: 1-4 의 웹 클라이언트 ID·보안 비밀 입력.
3. **Leaderboards**: 새 리더보드
   - ID `trophies`, 정렬 높은 순, **업데이트 방식: 마지막 점수 유지(Keep latest)** - 트로피는 내려가기도 해서 '최고 점수'로 하면 안 된다.
4. **Economy**:
   - 통화 하나 만들기: ID `PURCHASE_TOKEN` (영수증 검증용 자리표시 - 게임에서 안 씀)
   - **실제 결제(Real money purchases)**: 1-2 의 상품마다 하나씩. Economy ID = 상품 ID 대문자 (`CRYSTAL_550` …),
     Google Play 상품 ID = 원래 상품 ID (`crystal_550` …), 보상 = `PURCHASE_TOKEN` 1개.
     실제 지급(보석·상자·유닛)은 서버 코드(`EconRules.GrantProduct`)가 하고, Economy 는 영수증 진위 확인만 한다.
   - 설정 → Google Play 라이선스 키: 1-3 의 공개 키 입력.
   - 다 만든 뒤 **Publish**.
5. **Cloud Code**: 모듈은 Unity 에디터에서 배포한다 (PC 작업).
   - Package Manager 에 `Cloud Code`, `Deployment` 패키지 설치 → **Window → Deployment** → `Assets/CloudCode/DefenseBattleCloud.ccmr` 선택 → Deploy.
   - 모듈 소스: `CloudCode/Module` (게임 규칙은 클라이언트와 같은 `Assets/Scripts/Core` 를 그대로 컴파일).

## 5. 확인
- Cloud Code 대시보드에 `DefenseBattleCloud` 모듈과 함수 30여 개(EconSync, PvpQueue …)가 보이면 배포 성공.
- 에디터에서 Google Play 로그인 → `EconSync` 호출 → Cloud Save 의 플레이어 Protected 데이터에 `econ` 이 생기면 연결 성공 (5단계 클라이언트 작업 때 확인).

## 6. 원화 가격 (확정 대기)
가격표 초안: 원본 달러 가격을 Google Play 한국 가격대(부가세 포함)에 맞춤. 확정되면 Play Console 상품 가격과 `Assets/Scripts/Core/Config/Products.cs` 에 넣는다.

| 원본 | 원화 | 상품 |
| --- | --- | --- |
| $0.99 | ₩1,200 | starter_pack, crystal_100 |
| $1.99 | ₩2,500 | summon_ticket_pack, gold_booster_pack, offer_comeback |
| $2.99 | ₩3,900 | unit_ranger, unit_frost, chest_gold |
| $4.99 | ₩6,500 | monthly_pass, ad_remove, unit_paladin, unit_bomber, offer_arena, offer_chapter, crystal_550 |
| $6.99 | ₩8,900 | unit_assassin, chest_magic |
| $9.99 | ₩12,000 | season_premium, chest_legend, crystal_1200 |
| $19.99 | ₩25,000 | crystal_2600 |
| $49.99 | ₩65,000 | crystal_7000 |
| $99.99 | ₩119,000 | crystal_15000 |
