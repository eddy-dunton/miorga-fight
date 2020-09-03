using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiorgaFight {

public class Lightning : AnimationPlayer
{
    //Path to the foreground AnimatedSprite
    [Export] private NodePath fgPath;
    //Path to the background AnimatedSprite
    [Export] private NodePath bgPath;

    //Width of the area affected by lightning
    [Export] private double width;
    //Minimum gap (in seconds) between strikes
    [Export] private double minStrikeGap;
    //Maximum gap (in seconds) between strikes
    [Export] private double maxStrikeGap;
    //Strength is multiplied by this when calculating the length of the gradient of lighting that's used
    [Export] private double strengthWidthMultiplyer;
    //Minimum playback speed which the animations will run at
    [Export] private double minStrikeSpeed;
    //Maxiumu playback speed which the animations will run at
    [Export] private double maxStrikeSpeed;

    //Probabilty that a strike will hit the foregound (between 0.0 and 1.0, anything above 1 will garrantee a fg hit)
    [Export] private double fgStrikeChance;
    //Minimum strength of a foreground strike
    [Export] private double fgMinStrength;
    //Maximum strength of a foreground strike
    [Export] private double fgMaxStrength;
    //Light map used in a foreground strike 
    [Export] private int fgLightMask;

    //Minimum strength of a background strike
    [Export] private double bgMinStrength;
    //Maximum strength of a background strike
    [Export] private double bgMaxStrength;
    //Light mask used in a background strike
    [Export] private int bgLightMask;


    //Animations for foreground and background
    //Is each is an list of arrays, each containing 2 strings, 0 is the start animation, 1 is the end animation
    private string[][] fgAnims;
    private string[][] bgAnims;

    private Light2D nodeLightning;
    private AnimatedSprite fg;
    private AnimatedSprite bg;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.nodeLightning = this.GetNode<Light2D>("lightning");
        this.fg = this.GetNode<AnimatedSprite>(this.fgPath);
        this.bg = this.GetNode<AnimatedSprite>(this.bgPath);

        this.Connect("animation_finished", this, nameof(this._OnAnimationFinished));
        //Queue up an animation
        this._OnAnimationFinished("");

        //Calculate animation sets for foreground and background 

        //Find all starting animations, which have a matching post
        IEnumerable<string> startAnims = this.fg.Frames.GetAnimationNames().Where(
            x => 
            x.EndsWith("pre") && 
            this.fg.Frames.HasAnimation(x.Replace("pre", "post")));

        //Contructs an array of arrays, each containing a matching pre and post animation, from start anims
        this.fgAnims = startAnims.Select(x => new string[] {x, x.Replace("pre", "post")}).ToArray();

        //Repeat with bgAnims
        //Find all starting animations, which have a matching post
        startAnims = this.bg.Frames.GetAnimationNames().Where(
            x => 
            x.EndsWith("pre") && 
            this.bg.Frames.HasAnimation(x.Replace("pre", "post")));

        //Contructs an array of arrays, each containing a matching pre and post animation, from start anims
        this.bgAnims = startAnims.Select(x => new string[] {x, x.Replace("pre", "post")}).ToArray();
    }

    //Called when an animation is finished
    //Sets up a timer which calls creates a new strike after a random amount of time
    void _OnAnimationFinished(String anim) {
        //Create a timer, which will pause when the game is paused
        SceneTreeTimer timer = GetTree().CreateTimer(
                (float) Command.Random(this.minStrikeGap, this.maxStrikeGap), false);
        timer.Connect("timeout", this, nameof(this.Strike));
    }

    //Sets up (and then fires off a strike)
    void Strike() {
        AnimatedSprite target;
        NodePath targetPath;
        string[] spriteAnims;
        double strength;

        //Randomly select whether the foreground or background will strike
        if (Command.Random(0.0,1.0) <= this.fgStrikeChance) { //Foreground strike
            target = this.fg;
            targetPath = this.fgPath;
            spriteAnims = Command.Random(this.fgAnims);
            strength = Command.Random(this.fgMinStrength, this.fgMaxStrength);

            //Change position
            Vector2 pos = this.fg.Position;
            pos.x = (float) this.GetFGPosition(this.width);
            this.fg.Position = pos;
            this.nodeLightning.Position = pos;
            this.nodeLightning.RangeItemCullMask = this.fgLightMask;
        } else { //Background strike
            target = this.bg;
            targetPath = this.bgPath;
            spriteAnims = Command.Random(this.bgAnims);
            strength = Command.Random(this.bgMinStrength, bgMaxStrength);

            //Change position
            Vector2 pos = this.bg.Position;
            pos.x = (float) this.GetBGPosition(this.width);
            this.bg.Position = pos;
            this.nodeLightning.Position = pos;
            this.nodeLightning.RangeItemCullMask = this.bgLightMask;
        }

        //Randomise whether the sprite should be flipped
        target.FlipH = Command.RandomBool();

        //Randomise speed
        this.PlaybackSpeed = (float) Command.Random(this.minStrikeSpeed, this.maxStrikeSpeed);

        //Map strength to width
        (this.nodeLightning.Texture as GradientTexture).Width = (int) (strength * this.strengthWidthMultiplyer); 

        Animation anim = this.GetAnimation("strike");

        //Set strength in animation
        anim.TrackSetKeyValue(0, 2, strength);

        //Point paths to the correct place
        anim.TrackSetPath(1, new NodePath(targetPath.ToString() + ":frame"));
        //Set frames to correct frames used on the animation used
        //Index 1 is at the end of the end of the pre strike, set it to the final frame in the animation
        anim.TrackSetKeyValue(1, 1, target.Frames.GetFrameCount(spriteAnims[0]) - 1);
        //Index 3 is at the end of the end of the post strike, set it to the final frame in the animation
        anim.TrackSetKeyValue(1, 3, target.Frames.GetFrameCount(spriteAnims[1]) - 1);

        anim.TrackSetPath(2, new NodePath(targetPath.ToString() + ":playing"));
        anim.TrackSetPath(3, new NodePath(targetPath.ToString() + ":animation"));
        //Sets the starting animation
        anim.TrackSetKeyValue(3, 0, spriteAnims[0]);
        //Swap "pre" for "post" and set ending animation
        anim.TrackSetKeyValue(3, 1, spriteAnims[1]);

        this.Play("strike");
    }

    //Generates a position for a foreground strike
    private double GetFGPosition(double width) {
        double x = Command.Random(0.0,1.0);

        //Foregound positions are distrobuted by the following curve: https://www.desmos.com/calculator/ln2sf8feza
        //The probablity of them striking any position on the level follows a sine curve where the levels width = x
        double y = x;

        y -= 0.5;
        y *= 2;
        y = Math.Asin(y);
        y /= Math.PI;

        y += 0.5;

        //Maps y: 0-1 => (-width/2) => width/2
        return Command.Map(0.0, 1.0, -(width/2), width/2, y);
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