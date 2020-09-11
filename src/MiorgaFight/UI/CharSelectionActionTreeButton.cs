using Godot;
using System;

namespace MiorgaFight {

public class CharSelectionActionTreeButton : RaiseButton
{
    [Export] private String anim;

    //CharSelectionDataPanel at root of scene
    [Export] private NodePath parentPath;

    //Animation played on end
    [Export] private string end;

    //Overlay to be placed over tree when selected
    [Export] private Texture overlay; 

    private CharSelectionDataPanel parent;

    public override void _Ready()
    {
        base._Ready();
        this.parent = GetNode<CharSelectionDataPanel>(parentPath);
        this.parent.buttons.Add(this);

        this.Connect("pressed", this, nameof(this._OnPressed));
    }

    private void _OnPressed() {
        //Check for correct setup
        if (this.parent != null) {
            this.parent.Play(this.anim, this.end, this.overlay);
        }
    }
}}