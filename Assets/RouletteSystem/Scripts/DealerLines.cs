using System.Collections.Generic;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 딜러별 짧은 반응 대사와 층간 안내 방송(데이터). 표면은 친절하고 내용은 미묘하게 위협적인 블랙 코미디 톤(CLAUDE.md 3·4장).
    /// 어느 순간에 어떤 줄을 띄울지는 전투 컨트롤러가 정하고, 여기는 문구만 맡는다.
    /// </summary>
    public sealed class DealerLines
    {
        public string Opening;
        /// <summary>플레이어가 하우스 몫에 걸렸을 때.</summary>
        public string PlayerHouseCut;
        /// <summary>플레이어가 큰 연쇄·잭팟 라인으로 판돈을 크게 불렸을 때.</summary>
        public string BigCombo;
        /// <summary>플레이어의 큰 CASH OUT(피해 8 이상).</summary>
        public string BigCashOut;
        public string SmallCashOut;
        public string DealerCashOut;
        public string DealerHouseCut;
        public string Hijacked;
        public string JackpotHijacked;
        public string Nudged;
        public string DealerLoses;
        public string CleanSwept;

        private static readonly DealerLines Default = new DealerLines
        {
            Opening = "오늘의 하우스 룰은 게시판에 붙어 있습니다.",
            PlayerHouseCut = "하우스 몫입니다. 테이블 위의 칩은 저희가 정리하겠습니다.",
            BigCombo = "손님, 확률을 직접 수정하시는 건 약관 위반입니다.",
            BigCashOut = "크게 가져가시네요. 장부에 기록해 두겠습니다.",
            SmallCashOut = "정산 완료. 다음 판도 기대하겠습니다.",
            DealerCashOut = "정산하겠습니다. 손님 칩에서 받아 두었어요.",
            DealerHouseCut = "...하우스는 원래 저희 편인데요.",
            Hijacked = "양도 처리가 완료됐습니다. 반환은 불가능합니다.",
            JackpotHijacked = "JACKPOT까지요? 보안팀을 불러드리겠습니다.",
            Nudged = "방금 룰렛이... 저절로 움직였네요. 그렇다고 해 두죠.",
            DealerLoses = "축하드립니다. 다음 테이블도 화이팅~",
            CleanSwept = "...제 룰렛이 텅 비었네요."
        };

        private static readonly Dictionary<string, DealerLines> ByDealer = new Dictionary<string, DealerLines>
        {
            ["토끼"] = new DealerLines
            {
                Opening = "어서오세요, 첫 손님이시네요. 걸고, 돌리고, 적당할 때 터뜨리세요.",
                PlayerHouseCut = "앗, 하우스 몫! 괜찮아요, 원래 다들 한 번씩 잃어요.",
                BigCombo = "와아, 이어진 칸이 한꺼번에! 그게 연쇄예요.",
                BigCashOut = "아야야... 그렇게 크게 터뜨리시면 제 귀가 쫑긋해요.",
                SmallCashOut = "정산 완료! 작게라도 챙기는 거, 좋은 습관이에요.",
                DealerCashOut = "제 차례였죠? 손님 칩에서 살짝 받아 갈게요.",
                DealerHouseCut = "어라, 저도 걸렸네요. 하우스는 공평하답니다.",
                Hijacked = "제 칸을 가져가셨네요! 이제 손님 거예요.",
                JackpotHijacked = "「서비스」까지! 위에 보고는... 안 할게요. 아마도.",
                Nudged = "방금 룰렛 밀었죠? 처음이니까 못 본 걸로 할게요.",
                DealerLoses = "다음 층 딜러들은 저만큼 친절하지 않아요. 화이팅~",
                CleanSwept = "제 룰렛이 텅 비었어요... 당근이라도 드릴까요?"
            },
            ["여우"] = new DealerLines
            {
                Opening = "여우입니다. 보험은 두둑이 들어 두었으니 마음껏 덤비세요.",
                PlayerHouseCut = "어머, 증발. 욕심은 늘 하우스 편이에요.",
                BigCombo = "그 판돈, 제 보험으로 다 막을 수 있을까요? 궁금하네요.",
                BigCashOut = "제 보험을 뚫으셨어요? 약관을 다시 읽어 봐야겠네요.",
                SmallCashOut = "작게, 또 작게. 그렇게 제 장부를 갉아먹을 생각이세요?",
                DealerCashOut = "허풍인지 아닌지는 정산서가 말해 주죠.",
                DealerHouseCut = "...방금 건 연습이었어요.",
                Hijacked = "제 칸을요? 반납 기한은 영원이겠죠.",
                JackpotHijacked = "「허풍」을 가져가시면 저는 뭘로 거짓말하죠?",
                Nudged = "룰렛이 미끄러졌네요. 바닥 청소를 다시 시켜야겠어요.",
                DealerLoses = "이번엔 제가 속았네요. 다음 테이블 딜러에겐 비밀로 해 주세요.",
                CleanSwept = "꼬리까지 털렸네요. 이건 장부에 못 적겠어요."
            },
            ["고양이"] = new DealerLines
            {
                Opening = "냐... 고양이야. 난 느긋하게 키워서 한 방에 터뜨려.",
                PlayerHouseCut = "증발했네. 꾹꾹이로 위로해 줄까?",
                BigCombo = "오, 그 판돈 맛있어 보이는데. 배율은 내 전문인데 말이야.",
                BigCashOut = "냐앗! 낮잠 자다 꼬리를 밟혔어.",
                SmallCashOut = "그 정도는 간지러워.",
                DealerCashOut = "기다린 보람이 있네. 크게 받아 갈게.",
                DealerHouseCut = "...안 봤어. 아무것도 안 봤어.",
                Hijacked = "내 칸 가져가도 돼. 대신 츄르 값은 따로야.",
                JackpotHijacked = "「더블 다운」은 내 낮잠 자금이었는데...",
                Nudged = "방금 룰렛 툭 쳤지? 그건 원래 고양이 특기인데.",
                DealerLoses = "졌다냐. 다음 층에 가서 내 칩 자랑하지 마.",
                CleanSwept = "텅 빈 룰렛... 박스처럼 들어가 앉아야겠다."
            },
            ["까마귀"] = new DealerLines
            {
                Opening = "까악. 반짝이는 건 다 내 거야. 선공도 내가 가져갈 거고.",
                PlayerHouseCut = "증발한 칩, 바닥에 떨어진 건 내가 주울게.",
                BigCombo = "반짝반짝하네. 그 판돈, 둥지에 딱 어울리겠어.",
                BigCashOut = "까악! 내 수집품이...!",
                SmallCashOut = "쪼잔하긴. 반짝이지도 않네.",
                DealerCashOut = "선공은 내 거, 칩도 내 거.",
                DealerHouseCut = "까악... 하우스가 내 걸 훔쳐 갔어.",
                Hijacked = "도둑이야! ...나도 도둑이지만.",
                JackpotHijacked = "「선불」은 내가 제일 아끼던 거라고!",
                Nudged = "방금 바늘이 움직였어. 나만 봤어. 나만.",
                DealerLoses = "둥지가 털렸어. 다음 층에선 조심해, 다들 나보다 욕심쟁이야.",
                CleanSwept = "반짝이는 게 하나도 안 남았어... 까악."
            },
            ["매니저"] = new DealerLines
            {
                Opening = "층 매니저입니다. 손님의 그간 활약, 보고서로 잘 읽었습니다.",
                PlayerHouseCut = "하우스 몫은 회사 방침입니다. 저도 어쩔 수 없어요.",
                BigCombo = "그 연쇄, 감사팀에 회부하겠습니다.",
                BigCashOut = "손실 보고서를 쓰게 만드시는군요.",
                SmallCashOut = "그 정도는 운영비로 처리하겠습니다.",
                DealerCashOut = "이것이 하우스 엣지입니다, 손님.",
                DealerHouseCut = "...이 건은 회의록에서 빼 주세요.",
                Hijacked = "규칙을 훔치시다니. 인사팀에서 연락드릴 겁니다.",
                JackpotHijacked = "「VIP 보호막」은 VIP 전용입니다. ...아, 이제 손님이 VIP군요.",
                Nudged = "룰렛 조작은 약관 위반입니다. 증거가 있다면요.",
                DealerLoses = "탈출구는 저쪽입니다. 당첨 확률은 여전히 공개하지 않습니다.",
                CleanSwept = "제 룰렛이... 전부 봉인? 사직서를 써야겠군요."
            }
        };

        /// <summary>층간 안내 방송(문 선택 화면 맨 위). 층 번호(0부터)로 고른다.</summary>
        private static readonly string[] FloorAnnouncements =
        {
            "안내 방송: 첫 테이블은 무료 체험입니다. 미래의 행운은 나중에 청구됩니다.",
            "안내 방송: 손님의 행운 잔고가 확인되었습니다. 원하시는 딜러를 고르세요.",
            "안내 방송: 연쇄를 지나치게 잘 만드는 손님은 보안팀이 지켜보고 있습니다.",
            "안내 방송: 다음 층부터 딜러들이 진지해집니다. 진지함은 무료입니다.",
            "안내 방송: 매니저실이 열렸습니다. 탈출구는 존재합니다. 당첨 확률은 공개하지 않습니다."
        };

        public static DealerLines For(string dealerName)
        {
            foreach (KeyValuePair<string, DealerLines> pair in ByDealer)
            {
                if (dealerName.Contains(pair.Key)) return pair.Value;
            }

            return Default;
        }

        public static string FloorAnnouncement(int floorIndex)
        {
            if (FloorAnnouncements.Length == 0) return "";
            return FloorAnnouncements[System.Math.Min(System.Math.Max(0, floorIndex), FloorAnnouncements.Length - 1)];
        }
    }
}
