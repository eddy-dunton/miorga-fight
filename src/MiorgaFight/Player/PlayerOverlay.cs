using Godot;
using System;
using System.Collections.Generic;

namespace MiorgaFight {

public class PlayerOverlay : AnimatedSprite {
    [Export] public Dictionary<String, Vector2> offsets;

    private Player parent;

    public override void _Ready() {
        //Keep going up until you reach the player
        //Is there a better way to do this?
        //Probably
        Node node = this.GetParent();
        while (! (node is Player)) {
            
            //Player is not a ancestor
            if (node == null) {
                //Cry
                GD.PushError("PlayerOverlay.cs: Player overlay is not the descendant of a Player object");
                return;
            }
            
            node = node.GetParent();
        }

        this.parent = (Player) node;

        //Setup trigger
        this.Connect("animation_finished", this, nameof(_AnimationFinished));
    }

    public new void Play(String anim, bool backwards = false) {
        Vector2 off;
        this.offsets.TryGetValue(anim, out off);
        if (off != null) this.Position = off;
        else this.Position = new Vector2();  

        this.Visible = true;
        this.Frame = 0;
        base.Play();
    }

    public void End() {
        this.Visible = false;
        this.Stop();
    }

    public void _AnimationFinished() {
        this.End();
    } 
}}