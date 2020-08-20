using Godot;
using System;

namespace MiorgaFight {

public class CharSelectionActionTreeButton : Button
{
    [Export] private String anim;

    //CharSelectionDataPanel at root of scene
    [Export] private NodePath parentPath;

    private CharSelectionDataPanel parent;

    public override void _Ready()
    {
        this.parent = GetNode<CharSelectionDataPanel>(parentPath);
        this.Connect("pressed", this, nameof(this._OnPressed));
    }

    private void _OnPressed() {
        //Check for correct setup
        if (this.parent != null) {
            this.parent.Play(this.anim);
        }
    }
}}