using Godot;
using System;

namespace MiorgaFight {

public class PauseMenu : CanvasLayer {
    public override void _Ready() {
        //Connect the resume button to end pause
        GetNode("bt_resume").Connect("pressed", GetNode("/root/Command"), nameof(Command.PauseEnd));
        GetNode("bt_quit").Connect("pressed", Command.lobby, nameof(Lobby.GameQuit));
    }
}}
