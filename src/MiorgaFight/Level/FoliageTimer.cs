using Godot;
using System;

namespace MiorgaFight {

public class FoliageTimer : Timer
{
	//Minimum number of zones that will be moved through in a second
	[Export] private int zonesPerSecondMin;
	//zonesPerSecondMin Converted into waitTime (1 / zonesPerSecondMin)
	private float waitTimeMax;
	//Maximum number of zones that will be moved thorugh in a second
	[Export] private int zonesPerSecondMax;
	//zonesPerSecondMax Converted into waitTime (1 / zonesPerSecondMax)
	private float waitTimeMin;

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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{   
		//These are intentionally swapped round (cos of you 1 / x you wally)
		this.waitTimeMin = 1 / (float) this.zonesPerSecondMax;
		this.waitTimeMax = 1 / (float) this.zonesPerSecondMin;

		this.parent = this.FindParent("level") as Level;
		if (this.parent == null) {
			GD.Print("Error: FoliageTimer._Ready(...): Finding parent level");
			return;
		}

		this.Connect("timeout", this, nameof(_OnTimeout));
		this.position = this.parent.foliagePositions;
		//Reset cycle for start
		this.ResetCycle();
	}

	void _OnTimeout() {
		if (this.gusting) {
			this.position --;
			this.StartGustAt(this.position);
			this.EndGustAt(this.position + this.gustLength);
			//Once gust has passed through all the positions, reset
			if (this.position + this.gustLength < -this.parent.foliagePositions) {this.ResetCycle();}
		} else { //Gust gap has ended, start gust
			this.WaitTime = this.gustWaitTime;
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
	}
}}
