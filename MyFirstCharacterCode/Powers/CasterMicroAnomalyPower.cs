using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Powers;
using MyFirstCharacter.MyFirstCharacterCode.FGOCore;

namespace MyFirstCharacter.MyFirstCharacterCode.Powers;

/// <summary>Micro Anomaly: at end of turn, switch form, then remove self. 每层切换一次。</summary>
public class CasterMicroAnomalyPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner == null || !participants.Contains(Owner)) return;
        var c = Owner;

        int times = (int)Amount;
        for (int i = 0; i < times; i++)
        {
            if (c.HasPower<CasterFormOnePower>())
            {
                await PowerCmd.Remove(c.GetPower<CasterFormOnePower>()!);
                await PowerCmd.Apply<CasterFormTwoPower>(choiceContext, c, 1, c, null, false);
                await PowerCmd.Apply<StrengthPower>(choiceContext, c, CasterFormValues.FormTwoStrength(c), c, null, false);
                await PowerCmd.Apply<VulnerablePower>(choiceContext, c, CasterFormValues.FormTwoVulnerable(c), c, null, false);
            }
            else if (c.HasPower<CasterFormTwoPower>())
            {
                await PowerCmd.Remove(c.GetPower<CasterFormTwoPower>()!);
                await PowerCmd.Apply<CasterFormOnePower>(choiceContext, c, 1, c, null, false);
                NpCharge.Gain(c, CasterFormValues.FormOneNp(c));
                await PowerCmd.Apply<WeakPower>(choiceContext, c, 1, null, null, false);
            }
        }

        await PowerCmd.Remove(this);
    }
}
