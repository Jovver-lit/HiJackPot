# pdf — 공유·보관용 PDF 문서

팀 밖에 전달하거나 한 번에 읽기 좋게 묶은 PDF와 그 원본(HTML)이다. 내용의 근거는 `Docs/adr`, `Docs/design`, `Docs/playtest`, 규칙 코드(`Assets/RouletteSystem/Core`)에 있고, 여기는 그것을 정리한 사본이다.

| 파일 | 내용 |
|---|---|
| `HIJACKPOT-1차-MVP-개발-검증-보고서.pdf` | 1차 MVP 전체 기록: 정체성, 구현된 시스템, 파트별 커밋 타임라인, ADR 0001~0007, 밸런스 시뮬레이션, 직접 플레이테스트 피드백→원인→조치, 검증, MVP 점검표, 남은 과제 |
| `HIJACKPOT-전투-단계-순서도.pdf` | 런 → 전투 → 라운드 → 턴 → SPIN·착지의 처리 순서와 순서도, 착지 발동 순서 표, CASH OUT 정산, HIJACK·역탈취, 딜러 턴, 수치 부록 |
| `build-pdf.sh` | 원본 HTML을 Chrome 헤드리스로 A4 PDF로 만든다(순서도는 mermaid CDN 사용, 인터넷 필요) |
| `src/battle-flow.html`, `src/mvp-report.html` | PDF 원본. 내용을 고칠 때는 여기를 고치고 `build-pdf.sh`를 다시 실행한다 |
| `src/print.css`, `src/mermaid-fit.js` | 두 원본의 공용 인쇄 스타일, 순서도를 페이지 크기에 맞추는 스크립트 |
| `src/img/` | 보고서에 넣은 게임 스크린샷(2026-10-01 그레이박스 빌드) |

규칙: 규칙이나 수치가 바뀌면 원본 HTML과 코드를 함께 고치고 PDF를 다시 만든다. PDF는 Git LFS로 관리된다.
