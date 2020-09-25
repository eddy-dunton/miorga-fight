using Godot;
using System;

public class SoundEffect : Resource
{
    //A random one of these will be played
    [Export] public AudioStream[] streams;

    [Export] public float volume;

    [Export] public bool repeat;

    //Default
    public SoundEffect() {
        this.streams = new AudioStream[0];

        this.volume = 0;

        this.repeat = false;
    }
}
