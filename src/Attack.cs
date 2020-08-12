using Godot;
using System;

public class Attack : Action {
    [Export] public Shape2D hitbox;

    [Export] public Vector2 hitboxoffset;

    [Export] public int hitframe;

    [Export] public bool halt;

    [Export] public int damage;

    //True if the attack requires a successful parry in order to be pulled off
    //For this to work correctly triggerAnimation must be a parry, else this will not work
    [Export] private bool requiresSuccessfulParry;

    public Attack() {
        this.type = Action.Type.ATTACK;
    }

    public override void Start(Player player) {
        //Cancels if this attack requires parry but the player has not parried
        if (this.requiresSuccessfulParry && !player.parrySuccessful) return;

        player.attack = this;
        player.nodeAnimateSprite.Play(this.animation);
        player.ChangeState(Player.State.ATTACK);
    }

    //Standard attack code
    public void Hit(Player player) {
        //Calculate transform for collision shape
		Transform2D thisXform = player.nodeAnimateSprite.GlobalTransform.Translated(this.hitboxoffset);
		
        //Just to shorten things
        PlayerAnimation enemySprite = player.nodeEnemy.nodeAnimateSprite;

        //No hitbox, no point trying
        if (enemySprite.Current().hitboxOffset.Length == 0) return;

        (Shape2D enHitbox, Transform2D enXform) = player.nodeEnemy.nodeAnimateSprite.GetHitbox(); 

        //Check for collision with other player
		if (this.hitbox.Collide(thisXform, enHitbox, enXform)) {
			//Check for parry
			if (player.nodeEnemy.state == Player.State.PARRY && 
                    player.nodeEnemy.parry.Check(player, player.nodeEnemy, this)) {
                //Attack parried
                player.Parried(this, player.nodeEnemy.parry);
            }
			else { 
                //Attack not parried
                player.nodeEnemy.Hurt(damage, this.halt);
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
