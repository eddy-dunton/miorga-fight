using Godot;
using System;

namespace MiorgaFight {

public class Foliage : AnimatedSprite
{
    public override void _Ready()
    { 
        //Get holytree parent
        Level parent = this.FindParent("level") as Level;
        if (parent == null) {
            GD.Print("Error: Foliage._Ready(...): Finding parent level");
            return;
        }

        parent.AddFoliage(this);
        
        //this.Connect("animation_finished", this, nameof(_OnAnimationFinished));    
    }

    void _OnAnimationFinished() {
        //this.SpeedScale = (float) (1f + random.NextDouble());
    }
}}
