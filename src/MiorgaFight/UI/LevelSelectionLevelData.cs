using Godot;
using System;

namespace MiorgaFight {

public class LevelSelectionLevelData : Resource
{
    // Description of this level
    [Export] public string text;

    // Packed Scene version of this
    [Export] public PackedScene packed;

    // Width of this level
    [Export] private int width;

    //Used for caching camera position because getting it is a pain
    private Vector2? cameraPos;

    //Set to defaults
    public LevelSelectionLevelData() {
        this.text = "";
        this.packed = null;
        this.width = 0;

        this.cameraPos = null;
    }

    public Vector2 GetCameraPos() {
        if (!this.cameraPos.HasValue) {
            this.cameraPos = (this.packed.Instance() as Level).GetNode<Node2D>("camera_track").Position;
        }

        return this.cameraPos.Value;
    }

    public int GetMovement() {
        return (this.width - 780) / 2;
    }
}}
