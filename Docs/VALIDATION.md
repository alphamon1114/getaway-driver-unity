# 검증 결과 — 은행 약속과 커리어 경제

## 이번 변경에서 통과한 검사

- 실제 제작 코드 `CareerRules.cs`, `CareerStorage.cs`를 직접 사용하는 .NET 10 검사 **42개 통과**. 결과: [career-tests.txt](Validation/career-tests.txt).
- 시간 정확도, 조기 대기·재진입 점수 악용 방지, 탑승 기한·연속 정차, 현금 손실 비례·하한, 이전 저장 이관, 잔액 부족·중복 구매·강화 상한, 차량별 강화, 정산 중복 방지 검사.
- 실제 파일에 저장 후 다시 읽어 지갑·차량·강화·장착 복원을 확인. 저장 실패 시 구매·정산 롤백, 손상 파일 백업 복원, 미래 버전 덮어쓰기 차단도 검사.
- Unity **6000.6.0f1**의 실제 API 및 .NET Standard 2.1 참조를 사용한 런타임·에디터 C# 컴파일: **오류 0 / 경고 0**.
- 이 테스트에서 JSON 직렬화는 .NET System.Text.Json을 사용합니다. 게임에서 쓰는 Unity JsonUtility 및 충돌 물리 연결은 아래 Play Mode 검사 대상입니다.

## Unity 실행 범위

사용자는 이전 버전이 Unity에서 실행되는 것을 확인했습니다. 이번 변경 기준으로는 새 작업 폴더에서 Unity 배치 Play Mode를 시도했으나 **Package Manager IPC 연결 실패**로 스크립트 실행 전 종료되었습니다. 따라서 새 은행 타이밍·상점 UI를 실제 Unity에서 조작한 결과, Unity JSON 저장, 충돌 물리, 셰이더 및 Windows 빌드는 아직 미검증입니다.

기존 주행감 개선과 사용자가 에디터에서 저장한 `testplay` 씬을 기반으로 작업했습니다. 이번 작업에서 데모 씬은 재생성하지 않았습니다. 새 필드는 스크립트와 스테이지 에셋으로 연결됩니다.

## 재현

에디터 없이 규칙·저장 테스트 (.NET 10 SDK 필요):

```powershell
.\Tools\Test-Career.ps1
```

Unity 설치 파일을 참조하는 C# 검사:

```powershell
.\Tools\Check-CSharp.ps1
```

Unity Play Mode와 Windows 빌드 (실행 중인 해당 프로젝트 에디터는 먼저 닫기):

```powershell
.\Tools\Validate.ps1 -Build
```

Play Mode 검사에는 기존 임무·물리 확인 외에 예정 시각 전 탑승 금지, 조기 도착 점수 고정, 경찰 피해 이벤트의 현금 차감, 환경 피해의 현금 유지, 성공 정산·중복 지급 방지, 상점 강화의 Unity JSON 저장, 실패 시 지갑 유지가 추가되었습니다. 검사마다 `GetawayDriver-AutomatedTests-<ID>` 제품 이름을 사용해 실제 플레이 저장과 분리합니다.

직접 확인할 순서: ① 1스테이지에서 14초에 첫 정차 ② 조기 도착 후 기다려도 점수가 오르지 않는지 ③ 강도 탑승 뒤 경찰 충돌 시 현금 차감 ④ 성공 후 지갑 입금 ⑤ 상점에서 엔진 강화·차량 구매와 장착 ⑥ Play를 껐다 켜 지갑·강화·차량이 남는지.
