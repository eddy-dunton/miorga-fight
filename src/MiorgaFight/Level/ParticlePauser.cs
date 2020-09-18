using Godot;
using System.Collections.Generic;
using System.Linq;

/*
This is developed as a working around for a Godot bug which causes particle animations to not pause 
when the game is paused

This should be removed ASAP

See https://github.com/edward-dunton/miorga-fight/issues/34

This has to be seperate from the particles themselves as they have to be have PauseMode set to Stop,
However, PauseMode must be set to Process in order to realise the game has been paused
This this class exists
*/
public class ParticlePauser : Node
{
    //Used to track the state of pause during the last physics process,
    //If this does not equal the current pause state then the state has changed and the particles pause values need to
    //Change
    private bool lastPaused;

    //A list (rather than an array is fine here, it is always iterated through, and performance impact will be negliable)
    //Using a list also leads to simpler init code
    private List<Particles2D> particles;

    public override void _Ready()
    {
        this.lastPaused = false;
        
        this.particles = new List<Particles2D>();

        Particles2D asParticles;
        foreach (Node child in this.GetChildren()) { //Iterate through children
            //Filter out all none particles
            asParticles = child as Particles2D;
            if (asParticles != null) {
                //Add particles to the list of particles
                this.particles.Add(asParticles);
            }
        }
    }

    public override void _PhysicsProcess(float delta) {
        //Check for a change in pause state
        if (this.lastPaused != GetTree().Paused) {
            this.lastPaused = GetTree().Paused;

            foreach (Particles2D p in this.particles) {
                //Check that the particle uses 
                AnimatedTexture tex = p.Texture as AnimatedTexture;
                if (tex != null) {
                    //If so, pause the texture
                    tex.Pause = this.lastPaused;
                }
            }
        }
    }
}
