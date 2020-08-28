using Godot;
using System.Collections.Generic;

namespace MiorgaFight {

public class FoliageTimer : AnimationPlayer
{
	//Minimum number of pixels that will be moved through in a second
	[Export] private int pxPerSecondMin;
	//Maximum number of pixels that will be moved thorugh in a second
	[Export] private int pxPerSecondMax;
	
	//Lowest possible base speed scale for foliage (when zones / second = min)
	[Export] private float speedScaleMin;
	//Highest possible base speed scale for the foliage (when zones / second = max)
	[Export] private float speedScaleMax;
	//Vairance of speed scale, each individual piece of foliage's speed scale = base speed scale + random(-vss, vss)
	[Export] private float speedScaleVar;

	//Minimum possible length of gust (in zones)
	[Export] private int gustLegnthMin;
	//Maximmum possible length of gust (in zones)
	[Export] private int gustLegnthMax;

	//Minimum length (in sec between gusts)
	[Export] private float gustGapMin;
	//Maximum length (in sec between gusts)
	[Export] private float gustGapMax;

	//Current head of the gust
	private int position;
	
	private Level parent;
	
	//Length of the current gust in foliage positions
	private int gustLength; 

	//Is there a gust currently happening
	private bool gusting;
	private float gustWaitTime;

	/*// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{ 
		this.parent = this.FindParent("level") as Level;
		if (this.parent == null) {
			GD.Print("Error: FoliageTimer._Ready(...): Finding parent level");
			return;
		}

		this.PopulateAnimation(this.parent.foliage);

		this.Connect("timeout", this, nameof(_OnTimeout));
		this.position = this.parent.foliagePositions;
		//Reset cycle for start
		this.ResetCycle();
	}

	//Redo this
	//Was called when the time ends
	void _OnTimeout() {
		if (this.gusting) {
			this.position --;
			this.StartGustAt(this.position);
			this.EndGustAt(this.position + this.gustLength);
			//Once gust has passed through all the positions, reset
			if (this.position + this.gustLength < -this.parent.foliagePositions) {this.ResetCycle();}
		} else { //Gust gap has ended, start gust
			this.gusting = true;
		}
	}

	private void StartGustAt(int x) {
		Foliage f;
		if (this.parent.foliage.TryGetValue(x, out f)) {
			f.StartGust();
		}
	}

	private void EndGustAt(int x) {
		Foliage f;
		if (this.parent.foliage.TryGetValue(x, out f)) {
			f.EndGust();
		}
	}

	//Resets the gust, waiting before the next gust happens
	private void ResetCycle() {
		//Reset position
		this.position = this.parent.foliagePositions;
		//Randomly generate new speed
		this.gustWaitTime = (float) Command.Random(this.waitTimeMin, this.waitTimeMax);
	
		//Calculate speed scale of foliage based off wait time
		float ss = (float) 
				Command.Map(this.waitTimeMax, this.waitTimeMin, this.speedScaleMin, this.speedScaleMax, this.WaitTime);

		foreach (Foliage f in this.parent.foliage.Values) {
			//Set foliage speed scale to ss +/- ss var
			f.SpeedScale = ss + (float) Command.Random(-this.speedScaleVar, this.speedScaleVar);
		}

		//Randomly generate gust length
		this.gustLength = Command.Random(this.gustLegnthMin, this.gustLegnthMax);

		//Set the gap to next gust
		this.WaitTime = (float) Command.Random(this.gustGapMin, this.gustGapMax);
		this.gusting = false;
	}*/

	//Uses the list provided to contruct a the wave animation
	private void PopulateAnimation() {
		Animation wave = this.GetAnimation("wave");
		wave.Clear();

		int i;
		Dictionary<string, Godot.Collections.Array> dict;
		foreach (Foliage f in this.parent.foliage) {
			//Create new track and get index
			i = wave.AddTrack(Animation.TrackType.Method);
			wave.TrackSetPath(i, this.GetPath());
			//The format for a function call is a Godot.Collections.Dictionary with keys "method" and "args"
			//The value for method should be the method name and args should be a Godot.Collections.Array with the
			//arguments for that method in it
			//It should look like this {method: nameof(this.startWave), args: [index of f]}
			dict = new Dictionary<string, Godot.Collections.Array>();
			dict.Add(nameof(this.startWave), )
		}
	}
}}
