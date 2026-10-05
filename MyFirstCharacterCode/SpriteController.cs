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
    // Track which sprites already have our signal handler connected
    private static readonly HashSet<AnimatedSprite2D> _connectedSprites = new();

    public static void ApplyFrames(Creature creature, string framesPath)
    {
        var combatRoom = NCombatRoom.Instance;
        if (combatRoom == null) return;

        var creatureNode = combatRoom.GetCreatureNode(creature);
        if (creatureNode == null) return;

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
