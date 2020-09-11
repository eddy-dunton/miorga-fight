using Godot;
using System;
using System.Collections.Generic;

namespace MiorgaFight {

public class CharSelectionDataPanel : Control
{
    [Export] Dictionary<String, Vector2> offsets;

    [Export] public PackedScene character;

    private Vector2 playerSpriteOrigin;
    private AnimatedSprite nodePlayerSprite;
    private Sprite nodeTreeOverlay;

    private string onAnimEnd;

    //List of all buttons on this panel, buttons add themselves to this panel
    public List<RaiseButton> buttons;

    public CharSelectionDataPanel() {
        this.buttons = new List<RaiseButton>();
    }

    public override void _Ready()
    {
        this.nodePlayerSprite = GetNode<AnimatedSprite>("sp_player");
        this.nodeTreeOverlay = GetNode<Sprite>("pa_action_tree/sp_action_overlay");

        this.nodePlayerSprite.Connect("animation_finished", this, nameof(_OnAnimationFinished));
        this.playerSpriteOrigin = this.nodePlayerSprite.Position;
        this.onAnimEnd = null;
    }

    //Resets this panel back to its original state
    //P1 is whether the player selecting a character is p1 or not
    public void Reset(bool p1) {
        this.Play("lax");
        
        //Change text to P1 or P2 bindings, if playing offline
        if (Lobby.role == Lobby.MultiplayerRole.OFFLINE) {
            string main = p1 ? "Q" : "I"; //Main actions are on Q if p1 and I otherwise
            string alt = p1 ? "E" : "P"; //Main actions are on E if p1 and P otherwise

            foreach (RaiseButton b in this.buttons) {
                if (b.Text == "Q" || b.Text == "I") b.Text = main;
                else if (b.Text == "E" || b.Text == "P") b.Text = alt;
                else if (b.Text == "E/Q" || b.Text == "P/I") b.Text = alt + "/" + main;
            }
        }
    }

    public void Play(String anim, string onEnd = null, Texture overlay = null) {
        this.onAnimEnd = onEnd;
        this.nodeTreeOverlay.Texture = overlay;

        //Try and get data for the animation
        Vector2 offset;
        if (this.offsets.TryGetValue(anim, out offset)) {
            //If there is, set sprite offset to graphics offset
            this.nodePlayerSprite.Position = this.playerSpriteOrigin + offset;
        } else {
            //Otherwise set to 0,0
            this.nodePlayerSprite.Position = this.playerSpriteOrigin;
        }

        this.nodePlayerSprite.Play(anim);
    }

    void _OnAnimationFinished() {
        if (this.onAnimEnd != null) {
            this.Play(this.onAnimEnd);
        }
    }
}}
