using Godot;
using System;

namespace MiorgaFight {

public class Overlay : Action {
    public enum OverlayOption {
        OVERLAY_TRACK,
        OVERLAY_NOTRACK
    }

    [Export] public OverlayOption overlay;

    [Export] public SoundEffect feintSound;

    //Cancel the players current action
    [Export] public bool cancel;

    private AnimatedSprite sprite;

    public override void Start(Player player) {
        //Stupid I know, but the only way to do it 
        switch (this.overlay) {
            case OverlayOption.OVERLAY_TRACK:
                player.nodeOverlayTrack.Play(this.animation);
                break;
            case OverlayOption.OVERLAY_NOTRACK:
                player.nodeOverlayNoTrack.Play(this.animation);
                break;
        }

        if (this.cancel) {
           player.PlaySfx(this.feintSound);
           this.End(player);
        }
    }

    //Should be immpossible
    public new void Cut(Player player) {}
}}