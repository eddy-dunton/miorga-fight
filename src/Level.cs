using Godot;
using System;

public class Level : Node2D
{
    [Export] private Vector2 POSITION_LEFT;

    [Export] private Vector2 POSITION_RIGHT;

    public Vector2 GetPlayerPosition(Player.Direction dir) {
        return (dir == Player.Direction.LEFT ? POSITION_RIGHT : POSITION_LEFT); 
    }
}
