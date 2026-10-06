using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MyFirstCharacter.MyFirstCharacterCode.FGOCore;

namespace MyFirstCharacter.MyFirstCharacterCode.Powers;

/// <summary>追猎：本回合每次受到攻击，使施放者获得宝具值（标记在敌人身上，回合结束移除）。</summary>
public class HuntMarkPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // Hook.AfterDamageReceived 会遍历战斗里**所有** hook 监听者派发（runState.IterateHookListeners），
        // 不是只发给受伤者身上的能力。所以必须自己校验「受伤的是不是挂着这个标记的那只」——
        // 卡面写的是「本回合每对**其**造成一次伤害」。少了这一句，打任何敌人都给宝具值。
        // （同类写法 CasterMagicDefensePower 里就有这个校验。）
        if (target != Owner) return;
        // 给施放者而不是给实际打伤害的人：多人下队友打了这只敌人，宝具值也该归放标记的人。
        Creature? gainer = Applier ?? dealer;
        if (gainer == null) return;
        await NpCharge.Gain(gainer, (int)Amount);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy)
            await PowerCmd.Remove(this);
    }
}
