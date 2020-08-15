using Godot;
using System;

public class HitboxDrawer : CollisionShape2D
{
    [Export] private NodePath parentPath;
    [Export] private bool enabled;

    private Player parent;

    public override void _Ready() {
        this.parent = GetNode(parentPath) as Player;
        this.SetProcess(this.enabled);
    }

    public override void _Process(float delta) {
        (Shape2D hitbox, Transform2D xform) = this.parent.GetHitbox();

        this.GlobalTransform = xform;
        
        this.Shape = hitbox;
    }
}
