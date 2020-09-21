using Godot;
using System;

namespace MiorgaFight {

public class PlayerHUD : Control
{
    [Export] private float healthbarDelay;

    public HUD parent;
    public Sprite[] lives; 

    private TextureProgress nodeHP;
    private ProgressBar nodeHPBacklay;
    
    public override void _Ready() {
        //Get nodes
        this.nodeHP = this.GetNode<TextureProgress>("pb_health");
        this.nodeHPBacklay = this.GetNode<ProgressBar>("pb_health/pb_backlay");

        //Populate lives
        this.lives = new Sprite[5];
        for (int i = 1; i <= 5; i ++) {
            this.lives[i - 1] = this.GetNode<Sprite>("sp_lives_" + i.ToString());
        }

        this.parent = this.GetParent() as HUD;

        //Be invisible on creation
        this.Visible = false;
    }

    public void SetLives(int lives) {
        //Map lives to array index of this.lives (1-5 > 0-4)
        lives --;
        //Set each sprite number below the lives to be visible
        for (int i = 0; i < lives; i ++) {
            this.lives[i].Visible = true;
        }
        //Set all the sprites above to be invisible
        for (int i = 4; i > lives; i --) {
            this.lives[i].Visible = false;    
        }
    }

    //Sets the max value of the health bar
    public void SetMaxHealth(int hp) {
        this.nodeHP.MaxValue = hp;
        this.nodeHPBacklay.MaxValue = hp;
    }

    //Sets the current value of the health bar (and runs the backlay)
    public void SetHealth(int hp) {
        this.nodeHP.Value = hp;

        //Setup timer to update the backlay
        SceneTreeTimer timer = GetTree().CreateTimer(this.healthbarDelay, false);
        timer.Connect("timeout", this, nameof(SetBacklayValue), new Godot.Collections.Array(new object[] {hp}));
    }

    //Resets health bar and the backlay to its max value
    //Not actually used
    public void ResetHealth() {
        this.nodeHP.Value = this.nodeHP.MaxValue;
        this.nodeHPBacklay.Value = this.nodeHPBacklay.MaxValue;
    }

    void SetBacklayValue(int value) {
        this.nodeHPBacklay.Value = value;
    }
}}
