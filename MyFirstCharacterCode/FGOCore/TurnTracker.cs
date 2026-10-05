using MegaCrit.Sts2.Core.Entities.Creatures;

namespace MyFirstCharacter.MyFirstCharacterCode.FGOCore;

/// <summary>本回合内事件的轻量追踪（如「本回合消耗过牌」）。由起始遗物标记/清理。</summary>
public static class TurnTracker
{
    private static readonly HashSet<Creature> _exhaustedThisTurn = new();

    public static void MarkExhausted(Creature c) => _exhaustedThisTurn.Add(c);

    public static bool HasExhausted(Creature c) => _exhaustedThisTurn.Contains(c);

    public static void ClearExhausted(Creature c) => _exhaustedThisTurn.Remove(c);
}
