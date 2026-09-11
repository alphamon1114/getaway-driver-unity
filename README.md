# GETAWAY DRIVER

강도단의 운전수가 동료를 태우고 경찰을 따돌려 은신처로 데려가는 **PC 1인용 3D 아케이드 레이싱 프로토타입**입니다. 은행 강도 장면은 브리핑으로 처리하고 운전과 도주에 집중합니다.

## 실행

1. Unity Hub에서 이 폴더를 `Add project from disk`로 추가합니다.
2. 제작 기준 에디터는 **Unity 6000.6.0f1**, 렌더링은 **Built-in**입니다.
3. `Assets/Getaway/Scenes/Getaway.unity`를 열고 ▶ Play를 누릅니다.
4. 씬이 없는 소스 패키지라면 상단 `Getaway > Create or rebuild demo scene`을 먼저 누릅니다. 이 메뉴는 데모 씬을 다시 만들므로 커스텀 씬 작업은 별도 이름으로 저장하세요.
5. `Start getaway`를 클릭합니다. 도로 앞쪽 파란 구역에 진입해 2초 정차하면 동료가 탑승하고 경찰이 출동합니다.
6. 경찰과 지정 거리 이상 떨어진 상태를 유지해 추격을 해제한 다음, 초록 목적지 구역에서 정차합니다.

Windows 빌드는 `Getaway > Build Windows`로 생성합니다. `Builds/Windows/GetawayDriver.exe`와 같은 폴더의 데이터 파일을 함께 배포해야 합니다. 종료는 Windows 기본 단축키 `Alt+F4`입니다.

## 조작

| 입력 | 동작 |
|---|---|
| W / ↑ | 가속 |
| S / ↓ | 전진 중 감속, 정지 후 후진 |
| A·D / ←·→ | 조향 |
| Space | 브레이크 / 낮은 횡방향 접지 |
| R | 도로 중앙으로 복구, 내구도 8 감소, 3초 쿨다운 |
| Esc | 일시 정지 / 계속 |

동료 탑승 조건은 반경 6m, 속도 1.5m/s 미만, 연속 2초입니다. 목적지 조건은 추격 해제, 반경 7m, 속도 2m/s 미만입니다. 경찰이 7m 안에 있고 플레이어 속도가 2m/s 미만인 상태가 4초 누적되면 체포됩니다. 내구도 0 또는 시간 소진도 실패입니다.

## 스테이지

| 스테이지 | 도로 | 경찰 | 제한 시간 | 탈출 조건 |
|---|---:|---:|---:|---|
| The First Job | 650m | 2대 | 100초 | 65m 초과 간격, 5초 |
| Downtown Heat | 850m | 3대 | 120초 | 70m 초과 간격, 6초 |
| The Last Ride | 1,050m | 4대 | 140초 | 75m 초과 간격, 7초 |

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
| `ProgressStore.cs` | 해금 상태 JSON 저장·임시 파일 교체·백업 |
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
