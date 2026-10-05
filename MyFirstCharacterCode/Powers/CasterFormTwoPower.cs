using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MyFirstCharacter.MyFirstCharacterCode.Powers;

/// <summary>Form 2 (attack form): each card played applies 1 Vulnerable to all enemies.</summary>
public class CasterFormTwoPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldReceiveCombatHooks => true;

    public const string FramesPath = "res://MyFirstCharacter/character/artoria_frames_avalon.tres";

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (Owner?.CombatState == null) return;
        foreach (var enemy in Owner.CombatState.Enemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<VulnerablePower>(context, enemy, 1, Owner, cardPlay.Card, false);
    }
}
