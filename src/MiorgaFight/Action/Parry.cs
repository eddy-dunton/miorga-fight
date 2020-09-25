using Godot;
using System;

namespace MiorgaFight {

public class Parry : Action
{
    //First frame that this parry blocks attacks on
    [Export] public int frameStart;

    //Last frame this parry blocks attacks on 
    [Export] public int frameEnd;

    [Export] public int knockback;

    //Shape of this parry's hitbox
    [Export] public Shape2D hitbox;

    //Position of this parry's hitbox, relative to the sprite of this Parry 
    [Export] public Vector2 hitboxoffset;

    //Sound to played when the parry is successful
    [Export] public SoundEffect successSound;

    public Parry() {
        this.type = Action.Type.PARRY;
    }

    public override void Start(Player player) {
        player.parry = this;
        player.parrySuccessful = false;
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
        player.parrySuccessful = false;
    }

    //Checks if an a players attack would have been parried by this
    public bool Check(Player attacker, Player parrier, Attack attack) {
        //Check that the correct frames are being used
        if (! (parrier.nodeAnimateSprite.Frame >= parrier.parry.frameStart) &&
                (parrier.nodeAnimateSprite.Frame <= parrier.parry.frameEnd)) 
            return false;
        
        //Parry hitbox xform
        Transform2D pXform = parrier.nodeAnimateSprite.GlobalTransform.Translated(this.hitboxoffset);

        //Attack hitbox xform
        Transform2D aXform = attacker.nodeAnimateSprite.GlobalTransform.Translated(attack.hitboxoffset);

        return this.hitbox.Collide(pXform, attack.hitbox, aXform);
    }

    public void Success(Player player) {
        player.nodeSparks.Emitting = true;
        player.parrySuccessful = true;
        //Play sound if it exists
        if (this.successSound != null) player.PlaySfx(this.successSound);
    }
}}
