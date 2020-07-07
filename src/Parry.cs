using Godot;
using System;

public class Parry : Action
{
    //First frame that this parry blocks attacks on
    [Export] public int frameStart;

    //Last frame this parry blocks attacks on 
    [Export] public int frameEnd;

    [Export] public int knockback;

    public Parry() {
        this.type = Action.Type.PARRY;
    }

    public override void Start(Player player) {
        player.parry = this;
        player.nodeAnimateSprite.Play(this.animation);
        player.ChangeState(Player.State.PARRY);
    }

    public new void Cut(Player player) {
        base.Cut(player);

        player.parry = null;
    }

    public new void End(Player player) {
        base.End(player);

		player.parry = null;
    }
}
