using Godot;
using System;
using System.Collections.Generic;

public class PlayerAnimation : AnimatedSprite {

    [Export] Dictionary<String, Vector2> offsets;

    private Player parent;

    public override void _Ready() {
        this.Connect("animation_finished", this, nameof(_AnimationFinished));
        this.Connect("frame_changed", this, nameof(_FrameChanged));

        this.parent = GetParent() as Player;
    }

    //Applies offset then calls parent play
    public new void Play(string anim = "", bool backwards = false) {
        Vector2 off = new Vector2(); 
        //Sets off to offset if there is one
        offsets.TryGetValue(anim, out off);
        
        this.Position = off * parent.SCALEFACTOR;
        base.Play(anim, backwards);
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
    }
    
    //Resets sprite to current stance
    public void Reset() {
        this.Play(this.parent.stance.sprite);
        this.Stop();
    }
}
