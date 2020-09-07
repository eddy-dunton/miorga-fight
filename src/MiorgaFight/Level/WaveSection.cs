using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiorgaFight {

public class WaveSection : Node2D
{
    [Export] private float duration;

    //Waves 
    private AnimatedSprite[][] waves;

    //Whether a wave group should refire instantly after finishing
    //private bool[] fireInstantly;

    public override void _Ready()
    {
        List<AnimatedSprite[]> waveslist = new List<AnimatedSprite[]>();

        //Get all children that are Groups, these are assumed to have exclusively AnimatedSprites children
        int i = 0; 
        foreach (Node child in this.GetChildren()) {
            //Check that this actually is a sprite group
            if (! child.Name.StartsWith("group")) continue;

            //Waves in this group
            //I can't use LINQ cos some twat decided Godot.Collections.Array shouldn't support it
            //Thanks a lot
            AnimatedSprite[] groupWaves = new AnimatedSprite[child.GetChildren().Count];
            i = 0;
            foreach (Node waveAsNode in child.GetChildren()) {
                AnimatedSprite wave = waveAsNode as AnimatedSprite;
                //Call wave finished with index when finished
                wave.Connect("animation_finished", this, nameof(_OnWaveEnded), 
                        new Godot.Collections.Array(new object[] {wave, waveslist.Count, i}));
                //Generate wave speed
                wave.SpeedScale = this.GenerateWaveSpeed(wave);

                groupWaves[i] = wave;                
                i ++;
            }

            waveslist.Add(groupWaves);
        }

        this.waves = waveslist.ToArray();

        SceneTreeTimer tmr;
        i = 0;
        //Fire off all the groups at regular intervals
        foreach (AnimatedSprite[] group in this.waves) {
            tmr = GetTree().CreateTimer((this.duration / this.waves.Count()) * i);
            tmr.Connect("timeout", this, nameof(StartWave), new Godot.Collections.Array(new object[] {i}));
            i ++;
        }
    }

    void _OnWaveEnded(AnimatedSprite wave, int groupIndex, int waveIndex) {
        //Reset the wave
        wave.Visible = false;

        //Used if waves are finished (and therefore hidden)
        if (this.waves[groupIndex].Any(x => x.Visible)) return;

        this.StartWave(groupIndex);
    }

    private void StartWave(int groupIndex) {
        //Play each wave animation
        foreach (AnimatedSprite wave in this.waves[groupIndex]) {
            //Why the fuck do I have to set frame to 0 when calling play?
            wave.Frame = 0;
            wave.SpeedScale = this.GenerateWaveSpeed(wave);
            wave.Visible = true;
        }
    }

    //Generates a random SpeedScale for a wave
    //Speed will make the wave last duration +/- 10%
    private float GenerateWaveSpeed(AnimatedSprite wave) {
        float baseSpeed = (wave.Frames.GetFrameCount("default") / duration) / wave.Frames.GetAnimationSpeed("default"); 
        return (float) Command.Random(baseSpeed - (baseSpeed / 10), baseSpeed + (baseSpeed) / 10);
    }
}}