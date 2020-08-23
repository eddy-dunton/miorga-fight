using Godot;
using System;

namespace MiorgaFight {

public class Foliage : AnimatedSprite
{
	//First frame of the in gust animation
	[Export] private int gustStartFrame;
	//Final frame of the in gust animation
	[Export] private int gustEndFrame;

	public bool gusting;

	public Foliage() {
		this.gusting = false;
	}

	public override void _Ready()
	{ 
		//Get holytree parent
		Level parent = this.FindParent("level") as Level;
		if (parent == null) {
			GD.Print("Error: Foliage._Ready(...): Finding parent level");
			return;
		}

		parent.AddFoliage(this);
		
		this.Connect("animation_finished", this, nameof(_OnAnimationFinished));    
		this.Connect("frame_changed", this, nameof(_OnFrameChanged));
	}

	//Caled when the frame has changed, continues tighter animation loop if gusting
	void _OnFrameChanged() {
		if (this.gusting && this.Frame > this.gustEndFrame) {
			this.Frame = this.gustStartFrame;
		}
	}

	//Called when a animation finishes, play idle
	void _OnAnimationFinished() {
		//At end of gust animation will fall through to finish, reset to idle
		if (!this.gusting) this.Play("idle");
	}
	
	public void StartGust() {
		this.Play("gust");
		this.gusting = true;
	}

	//Ends gust
	public void EndGust() {
		//this.Play("idle");
		this.gusting = false;
	}
}}
