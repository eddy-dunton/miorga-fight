using Godot;
using System;

public abstract class Action : Resource
{
    public enum Type{
        ATTACK,
        PARRY,
        VOID
    }

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
}
