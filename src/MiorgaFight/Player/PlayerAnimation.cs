using Godot;
using System;
using System.Collections.Generic;

namespace MiorgaFight {

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

        //Tries to get animation data
        //Resets this.current to a blank value if none is found 
        if (!this.data.TryGetValue(anim, out this.current)) {
            this.current = new AnimationData();
        }

        //Play sfx if present
        if (this.current.sound != null) this.parent.PlaySfx(this.current.sound);

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
    }
    
	//Returns this players hitbox as a shape and a transform
	//Will return a 0 long line with a transform of 0 the player does not have one
	//Returns value of this.nodeAnimateSprite.GetHitbox(..)
    //The length of the hitbox and hitbox_offset 
    public (Shape2D, Transform2D) GetHitbox() {
        //Lists are not the same length, or are both of length 0, cry
        if (this.current.hitboxOffset.Length != this.current.hitbox.Length || this.current.hitbox.Length == 0) 
            return (new LineShape2D(), new Transform2D());
        
        //Case: current frame is higher than number of hitboxes
        else if (this.Frame >= this.current.hitbox.Length) {
            //If there is not a hitbox for the current frame, use the last one
            return (this.current.hitbox[this.current.hitbox.Length - 1],
                    this.GlobalTransform.Translated(this.current.hitboxOffset[this.current.hitboxOffset.Length - 1]));   
            
        } else {
            //There is a hitbox offset for this frame
            return (this.current.hitbox[this.Frame], 
                    this.GlobalTransform.Translated(this.current.hitboxOffset[this.Frame]));
        }
    }

    //Resets sprite to current stance
    public void Reset() {
        this.Play(this.parent.stance.sprite);
        //this.Stop();
    }
}}
