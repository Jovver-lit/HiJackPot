using System.Runtime.CompilerServices;

// 테스트 어셈블리가 칩·판돈 같은 내부 상태를 직접 맞춰 경계 상황(예: 테이블 사용료로 파산)을 만들 수 있게 한다.
[assembly: InternalsVisibleTo("HiJackPot.Core.Tests")]
