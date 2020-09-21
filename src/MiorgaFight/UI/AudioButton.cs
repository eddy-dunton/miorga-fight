using Godot;
using System;

/*
    Class for any button which has audio
    Automatically creates a child sfx node which the sound effects are played
    
    The following signals are connected to the following methods,
    if you want to use these signals then make sure the new connected method calls the original method:
     - "mouse_entered": _OnMouseEntered()
     - "button_up": _ButtonUp()
     - "focus_entered": _FocusEntered()
*/
public class AudioButton : Button
{
    //Sound effect to be played when the button is hovered over or focused
	[Export] AudioStream audioHover;

    //Sound effect to be played when the button is clicked
	[Export] AudioStream audioClick;

    AudioStreamPlayer nodeSfx;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        //Create sfx player
        this.nodeSfx = new AudioStreamPlayer();
        this.nodeSfx.Bus = "ui";
        
        this.AddChild(this.nodeSfx);

        //Only bother connecting if an audio stream is present
        if (this.audioHover != null) {
            this.Connect("mouse_entered", this, nameof(_OnMouseEntered));
            this.Connect("button_up", this, nameof(_ButtonUp));
            this.Connect("focus_entered", this, nameof(_FocusEntered));
        }
    }

    public void _OnMouseEntered() {this.PlayHover();}

    public void _ButtonUp() {this.PlayHover();}

    public void _FocusEntered() {this.PlayHover();}

    public void PlayHover() {
        //Do not play if disabled
        if (this.Disabled) return;

        //Play the raise sound
		this.nodeSfx.Stream = this.audioHover;
		this.nodeSfx.Play();
    }
}
