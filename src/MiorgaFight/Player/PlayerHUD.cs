using Godot;
using System;

namespace MiorgaFight {

public class PlayerHUD : Control
{
    //Amount of base healthbar dealy (when damage = 0)
    [Export] private float healthbarDelayStart;

    //Delay regression
    [Export] private float healthbarDelayRegression;

    //Healthbar delay length = start + (damage / regression)

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
        //If hp is increasing then do so immediately
        if (hp > this.nodeHP.Value) {
            this.nodeHP.Value = hp;
            this.nodeHPBacklay.Value = hp;
            return;
        }

        double damage = this.nodeHP.Value - (double) hp;
        this.nodeHP.Value = hp;

        //Character is dead, do not add backlay timer
        if (hp <= 0) return;

        //Setup timer to update the backlay
        SceneTreeTimer timer = GetTree().CreateTimer((
                float) (this.healthbarDelayStart + damage / this.healthbarDelayRegression), false);
        timer.Connect("timeout", this, nameof(SetBacklayValue), new Godot.Collections.Array(new object[] {hp}));
    }

    void SetBacklayValue(int value) {
        this.nodeHPBacklay.Value = value;
    }
}}
