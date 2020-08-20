using Godot;
using System;

namespace MiorgaFight {

public class PlayerHUD : Control
{
    public HUD parent;
    public TextureProgress nodeHP;
    public Sprite[] lives; 

    public override void _Ready() {
        //Get nodes
        this.nodeHP = this.GetNode<TextureProgress>("pb_health");
        
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
}}
