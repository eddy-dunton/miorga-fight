using Godot;
using System;

public class PlayerBlood : AnimatedSprite
{
    public override void _Ready()
    {
        //Setup trigger
        this.Connect("animation_finished", this, nameof(_AnimationFinished));
    }

    public void Start() {
        this.Visible = true;
        this.Frame = 0;
        this.Play();
    }

    public void End() {
        this.Visible = false;
        this.Stop();
    }

    public void _AnimationFinished() {
        this.End();
    } 
}
