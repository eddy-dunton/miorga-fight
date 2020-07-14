using Godot;
using System;

public class Attack : Action {
    [Export] public Shape2D hitbox;

    [Export] public Vector2 hitboxoffset;

    [Export] public int hitframe;

    [Export] public bool halt;

    public Attack() {
        this.type = Action.Type.ATTACK;
    }

    public override void Start(Player player) {
        player.attack = this;
        player.nodeAnimateSprite.Play(this.animation);
        player.ChangeState(Player.State.ATTACK);
    }

    //Standard attack code
    public void Hit(Player player) {
        //Create transform for collision shape
		Transform2D xform = player.nodeAnimateSprite.GlobalTransform.Translated(this.hitboxoffset);
		//Check for collision with other player
		if (this.hitbox.Collide(xform, player.nodeEnemy.nodeCollision.Shape, 
				player.nodeEnemy.nodeCollision.GlobalTransform)) {
			//Check for parry
			if (! ((player.nodeEnemy.state == Player.State.PARRY) && 
					(player.nodeEnemy.nodeAnimateSprite.Frame >= player.nodeEnemy.parry.frameStart) &&
					(player.nodeEnemy.nodeAnimateSprite.Frame <= player.nodeEnemy.parry.frameEnd))) {
                player.nodeEnemy.Hurt(10, this.halt);
			} else {
                //Attack parried
                player.Parried(this, player.nodeEnemy.parry);
            }
		}
    }

    public new void Cut(Player player) {
        base.Cut(player);

        player.attack = null;
    }

    //Called to bring the attac to an end
    public new void End(Player player) {
        base.End(player);

        player.attack = null;
    }
}
