using TMPro;
using UnityEngine;

namespace RouletteLike.Roulette
{
    /// <summary>
    /// 전투 규칙을 소유하지 않고, 현재 전투 단계와 강조 상태만 표시하는 UI 전용 뷰입니다.
    /// 향후 글로우/페이즈 전환 애니메이션은 각 EffectRoot에서 교체할 수 있습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattlePresentationUI : MonoBehaviour
    {
        public enum Phase
        {
            Prepare,
            Spin,
            Resolve,
            Dealer
        }

        [Header("Phase Indicator")]
        [SerializeField] private TMP_Text prepareText;
        [SerializeField] private TMP_Text spinText;
        [SerializeField] private TMP_Text resolveText;
        [SerializeField] private TMP_Text dealerText;

        [Header("Active Areas")]
        [SerializeField] private UnityEngine.UI.Image playerRouletteGlow;
        [SerializeField] private UnityEngine.UI.Image dealerRouletteGlow;
        [SerializeField] private UnityEngine.UI.Image houseRuleFrame;

        [Header("Palette")]
        [SerializeField] private Color activeColor = new Color32(224, 168, 52, 255);
        [SerializeField] private Color inactiveColor = new Color32(112, 104, 125, 255);
        [SerializeField] private Color playerGlowColor = new Color32(57, 190, 198, 90);
        [SerializeField] private Color dealerGlowColor = new Color32(220, 72, 78, 90);
        [SerializeField] private Color houseRuleNormalColor = new Color32(125, 91, 38, 255);
        [SerializeField] private Color houseRuleHighlightColor = new Color32(255, 205, 82, 255);

        public Phase CurrentPhase { get; private set; }

        public void SetPhase(Phase phase)
        {
            CurrentPhase = phase;
            SetPhaseColor(prepareText, phase == Phase.Prepare);
            SetPhaseColor(spinText, phase == Phase.Spin);
            SetPhaseColor(resolveText, phase == Phase.Resolve);
            SetPhaseColor(dealerText, phase == Phase.Dealer);

            SetPlayerRouletteActive(phase == Phase.Prepare || phase == Phase.Spin || phase == Phase.Resolve);
            SetDealerRouletteActive(phase == Phase.Dealer);
        }

        public void SetPlayerRouletteActive(bool active)
        {
            if (playerRouletteGlow == null)
            {
                return;
            }

            playerRouletteGlow.gameObject.SetActive(active);
            playerRouletteGlow.color = playerGlowColor;
        }

        public void SetDealerRouletteActive(bool active)
        {
            if (dealerRouletteGlow == null)
            {
                return;
            }

            dealerRouletteGlow.gameObject.SetActive(active);
            dealerRouletteGlow.color = dealerGlowColor;
        }

        public void SetHouseRuleHighlighted(bool highlighted)
        {
            if (houseRuleFrame != null)
            {
                houseRuleFrame.color = highlighted ? houseRuleHighlightColor : houseRuleNormalColor;
            }
        }

        private void SetPhaseColor(TMP_Text label, bool active)
        {
            if (label != null)
            {
                label.color = active ? activeColor : inactiveColor;
            }
        }
    }
}
