#!/bin/bash
# Docs/pdf/src/*.html 원본을 Chrome 헤드리스로 A4 PDF로 만든다(mermaid 순서도는 CDN에서 불러와 렌더링하므로 인터넷 필요).
# 사용: Docs/pdf/build-pdf.sh   (macOS, Google Chrome 설치 필요)
set -euo pipefail
cd "$(dirname "$0")"
CHROME="/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
render() {
  local src="$1" out="$2"
  "$CHROME" --headless=new --disable-gpu --no-pdf-header-footer --virtual-time-budget=15000 \
    --print-to-pdf="$out" "file://$PWD/src/$src" 2>/dev/null
  echo "만듦: $out"
}
render battle-flow.html "HIJACKPOT-전투-단계-순서도.pdf"
render mvp-report.html "HIJACKPOT-1차-MVP-개발-검증-보고서.pdf"
