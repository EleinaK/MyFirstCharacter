using BaseLib.Abstracts;
using MyFirstCharacter.MyFirstCharacterCode.Extensions;
using Godot;

namespace MyFirstCharacter.MyFirstCharacterCode;

public class CasterCardPool : CustomCardPoolModel
{
    public override string Title => "MyFirstCharacter";
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
    public override float H => 0.6f;
    public override float S => 0.5f;
    public override float V => 1f;
    public override Color DeckEntryCardColor => new("4a90d9");
    public override bool IsColorless => false;
}
