using Godot;
using System;

public class PlayerHUD : Control
{
    public ProgressBar nodeHP;
    public Label nodeScore;

    public override void _Ready() {
        //Get nodes
        this.nodeHP = GetNode<ProgressBar>("pb_health");
        this.nodeScore = GetNode<Label>("la_score");
        //Be invisible on creation
        this.Visible = false;
    }
}
