using Godot;
using System;

public abstract class Action : Resource
{
    public enum Type{
        ATTACK,
        PARRY,
        VOID
    }

    //Trigger data 
    //Input action that causes action
    [Export] public String triggerInput;

    //Animation which must be playing for action to be triggered
    [Export] public String triggerAnimation;

    //Frames which must be playing in order for action to be triggered
    //Set min to -1 to allow for any trigger
    [Export] public int triggerMinFrame;
    [Export] public int triggerMaxFrame;

    [Export] public String animation;

    //Movement performed once the parry has concluded
    [Export] public Vector2 movement;

    //Trans animation
    [Export] public String transition;

    [Export] public Player.State transitionTo;

    [Export] public bool animateTransition;

    public Type type;

    public abstract void Start(Player player);

    
    //Cuts an attack short, stopping it where it is
    //Does not play the transition animation
    public void Cut(Player player) {
        player.ChangeState(player.GetStateFromStance(player.stance));
    }

    public void End(Player player) {
        player.MoveAndCollide(this.movement * player.SCALEFACTOR);
    
        if (this.transitionTo != Player.State.NONE) {
            //player.ChangeStance(player.GetStanceFromState(this.transitionTo), this.animateTransition);
            if (this.animateTransition) {
                player.stance = player.GetStanceFromState(this.transitionTo);
                player.TransitionTo(transition, this.transitionTo);
            } else {
                //Transition without animation
                player.ChangeState(this.transitionTo);
            }
        } else {
            player.ChangeState(player.GetStateFromStance(player.stance));
        }   
    }

    //Returns whether all trigger conditions have been met or not
    public bool IsPossible(Player player, InputEvent input) {
        //I don't like the way this works

        //Colossal if statement
        //Sorry
        if (input.IsActionPressed(player.prefix + this.triggerInput) && //Correct action has been pressed 
            player.nodeAnimateSprite.Animation == this.triggerAnimation && //Correct animation is playing
            ((this.triggerMinFrame == -1) || //Checks if min frame is set to -1
                (player.nodeAnimateSprite.Frame >= this.triggerMinFrame && //OR players current frame >= min frame 
                player.nodeAnimateSprite.Frame <= this.triggerMaxFrame))) //AND players current frame ,= max frame

            return true;
         else
            return false;
    }
}
