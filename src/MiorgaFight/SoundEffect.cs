using Godot;
using System.Collections.Generic;

namespace MiorgaFight{

public class SoundEffect : Resource
{
	//Different types of sound
	public enum Soundscape {
		ANY, //Used for when there isn't a specific soundscape for a audiostream, should not be used by levels
		GRASS, WETSTONE
	}

    //A random one of these will be played
    [Export] public Dictionary<Soundscape, AudioStream[]> streams;

    [Export] public float volume;

    [Export] public bool repeat;

    //Default
    public SoundEffect() {
        this.streams = new Dictionary<Soundscape, AudioStream[]>();

        this.volume = 0;

        this.repeat = false;
    }

    //Returns a random audiostream from the given soundscape, if there is no soundscape specific sound it will return a 
    //sound from the any soundscape, if there isn't one there it will return null
    public AudioStream Get(Soundscape ss) {
        AudioStream[] possibleStreams;
        
        //Look for soundscape specific streams
        if (this.streams.TryGetValue(ss, out possibleStreams)) {
            return Command.Random(possibleStreams);
        }

        //Look for generic streams
        if (this.streams.TryGetValue(Soundscape.ANY, out possibleStreams)) {
            return Command.Random(possibleStreams);
        }

        //No streams available
        return null;
    }
}}
