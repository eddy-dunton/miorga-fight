using Godot;
using System;

namespace MiorgaFight {

public class HUD : CanvasLayer
{
	[Export] private Texture p1Card;
	[Export] private Texture p2Card;

	//The actual cards sprite
	private AnimatedSprite nodeCardsSprite;
	//The animation player (responsible for focusing the sprites out)
	private AnimationPlayer nodeCardsPlayer;

	private Sprite nodeCardsPlayerWin;

	private Control nodeGroupP1, nodeGroupP2, nodeMobileControls;

	public override void _Ready()
	{
		this.nodeCardsSprite = this.GetNode<AnimatedSprite>("sp_cards");
		//Ensure nodeCards sprite is off when starting
		this.nodeCardsSprite.Play("default");
		this.nodeCardsPlayer = this.GetNode<AnimationPlayer>("an_cards");    
		this.nodeCardsPlayerWin = this.GetNode<Sprite>("sp_win_player");
		//Ensure that this is hidden
		this.nodeCardsPlayerWin.Visible = false;

		this.nodeGroupP1 = this.GetNode<Control>("gr_p1");
		this.nodeGroupP2 = this.GetNode<Control>("gr_p2");
		if (Command.IsMobile()) this.nodeMobileControls = this.GetNode<Control>("pa_mobile_controls");
	}

	public void ChangeRound(int round) {
		if (round == 1) { //case first round, play the fight graphic
			this.nodeCardsSprite.Play("go");
			this.nodeCardsPlayer.Play("cards_fade_long");
		} else if (round == 11) { //Final round, play the final round graphic
			this.nodeCardsSprite.Play("final");
			this.nodeCardsPlayer.Play("cards_fade_long");
		} else {
			this.nodeCardsSprite.Play("round" + round.ToString());
			this.nodeCardsPlayer.Play("cards_fade");
		}
	}

	public void Win(Player.Direction winnerDir) {
		//Set the player win card to the correct texture
		this.nodeCardsPlayerWin.Visible = false;
		this.nodeCardsPlayerWin.Texture = winnerDir == Player.Direction.RIGHT ? p1Card : p2Card;

		//Gets node cards sprite set up correctly
		this.nodeCardsSprite.Modulate = new Color(255,255,255,255);
		this.nodeCardsSprite.Playing = false;
		this.nodeCardsSprite.Animation = "default";

		//Hide the rest of the HUD
		this.nodeGroupP1.Visible = false;
		this.nodeGroupP2.Visible = false;
		if (Command.IsMobile()) this.nodeMobileControls.Visible = false;

		this.nodeCardsPlayer.Play("win");
	}

	public void ResetToLobby() {
		Command.lobby.ResetToLobby(Command.lobby.p1Id, Command.lobby.p2Id, Lobby.highLatency);
	}
}}
