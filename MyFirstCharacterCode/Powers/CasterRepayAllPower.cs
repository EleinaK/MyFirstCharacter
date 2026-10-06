using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MyFirstCharacter.MyFirstCharacterCode.Powers;

/// <summary>回报失去的一切：回合开始时消耗抽牌堆顶的牌（每层1张）。</summary>
public class CasterRepayAllPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        int times = (int)Amount;
        for (int i = 0; i < times; i++)
        {
            var top = PileType.Draw.GetPile(player).Cards.FirstOrDefault();
            if (top == null) break;
            await CardCmd.Exhaust(choiceContext, top);
        }
    }
}
