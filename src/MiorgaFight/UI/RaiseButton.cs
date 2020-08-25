using Godot;
using System;

namespace MiorgaFight {

public class RaiseButton : Button
{
    //Pass this buttons text field straight through to the label
    public new string Text {
        set {
            this.nodeLabel.Text = value;
        } get {
            return this.nodeLabel.Text;
        }
    }

    private Label nodeLabel;

    private Vector2 downPosition;
    private Vector2 upPosition;

    public override void _Ready()
    {
        this.nodeLabel = GetNode<Label>("label");
        this.Connect("mouse_entered", this, nameof(_TextUp));
        this.Connect("button_up", this, nameof(_TextUp));
        this.Connect("mouse_exited", this, nameof(_TextDown));
        this.Connect("button_down", this, nameof(_TextDown));

        this.downPosition = nodeLabel.RectPosition;
        this.upPosition = this.downPosition;
        this.upPosition.y -= 2;
    }

    void _TextUp() {
        this.nodeLabel.RectPosition = this.upPosition;
    }

    void _TextDown() {
        this.nodeLabel.RectPosition = this.downPosition;
    }
}}
