using Godot;
using System;
using System.Collections.Generic;

public class PlayerAnimation : AnimatedSprite {

    [Export] Dictionary<String, AnimationData> data;

    private Player parent;

    //Data for the current animation, will never be null
    private AnimationData current;
    public AnimationData Current() {return this.current;}

    public override void _Ready() {
        this.Connect("animation_finished", this, nameof(_AnimationFinished));
        this.Connect("frame_changed", this, nameof(_FrameChanged));

        this.parent = GetParent() as Player;
        this.current = new AnimationData();
    }

    //Applies offset then calls parent play
    public new void Play(string anim = "", bool backwards = false) {        
        base.Play(anim, backwards);
        this.Frame = 0;

        AnimationData data;
        //Has animation data
        if (this.data.TryGetValue(anim, out data)) {
            this.current = data;
        
            this.UpdateHitbox();
        } else {
            //Does not have data, sets to defaults
            this.current = new AnimationData();
        }
        
        //Set this position
        this.Position = this.current.offset;
    }

    private void _AnimationFinished() {
        //If player has just attacked or parried, return to stance
        if (this.parent.state == Player.State.ATTACK) {
            this.parent.attack.End(this.parent);
        } else if (this.parent.state == Player.State.PARRY) {
            this.parent.parry.End(this.parent);
        } else if (this.parent.state == Player.State.TRANS) {
            this.parent.TransitionEnd();
        } else if (! this.Frames.GetAnimationLoop(this.Animation)) {
            //If not repeating, reset sprite
            this.Reset();
        }
    }

    private void _FrameChanged() {
        //If player is attacking this is the hitframe
        if (this.parent.state == Player.State.ATTACK && this.Frame == this.parent.attack.hitframe) {
            this.parent.attack.Hit(this.parent);
        }

        this.UpdateHitbox();
    }
    
    private void UpdateHitbox() {
        if (this.Frame < this.current.hitbox.Length) {
            //this.parent.nodeCollision.Position = 
            //        (this.current.offset + this.current.hitbox_offset[this.Frame]);

            //this.parent.nodeCollision.Shape = this.current.hitbox[this.Frame];

            this.parent.hitbox = this.current.hitbox[this.Frame];
        }
    }

    //Resets sprite to current stance
    public void Reset() {
        this.Play(this.parent.stance.sprite);
        //this.Stop();
    }
}
