using Godot;
using System.Collections.Generic;

namespace MiorgaFight {

public class Level : Node2D
{
	//Width of the level that contains foliage
	[Export] private int foliageWidth = 0;

	//The width in px of each group of foliage elements
	public const int FOLIAGE_GROUPING_SIZE = 5;

	//Total number of possible positions in this.foliage
	public int foliagePositions;

	//Dict of all foliage in level, sorted by their x positions
	public Dictionary<int, Foliage> foliage;

	[Export] private float POSITION_LEFT;
	[Export] private float POSITION_RIGHT;
	[Export] private float POSITION_Y;

	//Type of sound produced in this level
	[Export] public SoundEffect.Soundscape soundscape;

	public Level() {
		this.foliage = new Dictionary<int, Foliage>();
	}

	public override void _Ready() {
		this.foliagePositions = foliageWidth / FOLIAGE_GROUPING_SIZE;
	}

	public Vector2 GetPlayerPosition(Player.Direction dir) {
		return new Vector2((dir == Player.Direction.LEFT ? POSITION_RIGHT : POSITION_LEFT), POSITION_Y); 
	}

	public float GetPlayerY() {return this.POSITION_Y;}

	//Adds foliage to the dictionary
	public void AddFoliage(Foliage f) {
		int x = ((int) f.GlobalPosition.x) / FOLIAGE_GROUPING_SIZE;
		if (this.foliage.ContainsKey(x)) {
			GD.Print("ERROR: Level.AddFoliage(...): duplicate key used: " + x.ToString());
			return;
		}
		this.foliage.Add(x, f);
	}
}}
