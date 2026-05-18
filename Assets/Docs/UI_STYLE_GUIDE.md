# CardAdventure UI 구현 기준

## 기준 이미지 해석

- 전체 톤은 검은 RPG 대화창, 얇은 금색/청색 픽셀 테두리, 흰색 고대비 텍스트를 기본으로 한다.
- 대화 UI는 화면 하단 전체 폭을 사용하고, 왼쪽에 초상화 프레임을 둔다.
- 본문 박스는 검은 배경 위에 흰색 한글 텍스트를 표시한다. 텍스트는 장식보다 판독성을 우선한다.
- 선택지, 상점, 보상, 전투 HUD도 같은 창 문법을 사용한다. 기능별 색상은 최소한의 보조색으로만 구분한다.

## 공통 색상

- 창 배경: 거의 검정 `ClassicPixelUiTheme.WindowBlack`
- 내부 본문: 순검정 `ClassicPixelUiTheme.InnerBlack`
- 주 테두리: 금색 `ClassicPixelUiTheme.Gold`
- 보조 테두리/선택 강조: 청색 `ClassicPixelUiTheme.Blue`, `ClassicPixelUiTheme.Cyan`
- 기본 텍스트: 흰색에 가까운 `ClassicPixelUiTheme.Text`
- 보조 텍스트: 회색 `ClassicPixelUiTheme.MutedText`
- HP/위험: 빨강 `ClassicPixelUiTheme.Danger`
- 에너지/골드: 금색 `ClassicPixelUiTheme.Energy`

## 레이아웃 규칙

- 모든 주요 패널은 검은 배경, 2px 내외 Outline, 안쪽 여백 12~24px를 기본으로 한다.
- 대화창 높이는 156px 기준이며, 초상화 프레임은 126x126px을 기본으로 한다.
- 대화 본문은 초상화가 있을 때 왼쪽 여백 176px, 없을 때 44px부터 시작한다.
- 버튼은 별도 밝은 면을 쓰지 않고 검은 패널 + 금색 테두리 + 흰 텍스트로 구성한다.
- 카드, 초상화, 캐릭터, 적, 상태 아이콘처럼 실제 그림을 보여주는 Image는 검은색으로 덮지 않는다.

## 구현 규칙

- 신규 uGUI 런타임 UI는 `ClassicPixelUiTheme`의 색상과 `ApplyPanel`, `ApplyText`, `ApplyButton`을 사용한다.
- 에디터에서 생성되는 UI는 `CardAdventure/UI/Apply Classic Pixel UI To Project` 도구를 실행해 씬/프리팹에 동일 기준을 적용한다.
- 대화 데이터에는 선택 초상화 `speakerPortrait`를 연결할 수 있다. 없으면 초상화 슬롯은 숨기고 본문 영역을 넓힌다.
- 서드파티 에셋 폴더는 직접 수정하지 않는다. 스타일 적용은 자체 씬, 프리팹, 스크립트에서만 수행한다.
