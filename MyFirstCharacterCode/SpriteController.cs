using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MyFirstCharacter.MyFirstCharacterCode;

/// <summary>
/// Applies SpriteFrames to the character's AnimatedSprite2D node.
/// After a non-looping animation finishes, auto-returns to idle.
/// </summary>
public static class SpriteController
{
    /// <summary>战斗模型缩放倍率。原版遗物「大蘑菇」用的是 1.5f，观感过大；这里两个形态统一取 1.4f。</summary>
    public const float CombatScale = 1.4f;

    // Track which sprites already have our signal handler connected
    private static readonly HashSet<AnimatedSprite2D> _connectedSprites = new();

    public static void ApplyFrames(Creature creature, string framesPath)
    {
        var combatRoom = NCombatRoom.Instance;
        if (combatRoom == null) return;

        var creatureNode = combatRoom.GetCreatureNode(creature);
        if (creatureNode == null) return;

        // 缩放整个 creature node —— 用引擎自带的 NCreature.ScaleTo（原版「大蘑菇」遗物同款 API），
        // 不要直接改 Sprite.Scale：ScaleTo 会同步 _tempScale，而 UpdateBounds 是按
        // Bounds.Size * Visuals.Scale / _tempScale 反推逻辑尺寸的，两者不同步会让
        // 命中框和敌人意图图标跟身体错位。
        // creature node 每场战斗重建，所以每次应用帧时重新施加一次（大蘑菇也是每进房间重调 Grow()）。
        creatureNode.ScaleTo(CombatScale, 0.0);

        // Try to find the AnimatedSprite2D
        AnimatedSprite2D? sprite = null;

        sprite = FindChildRecursive(creatureNode, "Sprite") as AnimatedSprite2D;
        if (sprite == null)
            sprite = FindFirstAnimatedSprite(creatureNode);

        if (sprite == null) return;

        // Load and apply SpriteFrames
        if (!ResourceLoader.Exists(framesPath)) return;
        var frames = GD.Load<SpriteFrames>(framesPath);
        if (frames == null) return;

        sprite.SpriteFrames = frames;

        // Connect animation_finished signal (only once per sprite)
        if (_connectedSprites.Add(sprite))
        {
            sprite.AnimationFinished += () =>
            {
                if (sprite.Animation != "idle" && sprite.Animation != "die")
                    sprite.Play("idle");
            };
        }

        sprite.Play("idle");
    }

    // Clean up dead sprites (call periodically or from mod unload)
    public static void Cleanup()
    {
        _connectedSprites.RemoveWhere(s => !GodotObject.IsInstanceValid(s));
    }

    private static Node? FindChildRecursive(Node parent, string name)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child.Name == name) return child;
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static AnimatedSprite2D? FindFirstAnimatedSprite(Node parent)
    {
        if (parent is AnimatedSprite2D anim) return anim;
        foreach (var child in parent.GetChildren())
        {
            var found = FindFirstAnimatedSprite(child);
            if (found != null) return found;
        }
        return null;
    }
}
