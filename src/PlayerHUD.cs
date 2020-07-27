using Godot;
using System;

public class PlayerHUD : Control
{
    public ProgressBar nodeHP;
    public Label nodeScore;

    public override void _Ready() {
        //Get nodes
        this.nodeHP = GetNode<ProgressBar>("health");
        this.nodeScore = GetNode<Label>("score");
    }
}
