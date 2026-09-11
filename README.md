# GETAWAY DRIVER

약속 시각에 은행에 도착해 강도와 돈을 싣고 경찰을 따돌리는 **PC 1인용 3D 아케이드 레이싱 프로토타입**입니다. 탈출에 성공해 지킨 돈은 저장되어 차량과 업그레이드 구매에 사용할 수 있습니다.

## 실행

1. Unity Hub에서 이 폴더를 `Add project from disk`로 추가합니다.
2. 제작 기준 에디터는 **Unity 6000.6.0f1**, 렌더링은 **Built-in**입니다.
3. `Assets/Getaway/Scenes/Getaway.unity`를 열고 ▶ Play를 누릅니다.
4. 씬이 없는 소스 패키지라면 상단 `Getaway > Create or rebuild demo scene`을 먼저 누릅니다. 이 메뉴는 데모 씬을 다시 만들므로 커스텀 씬 작업은 별도 이름으로 저장하세요.
5. `Start job`을 누릅니다. 1스테이지는 **시작 후 14초에 은행 파란 구역에서 정차**하는 것이 목표입니다. 첫 정차 시각이 약속 시각에 가까울수록 최대 1,000점을 받습니다.
6. 약속 시각에 강도가 나오면 2초 연속 정차해 태웁니다. 탑승 완료 시 현금과 경찰 추격이 시작됩니다.
7. 경찰 피해에 비례해 소지 현금이 줄어듭니다. 경찰을 따돌린 뒤 초록 목적지에서 정차하면 남은 돈이 지갑에 입금됩니다.
8. 시작 전이나 결과 화면의 `Garage / Shop`에서 차량·업그레이드를 구매합니다. 구매 차량은 `Equip`을 눌러 장착하고 다음 임무부터 사용합니다.

Windows 빌드는 `Getaway > Build Windows`로 생성합니다. `Builds/Windows/GetawayDriver.exe`와 같은 폴더의 데이터 파일을 함께 배포해야 합니다. 종료는 Windows 기본 단축키 `Alt+F4`입니다.

## 조작

| 입력 | 동작 |
|---|---|
| W / ↑ | 가속 |
| S / ↓ | 전진 중 감속, 정지 후 후진 |
| A·D / ←·→ | 조향 |
| Space | 브레이크. 조향과 함께 누르면 접지가 풀리며 드리프트 |
| R | 도로 중앙으로 복구, 내구도 8 감소, 3초 쿨다운 |
| Esc | 일시 정지 / 계속 |

직진 중 Space는 일반 브레이크이고, 조향을 넣은 채 누르면 횡방향 접지가 무너지면서 선회력이 1.4배로 올라가고 감속이 약해져 속도를 실은 채 코너를 돌 수 있습니다. 손을 떼도 접지는 약 1.4초에 걸쳐 서서히 회복되므로 슬라이드가 잠시 이어집니다. 조향은 저속에서 민감하고 고속에서 둔해지도록 속도에 따라 선회 속도가 120°/s에서 52°/s로 줄어듭니다. 주행 감각 수치는 모두 `ArcadeCar` 컴포넌트의 Inspector에서 조절할 수 있습니다.

동료 탑승 조건은 강도가 나온 뒤 은행 반경 6m, 속도 1.5m/s 미만, 연속 2초입니다. 약속 시각 +12초까지 탑승을 끝내야 합니다. 목적지 조건은 추격 해제, 반경 7m, 속도 2m/s 미만입니다. 경찰이 7m 안에 있고 속도가 2m/s 미만인 상태가 4초 누적되면 체포됩니다. 파손·시간 초과도 실패입니다. 실패한 임무는 현금이 입금되지 않으며 기존 지갑 잔액은 유지됩니다.

도착 점수와 자금은 별도입니다. 첫 정차 점수는 기다리거나 재진입해도 바뀌지 않습니다. 자세한 규칙과 가격은 [게임 진행과 상점](Docs/CAREER_GAMEPLAY.md)을 참고하세요.

## 스테이지

| 스테이지 | 도로 | 경찰 | 제한 시간 | 탈출 조건 |
|---|---:|---:|---:|---|
| The First Job | 650m | 2대 | 100초 | 65m 초과 간격, 5초 |
| Downtown Heat | 850m | 3대 | 120초 | 70m 초과 간격, 6초 |
| The Last Ride | 1,050m | 4대 | 140초 | 75m 초과 간격, 7초 |

| 스테이지 | 은행 Z 위치 | 약속 시각 | 초기 현금 | 경찰 피해 1당 손실 |
|---|---:|---:|---:|---:|
| 1 | 180m | 14초 | 12,000 | 120 |
| 2 | 240m | 18초 | 18,000 | 180 |
| 3 | 300m | 22초 | 24,000 | 240 |

`Assets/Getaway/Stages/Stage01~03.asset`의 Inspector에서 난이도를 조절합니다. 새 `Getaway/Stage` 에셋을 만들고 씬의 `GameSession.stages` 배열에 추가하면 스테이지를 늘릴 수 있습니다. 재실행하면 가장 높은 해금 스테이지에서 시작하며, 화면 버튼으로 1스테이지부터 다시 할 수 있습니다.

## 코드 구조

| 파일 | 역할 |
|---|---|
| `ArcadeCar.cs` | Rigidbody 기반 가속·후진·조향·충돌 피해·복구 |
| `GameInput.cs` | 키보드 입력, 기존/New Input System 조건부 지원 |
| `PoliceDriver.cs` | 추적·장애물 감지·막힘 시 후진 |
| `GameSession.cs` | 브리핑→탑승→추격→탈출→성공/실패 |
| `StageDefinition.cs` | ScriptableObject 스테이지 데이터 |
| `WorldBuilder.cs` | 도로·도시·차량·동료·목적지 생성, 모델 교체 슬롯 |
| `FollowCamera.cs` | 추적 카메라, 환경 충돌 회피 |
| `GameHud.cs` | 속도·내구도·시간·목표 방향·진행 UI |
| `RunLogger.cs` | 세션별 JSONL 이벤트, 1초 차량 상태, Console 로그 |
| `CareerRules.cs` | 시간 점수·현금 손실·구매·정산 규칙, 차량 목록 |
| `CareerStorage.cs` | 임시 파일 검증·교체, 백업 복원, 손상 파일 보호 |
| `ProgressStore.cs` | Unity JSON 직렬화와 기존 저장 경로 연결 |
| `Editor/ProjectSetup.cs` | 씬 생성·Windows 빌드 |
| `Editor/SmokeTests.cs` | 실제 Play Mode 임무·물리 검사 |

## 모델·로그·GitHub

- [Varco 3D 모델 적용](Docs/VARCO_WORKFLOW.md)
- [기록과 GitHub 운영](Docs/LOGGING_AND_GITHUB.md)
- [개발 이력](Docs/DEVLOG.md)
- [검증 내역](Docs/VALIDATION.md)

외부 패키지 없이 실행하도록 기본형 모델과 IMGUI를 사용했습니다. 게임 HUD는 영문이며 설명서는 한국어입니다. Varco 모델, 애니메이션, 음원은 아직 포함하지 않았습니다. 실제 경찰 시뮬레이션이나 휠별 서스펜션이 아닌 아케이드 차량 모델이며 평탄한 직선 도로와 장애물을 사용하는 첫 프로토타입입니다. 복잡한 교차로용 경로 탐색, 교통 차량, 재추격 파동, 승객별 부상, 리플레이, 한글 UI는 후속 확장 항목입니다.

## 참고한 공식 문서

- [Unity Rigidbody.linearVelocity](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rigidbody-linearVelocity.html)
- [Unity persistentDataPath](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-persistentDataPath.html)
- [Unity Input System 설치 및 입력 백엔드](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/Installation.html)
- [GitHub Git LFS](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-git-large-file-storage)
