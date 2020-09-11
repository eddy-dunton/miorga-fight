using Godot;
using System;

namespace MiorgaFight {

public class PauseMenu : CanvasLayer {
    
    //Text to be used in place of 'Paused!' if the game is in multiplayer 
    [Export] string textMp;

    private Label nodeText;

    public override void _Ready() {
        this.nodeText = this.GetNode<Label>("la_paused");

        //Case online
        if (Lobby.role != Lobby.MultiplayerRole.OFFLINE) {
            this.nodeText.Text = this.textMp;
        }
        //Connect the resume button to end pause
        GetNode("bt_resume").Connect("pressed", this, nameof(this._OnContinuePressed));
        GetNode("bt_quit").Connect("pressed", this, nameof(this._OnQuitPressed));
    }

    void _OnContinuePressed() {
        Command.command.PauseEnd();
    }

    void _OnQuitPressed() {
        Command.lobby.Reset();
    }
}}
