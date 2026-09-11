# 검증 결과

## 완료한 검사

- 설치된 Unity **6000.6.0f1**의 실제 `UnityEngine.*` / `UnityEditor.*` 어셈블리 및 .NET Standard 2.1 참조를 사용해 런타임·에디터 C# 소스를 Roslyn으로 컴파일.
- 최종 소스 컴파일: **오류 0, 경고 0**. 이는 C# 구문·타입·Unity API 참조 검사이며 Unity 에디터 전체 임포트/셰이더 빌드 통과를 의미하지 않음.
- 씬/스테이지/스크립트의 `.meta` GUID 참조와 빌드 씬 연결을 정적 검사.
- GitHub 비공개 저장소 생성과 읽기·쓰기 권한 확인.

## 실행하지 못한 검사

Unity 배치 실행은 첫 시도에서 Package Manager IPC 연결 실패로 종료되었습니다. Package Manager를 사용하지 않는 재시도에서는 Unity Licensing Client 연결 거부와 초기화 시간 초과가 발생했습니다. 권한 요청 결과 네트워크만 허용되었고 Unity 캐시 쓰기 권한은 추가되지 않았습니다. 실제 원인은 이 실행 환경의 IPC/권한 제약 또는 로컬 라이선스 상태일 수 있으며 이 작업에서 확정하지 못했습니다.

따라서 **Play Mode 검사, 키보드 플레이, 화면 육안 확인, 셰이더 컴파일, Windows 실행 파일 빌드는 미검증**입니다. 실행 파일을 제공하지 않으며 Unity 소스 프로젝트를 제공합니다. 씬과 스테이지 YAML은 에디터 실행 제한으로 직접 작성한 뒤 GUID와 구성만 정적으로 검사했습니다.

## PC에서 이어서 검사

Unity Hub에서 해당 에디터가 정상적으로 프로젝트를 열 수 있는지 확인하고 데모 씬을 Play 하세요. 필요하면 `Getaway > Create or rebuild demo scene`으로 에디터가 씬을 다시 생성하게 할 수 있습니다. 이후 에디터를 닫고 프로젝트 폴더의 PowerShell에서:

```powershell
.\Tools\Validate.ps1 -Build
```

자동 Play Mode 검사에는 탑승 위치·속도 조건, 추격 중 도착 금지, 탈출 카운트 초기화, 목적지 정차, 성공·해금 저장, 일시 정지, 시간 초과, 파손, 체포, 실제 Rigidbody 전진·브레이크, 경찰 AI 이동, 마지막 스테이지 경찰 수, 로그 파일 생성을 포함합니다. 자동 검사는 별도 제품 이름 `GetawayDriver-AutomatedTests`를 사용합니다.

통과 시 `Docs/Validation/unity-smoke-tests.txt`가 생성됩니다. 이번 작업에서는 그 파일이 생성되지 않았습니다. 실제 주행 감각과 경찰 난이도는 자동 검사 후 직접 플레이하면서 조정해야 합니다.
