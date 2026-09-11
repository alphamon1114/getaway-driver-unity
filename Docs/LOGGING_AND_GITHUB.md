# 로그와 GitHub

## 플레이 기록

`Application.persistentDataPath` 아래에 기록합니다. Windows 기본 위치는 `%USERPROFILE%\AppData\LocalLow\GetawayPrototype\GetawayDriver\`입니다.

- `Logs/<UTC시각>_<세션ID>.jsonl`: 이벤트와 1초 간격 차량 상태.
- `Logs/<동일세션>_console.txt`: 해당 세션의 Unity Console 메시지와 오류 스택.
- `progress.json`: 최고 해금 스테이지, 누적 성공 횟수.
- `progress.json.bak`: 기존 진행 저장의 백업. 자동 복구는 구현하지 않았으므로 손상 시 기존 파일을 별도 보관하고 백업을 복원할 수 있습니다.

JSONL 필드는 `utc`, `session`, `type`, `stage`, `elapsed`, `x`, `z`, `speedKph`, `health`, `detail`입니다. 세션 시작·종료, 스테이지 로드·시작, 탑승, 추격 시작·해제, 피해, 복구, 일시 정지·재개, 성공·실패, 진행 저장 성공·실패를 기록합니다. 세션마다 별도 파일을 만들고 각 행을 즉시 flush합니다. 디스크 쓰기가 실패하면 경고를 내고 로그 기능을 중단하며 게임은 계속 실행합니다.

이 기록은 이벤트와 주기적 상태 로그입니다. 모든 프레임·모든 키 입력·화면 영상이나 결정론적 리플레이는 포함하지 않습니다. 강제 종료 시 마지막 종료 이벤트는 없을 수 있습니다. 자동 업로드나 자동 삭제는 하지 않으며 장기 플레이 시 오래된 로그를 별도로 보관하세요.

## 개발 기록

- 소스 변경은 Git 커밋, 의사결정과 검증 결과는 `Docs/DEVLOG.md`에 기록합니다.
- `.meta` 파일은 꼭 함께 커밋합니다. `Library`, `Temp`, `Logs`, `Builds`는 제외합니다.
- FBX·GLB·이미지·음원은 `.gitattributes`의 Git LFS 규칙에 포함되어 있습니다. [Git LFS 공식 안내](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-git-large-file-storage)에 따라 실제 큰 파일 대신 참조 파일이 Git에 저장됩니다.
- 개발 중인 프로젝트는 비공개 저장소로 시작하고, 새 기능은 `feature/car-handling` 같은 브랜치에서 작업하는 방식을 권장합니다.
- 런타임 로그는 자동 Git 커밋하지 않습니다. 재현에 필요한 파일만 검토 후 이슈나 별도 보관소에 첨부하세요.

## 저장소

이 프로젝트의 비공개 원격 저장소는 [alphamon1114/getaway-driver-unity](https://github.com/alphamon1114/getaway-driver-unity)입니다. 새 PC에서는 이 저장소를 Clone한 뒤 Unity Hub로 엽니다.

## 다른 원격 저장소를 연결하는 경우

GitHub에서 `getaway-driver-unity`라는 빈 **Private** 저장소를 만들고, 프로젝트 폴더의 터미널에서 아래를 실행합니다. `OWNER`는 본인 계정으로 바꿉니다. 이미 origin이 있으면 `git remote -v`로 확인 후 필요할 때 `git remote set-url origin ...`을 사용합니다.

```powershell
git lfs install --local
git remote add origin https://github.com/OWNER/getaway-driver-unity.git
git push -u origin main
```

이 문서의 명령은 사용 안내이며 실제 원격 생성·업로드 상태는 작업 완료 보고 및 개발 로그에서 구분합니다. 로그인은 GitHub/Git Credential Manager의 인증 창에서 진행하고 토큰을 파일에 적지 않습니다.

## 변경을 남기는 기본 흐름

```powershell
git status
git switch -c feature/car-handling
git add Assets Docs ProjectSettings Packages
git commit -m "feat: improve handling and document tuning"
git push -u origin feature/car-handling
git log --date=iso --stat
```

빌드/검사 자동화는 `Tools/Validate.ps1`로 같은 Unity 버전이 설치된 PC에서 실행할 수 있습니다. GitHub Actions에서 Unity 빌드를 돌리려면 라이선스/러너 구성이 추가로 필요하므로 이번 프로젝트에는 자동 원격 빌드가 설정되어 있지 않습니다.
