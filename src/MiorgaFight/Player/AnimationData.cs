using Godot;
using System;

namespace MiorgaFight {

public class AnimationData : Resource
{
    //Position the animation should be moved to when this animation is 
    [Export] public Vector2 offset;

    //Position relative to the animation's position (after offset is applied) of offset
    [Export] public Vector2[] hitboxOffset;

    //Shape of the hitbox
    [Export] public Shape2D[] hitbox;

    [Export] public SoundEffect sound;

    //If the length of hitbox and hitbox_offset are not the same Godot will most likely throw a fit

    //Sets deafults
    public AnimationData() {
        this.offset = new Vector2();

        this.hitboxOffset = new Vector2[0];

        this.hitbox = new Shape2D[0];

        this.sound = null;
    }
}}
