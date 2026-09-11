# GETAWAY DRIVER

경찰 바리케이드를 뚫고 제한시간 안에 은행에 닿아 강도와 돈을 싣고, 도시 밖 탈출 도로까지 차를 온전히 몰고 나가는 **PC 1인용 3D 아케이드 레이싱 프로토타입**입니다. 지켜낸 돈은 저장되어 차량과 업그레이드 구매에 쓰입니다.

## 실행

1. Unity Hub에서 이 폴더를 `Add project from disk`로 추가합니다.
2. 제작 기준 에디터는 **Unity 6000.6.0f1**, 렌더링은 **Built-in**입니다.
3. `Assets/Getaway/Scenes/Getaway.unity`를 열고 ▶ Play를 누릅니다.
4. 씬이 없는 소스 패키지라면 상단 `Getaway > Create or rebuild demo scene`을 먼저 누릅니다. 이 메뉴는 데모 씬을 다시 만들므로 커스텀 씬 작업은 별도 이름으로 저장하세요.
5. `Start job`을 누릅니다. 도로를 가로막은 노란 바리케이드마다 갭이 하나씩 뚫려 있고, 사거리에는 일반 차량이 지나다닙니다. 1스테이지는 **13초 안에** 은행 파란 구역에 정차해 탑승까지 마쳐야 합니다.
6. 은행에서 2초 연속 정차하면 강도가 탑니다. 빨리 도착할수록 점수가 높고, 7초 이내면 만점입니다.
7. 탑승 즉시 현금을 받고 경찰이 출동합니다. 들이받힐 때마다 현금이 줄어듭니다. 본선 끝 부근 오른쪽으로 갈라지는 탈출 도로를 타고 초록 `CITY LIMITS` 구역에 도달하면 남은 돈이 지갑에 입금됩니다. 정차할 필요는 없습니다.
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

탑승 조건은 은행 반경 6m, 속도 1.5m/s 미만, 연속 2초이며 제한시간 안에 끝내야 합니다. 성공 조건은 탈출 구역 반경 10m 진입이고 속도 제한은 없습니다. 경찰이 7m 안에 있고 속도가 2m/s 미만인 상태가 4초 누적되면 체포됩니다. 파손·시간 초과도 실패입니다. 실패한 임무는 현금이 입금되지 않으며 기존 지갑 잔액은 유지됩니다.

도착 점수와 자금은 별도입니다. 점수는 첫 정차 순간에 확정되며 재진입해도 바뀌지 않습니다. 빠를수록 점수가 높으므로 엔진 강화가 점수에 손해를 주지 않습니다. 자세한 규칙과 가격은 [게임 진행과 상점](Docs/CAREER_GAMEPLAY.md)을 참고하세요.

## 스테이지

| 스테이지 | 도로 | 경찰 | 경찰 최고속 / 가속 | 임무 제한 시간 |
|---|---:|---:|---:|---:|
| The First Job | 650m | 2대 | 38 m/s · 9 m/s² | 100초 |
| Downtown Heat | 850m | 3대 | 46 m/s · 9.5 m/s² | 120초 |
| The Last Ride | 1,050m | 4대 | 56 m/s · 10 m/s² | 140초 |

| 스테이지 | 은행 Z | 은행 제한시간 | 만점 기준 | 사거리 | 바리케이드 | 갭 폭 | 주차 차량 | 통행 차량 | 탈출 분기 Z | 초기 현금 | 피해 1당 손실 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 180m | 13초 | 7초 | 4개 | 3개 | 8m | 14대 | 8대 | 530m | 12,000 | 120 |
| 2 | 240m | 17초 | 9초 | 7개 | 6개 | 7m | 14대 | 14대 | 720m | 18,000 | 180 |
| 3 | 300m | 20초 | 11초 | 8개 | 9개 | 6.5m | 31대 | 24대 | 910m | 24,000 | 240 |

경찰 최고속은 최강 차량(Night Runner 엔진 3레벨, 약 52 m/s)보다 약간 높게 잡혀 있습니다. 대신 가속도가 플레이어의 절반 이하라, 바리케이드에서 감속한 뒤 다시 붙는 구간마다 거리가 벌어집니다.

`Assets/Getaway/Stages/Stage01~03.asset`의 Inspector에서 난이도를 조절합니다. 새 `Getaway/Stage` 에셋을 만들고 씬의 `GameSession.stages` 배열에 추가하면 스테이지를 늘릴 수 있습니다. 재실행하면 가장 높은 해금 스테이지에서 시작하며, 화면 버튼으로 1스테이지부터 다시 할 수 있습니다.

## 코드 구조

| 파일 | 역할 |
|---|---|
| `ArcadeCar.cs` | Rigidbody 기반 가속·후진·조향·충돌 피해·복구 |
| `GameInput.cs` | 키보드 입력, 기존/New Input System 조건부 지원 |
| `PoliceDriver.cs` | 추적, 바리케이드 갭 조준과 감속, 막힘 시 후진 |
| `GameSession.cs` | 브리핑→은행 돌파→추격→도시 탈출→성공/실패 |
| `StageDefinition.cs` | ScriptableObject 스테이지 데이터 |
| `WorldBuilder.cs` | 도로·사거리·횡단보도·건물·바리케이드·탈출 도로·차량 생성, 모델 교체 슬롯 |
| `TrafficCar.cs` | 사거리를 가로지르는 일반 차량 주행 |
| `PoliceLights.cs` | 순찰차 지붕 경광등 적청 점멸 |
| `FollowCamera.cs` | 추적 카메라, 환경 충돌 회피 |
| `GameHud.cs` | 속도·내구도·시간·목표 방향·진행 UI |
| `RunLogger.cs` | 세션별 JSONL 이벤트, 1초 차량 상태, Console 로그 |
| `CareerRules.cs` | 도착 점수·현금 손실·구매·정산 규칙, 차량 목록 |
| `CareerStorage.cs` | 임시 파일 검증·교체, 백업 복원, 손상 파일 보호 |
| `ProgressStore.cs` | Unity JSON 직렬화와 기존 저장 경로 연결 |
| `Editor/ProjectSetup.cs` | 씬 생성·Windows 빌드 |
| `Editor/SmokeTests.cs` | 실제 Play Mode 임무·물리 검사 |

## 모델·로그·GitHub

- [Varco 3D 모델 적용](Docs/VARCO_WORKFLOW.md)
- [기록과 GitHub 운영](Docs/LOGGING_AND_GITHUB.md)
- [개발 이력](Docs/DEVLOG.md)
- [검증 내역](Docs/VALIDATION.md)

도시는 사거리로 나뉜 블록 구조이며 횡단보도, 신호등, 인도, 갓길 주차 차량, 사거리 통행 차량이 있습니다. 통행 차량과 주차 차량은 경찰이 아니므로 부딪혀도 현금은 줄지 않고 내구도만 깎입니다. 순찰차는 흰 차체에 측면 스트라이프와 적청 점멸 경광등을 답니다. 추격 카메라가 앞을 보기 때문에 뒤에 붙은 순찰차는 화면에 안 보이며, HUD의 `Patrol BEHIND 12m` 표시로 위치를 알립니다.

외부 패키지 없이 실행하도록 기본형 모델과 IMGUI를 사용했습니다. 게임 HUD는 영문이며 설명서는 한국어입니다. Varco 모델, 애니메이션, 음원은 아직 포함하지 않았습니다. 실제 경찰 시뮬레이션이나 휠별 서스펜션이 아닌 아케이드 차량 모델이며 평탄한 직선 도로와 장애물을 사용하는 첫 프로토타입입니다. 복잡한 교차로용 경로 탐색, 교통 차량, 재추격 파동, 승객별 부상, 리플레이, 한글 UI는 후속 확장 항목입니다.

## 참고한 공식 문서

- [Unity Rigidbody.linearVelocity](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rigidbody-linearVelocity.html)
- [Unity persistentDataPath](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-persistentDataPath.html)
- [Unity Input System 설치 및 입력 백엔드](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/Installation.html)
- [GitHub Git LFS](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-git-large-file-storage)
