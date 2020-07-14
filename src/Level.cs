using Godot;
using System;

public class Level : Node2D
{
    [Export] private Vector2 POSITION_LEFT;

    [Export] private Vector2 POSITION_RIGHT;

    [Export] private float POSITION_Y;

    public Level() {
        GD.Print("Game created");
    }

    public override void _Ready() {
        if (POSITION_LEFT.y != POSITION_RIGHT.y) GD.Print("Error: Left and Right players do not have the same y value");

        this.POSITION_Y = POSITION_LEFT.y;
    }

    public Vector2 GetPlayerPosition(Player.Direction dir) {
        return (dir == Player.Direction.LEFT ? POSITION_RIGHT : POSITION_LEFT); 
    }

    public float GetPlayerY() {return this.POSITION_Y;}
}
