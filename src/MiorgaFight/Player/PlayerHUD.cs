using Godot;
using System;

namespace MiorgaFight {

public class PlayerHUD : Control
{
    public TextureProgress nodeHP;
    public Label nodeScore;

    public override void _Ready() {
        //Get nodes
        this.nodeHP = GetNode<TextureProgress>("pb_health");
        this.nodeScore = GetNode<Label>("la_score");
        //Be invisible on creation
        this.Visible = false;
    }
}}
