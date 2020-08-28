using Godot;
using System.Collections.Generic;

namespace MiorgaFight {

public class Level : Node2D
{
    //Dict of all foliage in level, sorted by their x positions
    public List<Foliage> foliage;

    [Export] private float POSITION_LEFT;
    [Export] private float POSITION_RIGHT;
    [Export] private float POSITION_Y;

    public Level() {
        this.foliage = new List<Foliage>();
    }

    public override void _Ready() {}

    public Vector2 GetPlayerPosition(Player.Direction dir) {
        return new Vector2((dir == Player.Direction.LEFT ? POSITION_RIGHT : POSITION_LEFT), POSITION_Y); 
    }

    public float GetPlayerY() {return this.POSITION_Y;}
}}
