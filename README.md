# SWUtils

Unity 프로젝트에서 공통으로 사용하는 런타임 및 에디터 유틸리티 패키지입니다.

## Git URL 설치

Unity Package Manager에서 다음 순서로 추가합니다.

1. Unity 상단 메뉴에서 `Window > Package Manager`를 엽니다.
2. 좌측 상단 `+` 버튼을 누릅니다.
3. `Add package from git URL...`을 선택합니다.
4. 이 저장소의 Git URL을 입력합니다.

브랜치 또는 태그를 고정해서 설치하려면 URL 뒤에 `#브랜치명` 또는 `#태그명`을 붙입니다.

```text
https://github.com/사용자명/저장소명.git#1.0.0
```

## 의존 패키지

`package.json`에서 Unity 패키지 의존성을 자동으로 설치합니다.

- Input System
- Localization
- TextMeshPro
- Unity UI

DOTween, Google Play Games, Steamworks.NET은 Unity Package Manager에서 직접 설치할 수 없는 외부 라이브러리일 수 있으므로, 해당 기능을 사용하는 프로젝트에서 먼저 설치해야 합니다.

## 선택 정의 심볼

클라우드 저장 기능에서 외부 라이브러리를 사용할 때 다음 정의 심볼을 프로젝트에 추가합니다.

- `SW_GOOGLEPLAY_ENABLE`: Android에서 Google Play Games 저장 기능을 사용합니다.
- `SW_STEAMWORKS_NET`: Standalone에서 Steamworks.NET 저장 기능을 사용합니다.
