using Godot;
using System;

public class PauseMenu : CanvasLayer {
    public override void _Ready() {
        //Connect the resume button to end pause
        GetNode("bt_resume").Connect("pressed", GetNode("/root/Command"), nameof(Command.PauseEnd));
    }
}
