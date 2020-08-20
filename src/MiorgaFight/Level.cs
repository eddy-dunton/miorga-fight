using Godot;
using System;

namespace MiorgaFight {

public class Level : Node2D
{
    [Export] private float POSITION_LEFT;

    [Export] private float POSITION_RIGHT;

    [Export] private float POSITION_Y;

    public Level() {}

    public Vector2 GetPlayerPosition(Player.Direction dir) {
        return new Vector2((dir == Player.Direction.LEFT ? POSITION_RIGHT : POSITION_LEFT), POSITION_Y); 
    }

    public float GetPlayerY() {return this.POSITION_Y;}
}}
