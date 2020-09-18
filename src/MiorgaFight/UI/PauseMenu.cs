using Godot;
using System;

namespace MiorgaFight {

public class PauseMenu : CanvasLayer {
    
    //Text to be used in place of 'Paused!' if the game is in multiplayer 
    [Export] string textMp;

    private Label nodeText;

    private Button nodeResume, nodeQuit; 

    public override void _Ready() {
        this.nodeText = this.GetNode<Label>("la_paused");

        //Case online
        if (Lobby.role != Lobby.MultiplayerRole.OFFLINE) {
            this.nodeText.Text = this.textMp;
        }
        //Connect the resume button to end pause
        this.nodeResume = GetNode<Button>("bt_resume");
        this.nodeResume.Connect("pressed", this, nameof(this._OnContinuePressed));

        this.nodeQuit = GetNode<Button>("bt_quit"); 
        this.nodeQuit.Connect("pressed", this, nameof(this._OnQuitPressed));

        this.nodeResume.GrabFocus();
    }

    void _OnContinuePressed() {
        Command.command.PauseEnd();
    }

    void _OnQuitPressed() {
        Command.lobby.Reset();
    }
}}
