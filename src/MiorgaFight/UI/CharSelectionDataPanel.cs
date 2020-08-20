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

    public override void _Ready()
    {
        this.nodePlayerSprite = GetNode<AnimatedSprite>("sp_player");
        this.playerSpriteOrigin = this.nodePlayerSprite.Position;
    }

    //Resets this panel back to its original state
    public void Reset() {
        this.Play("lax");
    }

    public void Play(String anim) {
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
}}
