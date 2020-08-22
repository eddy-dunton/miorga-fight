using Godot;
using System;

namespace MiorgaFight {

public class FoliageTimer : Timer
{
    private int position;
    
    private Level parent;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.parent = this.FindParent("level") as Level;
        if (this.parent == null) {
            GD.Print("Error: FoliageTimer._Ready(...): Finding parent level");
            return;
        }

        this.Connect("timeout", this, nameof(_OnTimeout));
        this.position = this.parent.foliagePositions;
    }

    void _OnTimeout() {
        this.position --;
        this.parent.PlayFoliage(this.position);
        if (this.position < -this.parent.foliagePositions) this.CycleEnded();
    }

    private void CycleEnded() {
        //Reset position
        this.position = this.parent.foliagePositions;
        //Randomly generate new speed, between 100 and 1000 zones / second
        this.WaitTime = (float) Command.Random(0.001, 0.005);
    
        //Calculate speed scale of foliage based off wait time
        //Whereby wt -> ss: (0.001, 0.01) -> (1.1, 2)
        float ss = 1 + (this.WaitTime * 100);

        foreach (Foliage f in this.parent.foliage.Values) {
            //Set foliage speed scale to ss +/- 0.2
            f.SpeedScale = ss + (float) Command.Random(-0.5, 0.5);
        }
    }
}}
