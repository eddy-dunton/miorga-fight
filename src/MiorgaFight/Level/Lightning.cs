using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiorgaFight {

public class Lightning : AnimationPlayer
{
    [Export] private NodePath foreground;
    [Export] private NodePath background;

    [Export] private double bgWidth;

    //Animations for foreground and background
    //Is each is an list of arrays, each containing 2 strings, 0 is the start animation, 1 is the end animation
    private string[][] fgAnims;
    private string[][] bgAnims;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.Connect("animation_finished", this, nameof(this._OnAnimationFinished));
        //Queue up an animation
        this._OnAnimationFinished("");

        //Calculate animation sets for foreground and background 

        SpriteFrames f = this.GetNode<AnimatedSprite>(this.foreground).Frames;
        //Find all starting animations, which have a matching post
        IEnumerable<string> startAnims = f.GetAnimationNames().Where(
            x => 
            x.EndsWith("pre") && 
            f.HasAnimation(x.Replace("pre", "post")));

        //Contructs an array of arrays, each containing a matching pre and post animation, from start anims
        this.fgAnims = startAnims.Select(x => new string[] {x, x.Replace("pre", "post")}).ToArray();

        //Repeat with bgAnims
        f = this.GetNode<AnimatedSprite>(this.background).Frames;
        //Find all starting animations, which have a matching post
        startAnims = f.GetAnimationNames().Where(
            x => 
            x.EndsWith("pre") && 
            f.HasAnimation(x.Replace("pre", "post")));

        //Contructs an array of arrays, each containing a matching pre and post animation, from start anims
        this.bgAnims = startAnims.Select(x => new string[] {x, x.Replace("pre", "post")}).ToArray();
    }

    //Called when an animation is finished
    //Sets up a timer which calls creates a new strike after a random amount of time
    void _OnAnimationFinished(String anim) {
        //Create a timer, which will pause when the game is paused
        SceneTreeTimer timer = GetTree().CreateTimer((float) Command.Random(1.0, 1.0), false);
        timer.Connect("timeout", this, nameof(this.Strike));
    }

    //Sets up (and then fires off a strike)
    void Strike() {
        NodePath targetPath;
        string[] spriteAnims;

        //Randomly select whether the foreground or background will strike
        if (Command.Random(0,1) == 1) { //Foreground strike
            targetPath = this.foreground;
            spriteAnims = Command.Random(this.fgAnims);
        } else { //Background strike
            targetPath = this.background;
            spriteAnims = Command.Random(this.bgAnims);

            //Change position
            Vector2 pos = this.GetNode<AnimatedSprite>(this.background).Position;
            pos.x = (float) this.GetBGPosition(this.bgWidth);
            this.GetNode<AnimatedSprite>(this.background).Position = pos;
        }

        //TODO continue here:
        //Randomise speed (ish)
        //Randomise fg positions

        //Point paths to the correct place
        Animation anim = this.GetAnimation("strike");
        anim.TrackSetPath(1, new NodePath(targetPath.ToString() + ":frame"));
        anim.TrackSetPath(2, new NodePath(targetPath.ToString() + ":playing"));

        anim.TrackSetPath(3, new NodePath(targetPath.ToString() + ":animation"));
        //Sets the starting animation
        anim.TrackSetKeyValue(3, 0, spriteAnims[0]);
        //Swap "pre" for "post" and set ending animation
        anim.TrackSetKeyValue(3, 1, spriteAnims[1]);


        this.Play("strike");
    }

    //Generates a position for a background strike
    private double GetBGPosition(double width) {
        double x = Command.Random(0.0,1.0);

        //background positions are distrobuted by the following curve: https://www.desmos.com/calculator/anbokqxx2q
        //It follows a rough bimodal distrobution, such that the majority of lightning should fall on either side of the
        //centre of the map
        //The ouptut of this function, y, will then have to be scaled to the length of the map itself
        double y = x;

        if (x >= 0.5) y -= 0.75;
        else y -= 0.25;
        
        y *= 4;
        y = Math.Asin(y);

        y /= (2 * Math.PI);

        if (x >= 0.5) y += 0.75;
        else y += 0.25;

        //Maps y: 0-1 => (-width/2) => width/2
        return Command.Map(0.0, 1.0, -(width/2), width/2, y);
    }
}}