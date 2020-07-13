using Godot;
using System;
using System.Collections.Generic;

public class Player : KinematicBody2D, CameraTrack.Trackable {
	//Players direction, also deciedes whether they are p1 or p2
	//Right is always p1 and left is always p2
	public enum Direction {RIGHT, LEFT}

	public enum State {NONE, LOW, HIGH, LAX, WALK, ATTACK, PARRY, TRANS}

	public enum ControlMethod {PLAYER1, PLAYER2, REMOTE}

	//private static Vector2 FLIP = new Vector2(-1, 1);
	//private static Vector2 NONFLIP = new Vector2(1, 1);

	[Export] string LAX_SPRITE = "lax";
	[Export] string LOW_SPRITE = "low";
	[Export] string HIGH_SPRITE = "high";

	[Export] List<Action> actions;

	[Export] int SPEED;

	[Export] int HP_MAX;

	[Export] public Direction DIRECTION;

	[Export] public ControlMethod controls;

	Stance STANCE_LAX, STANCE_LOW, STANCE_HIGH;

	public Stance stance;

	public State state;

	//State to move to once transition is over
	private State endTrans;

	//The players current attack, null if not attacking
	public Attack attack;
	//Players current parry, only valid if the player is parrying
	public Parry parry;

	private int hp;
	//Seems like a getter should be able to do this, but I couldn't the autogenerated one to work properly
	public int GetHP() {return hp;}

	private bool started;

	public bool mp;

	//Nodes
	public PlayerAnimation nodeAnimateSprite;
	public CollisionShape2D nodeCollision;
	public Player nodeEnemy;
	public Particles2D nodeSparks;
	public PlayerOverlay nodeOverlayTrack;
	public PlayerOverlay nodeOverlayNoTrack;

	public String inputPrefix;

	private HPBar hpBar;
	private Vector2 velocity;
	private Vector2 lastVelocity;

	//These are declared based on the players direction
	public string ACTION_UP, ACTION_DOWN, ACTION_LEFT, ACTION_RIGHT; 

	//1 if the player is facing right and -1 if facing left
	//Seems redundant but I couldn't get the enums to play nicely with the editor
	public Vector2 SCALEFACTOR;


	public Player() {
		this.attack = null;
		this.parry = null;
		this.started = false;
		this.lastVelocity = new Vector2();
		this.mp = false;
	}

	public override void _Ready() {
		GD.Print("Player ready");
		this.nodeAnimateSprite = GetNode<AnimatedSprite>("animate_sprite") as PlayerAnimation;
		this.nodeCollision = GetNode<CollisionShape2D>("collision");
		this.nodeSparks = GetNode<Particles2D>("animate_sprite/sparks");
		this.nodeOverlayTrack = GetNode<AnimatedSprite>("animate_sprite/overlay_track") as PlayerOverlay;
		this.nodeOverlayNoTrack = GetNode<AnimatedSprite>("overlay_notrack") as PlayerOverlay;

		//Load stances
		STANCE_LAX = new Stance(LAX_SPRITE);
		//Creates attacks if they exist
		STANCE_LOW = new Stance(LOW_SPRITE);
		STANCE_HIGH = new Stance(HIGH_SPRITE);

		this.ChangeState(State.LAX);

		this.SCALEFACTOR = new Vector2((this.DIRECTION == Direction.RIGHT) ? 1 : -1, 1);

		//Set up actions
		if (! this.mp) {
			if (this.controls == ControlMethod.PLAYER1) {this.inputPrefix = "p1_";}
			else {this.inputPrefix = "p2_";}
		} else {//Is multiplayer
			GD.Print("is mp");
			if (this.controls == ControlMethod.PLAYER1) { //Master
				this.inputPrefix = "p1_";
				RsetConfig("position", MultiplayerAPI.RPCMode.Puppetsync);
				RpcConfig(nameof(this.ChangeState), MultiplayerAPI.RPCMode.Puppet);
				RpcConfig(nameof(this.ActionStart), MultiplayerAPI.RPCMode.Puppetsync);
			} else { // Puppet
				this.inputPrefix = "remote_";
				RsetConfig("position", MultiplayerAPI.RPCMode.Puppet);
				RpcConfig(nameof(this.ChangeState), MultiplayerAPI.RPCMode.Puppet);
				RpcConfig(nameof(this.ActionStart), MultiplayerAPI.RPCMode.Puppet);
			}
		}

		this.ACTION_UP = this.inputPrefix + "up";
		this.ACTION_DOWN = this.inputPrefix + "down";
		this.ACTION_LEFT = this.inputPrefix + "left";
		this.ACTION_RIGHT = this.inputPrefix + "right";

		//Flip if necessary
		if (this.DIRECTION == Direction.LEFT) this.Flip();
	}

	//public void SetPosition(Vector2 pos) {
	//	this.Position = pos;
	//}

	//Called once both players exist, points players towards their HP bar and the enemy
	//Must be called externally
	public void Start(Player enemy, HPBar hpbar) {
		this.hpBar = hpbar;
		this.hpBar.MaxValue = this.HP_MAX;
		this.hpBar.Visible = true;
		this.ChangeHP(this.HP_MAX);

		this.nodeEnemy = enemy;
		this.started = true;
	}

	public override void _Input(InputEvent inputEvent) {
		if (this.controls == ControlMethod.REMOTE) return; //Do not check for inputs for remote objects

		if (inputEvent.IsActionPressed("debug")) {	
			if (true) {}; //Debug breakpoint
		}

		//No actions if the game hasn't started yet
		if (! this.started) return;

		//Check for actions
		foreach (Action action in this.actions) {
			if (action.IsPossible(this, inputEvent)) {
				if (! this.mp) {
					action.Start(this);
				} else {
					RpcUnreliable(nameof(this.ActionStart), new object[] {this.actions.IndexOf(action)});
				}
				//Mark input as dealt with
				GetTree().SetInputAsHandled();
				return;
			}
		}		

		//Check for up / down calls
		if (this.state == State.ATTACK || this.state == State.PARRY || this.state == State.TRANS) return;

		if (inputEvent.IsActionPressed(this.ACTION_UP)) {
			if (this.state == State.HIGH) {
				this.ChangeState(State.LAX);
			} else {
				this.ChangeState(State.HIGH);
			}
			GetTree().SetInputAsHandled();
		} else if (inputEvent.IsActionPressed(this.ACTION_DOWN)) {
			if (this.state == State.LOW) {
				this.ChangeState(State.LAX);
			} else {
				this.ChangeState(State.LOW);
			}
			GetTree().SetInputAsHandled();
		}
	}

	public override void _PhysicsProcess(float delta) {
		if (this.controls != ControlMethod.REMOTE) {
			if ((this.state == State.LAX || this.state == State.WALK)) {
				this.CalcMovement();
				if (this.velocity.x != 0) {
					//If player is actually moving
					MoveAndCollide(this.velocity * delta);
				
					//Player has just started moving
					if (this.state == State.LAX) this.ChangeState(State.WALK);
					//Player has changed direction        
					else if (Math.Sign(this.lastVelocity.x) != Math.Sign(this.velocity.x)){
						//restarts the walk
						this.WalkStart();
					}

				} else {
					//Player is not moving

					//Player has stopped moving
					if (this.state == State.WALK) this.ChangeState(this.GetStateFromStance(this.stance));
				}
			} else {
				//If neither player should be still
				this.velocity = new Vector2();
			}
		
			if (this.mp) RsetUnreliable("position", this.Position);
		}

		this.lastVelocity = this.velocity;
	}

	//Moves the player from one state to another, all state changes should be routed through here
	//A little unnecessary I know
	//Not completely sold on it myself, might end up being scrapped
	public void ChangeState(State newState) {
		//Perform actions for leaving state
		switch (this.state) {
			case State.WALK:
				this.WalkEnd();
				break;
		}
		
		//Check if a transition should be done
		//Not particularly proud on this 
		bool transition = ((this.state == State.LAX || this.state == State.LOW || this.state == State.HIGH || 
			this.state == State.WALK) && (newState == State.LAX || newState == State.LOW || newState == State.HIGH));

		this.state = newState;

		//Perform actions for entering 
		switch (this.state) {
			case State.WALK:
				this.WalkStart();
				break;
			case State.LAX:
				this.ChangeStance(STANCE_LAX, transition);
				break;
			case State.LOW:
				this.ChangeStance(STANCE_LOW, transition);
				break;
			case State.HIGH:
				this.ChangeStance(STANCE_HIGH, transition);
				break;
		}

		if (this.mp && this.controls != ControlMethod.REMOTE && this.state != State.ATTACK && this.state != State.PARRY) 
			RpcUnreliable(nameof(this.ChangeState), new object[] {this.state});
	}

	public void ChangeHP(int newhp) {
		//Ensures hp is not above max
		this.hp = Math.Min(this.HP_MAX, newhp);

		this.hpBar.Value = this.hp;
	}

	public void Knockback(int amount, bool animate = true) {
		this.MoveAndCollide(new Vector2(-amount * 2, 0) * this.SCALEFACTOR);	
		if (animate)
			this.Transition("flinch");
	}

	//Called when a player is damaged
	//Halting is true if the player received halting damage
	public void Hurt(int damage, bool halting = false) {
		if (halting || (this.state != State.ATTACK && this.state != State.PARRY)) {
			//Minus here as he's moving away from the damage
			if (halting) {
				if (this.state == State.ATTACK) this.attack.Cut(this);
				else if (this.state == State.PARRY) this.parry.Cut(this);
			}

			this.Knockback(damage);
		}

		this.nodeOverlayTrack.Play("blood_mid");
		this.ChangeHP(this.hp - damage);
	}

	//Returns the state of the given stance
	public State GetStateFromStance(Stance stance) {
		if (stance == STANCE_LOW) return State.LOW;
		else if (stance == STANCE_HIGH) return State.HIGH;
		else return State.LAX;
	}

	public Stance GetStanceFromState(State state) {
		if (state == State.LOW) return STANCE_LOW;
		else if (state == State.HIGH) return STANCE_HIGH;
		else return STANCE_LAX;
	}

	//Called when a transition has ended
	public void TransitionEnd() {
		this.state = this.endTrans;
		//Resets sprite
		//Might have to be changed later
		this.nodeAnimateSprite.Reset();
	}

	//Called when the player starts a transition
	public void Transition(string animation,  bool backwards = false) {
		//Ensures that player does not get stuck in transition if this called whilst the player is already transitioning
		if (this.state != State.TRANS) this.endTrans = this.state;
		this.state = State.TRANS;
		this.nodeAnimateSprite.Play(animation, backwards);
	}

	//Called when the player starts a transition
	public void TransitionTo(string animation, State state, bool backwards = false) {
		//Ensures that player does not get stuck in transition if this called whilst the player is already transitioning
		if (state != State.TRANS) this.endTrans = state;
		this.state = State.TRANS;
		this.nodeAnimateSprite.Play(animation, backwards);
	}

	//Only use this publiclly if you know what you're doing,
	//Normally stick to change state
	public void ChangeStance(Stance newStance, bool fromStance = false) {
		if (fromStance) {
			//Only perform transitions if moving from stance 
			if (this.stance == STANCE_LAX) {
				//Moving from lax to stance
				if (newStance == STANCE_HIGH) {
					//Lax -> high
					this.Transition("trans_lax->high");
				} else {
					//Lax -> low (no trans)
					this.nodeAnimateSprite.Play(newStance.sprite);
				}
			} else if (newStance == STANCE_LAX) {
				//Moving from stance to lax
				if (this.stance == STANCE_HIGH) {
					//High -> Lax
					this.Transition("trans_lax->high", true);
				} else {
					//Low -> Lax (no trans)
					this.nodeAnimateSprite.Play(newStance.sprite);
				}
			} else {
				if (this.stance == STANCE_LOW) {
					//Going between stances, plays forwards if going LOW -> HIGH, or backwards going HIGH -> LOW
					this.Transition("trans_low->high");
				} else if (this.stance == STANCE_HIGH) {
					this.Transition("trans_high->low");
				}
			}
			this.stance = newStance;
		} else {
			//Not coming from stance
			this.stance = newStance;
			this.nodeAnimateSprite.Reset();
		}
	}

	//Used for RPC calls
	private void ActionStart(int actionIndex) {
		this.actions[actionIndex].Start(this);
	}

	//Starts to walk 
	private void WalkStart() {
		this.nodeAnimateSprite.Play("walk", this.IsWalkingBackwards());
	}

	//Walk ends
	private void WalkEnd() {
		this.nodeAnimateSprite.Reset();
	}

	//True if the player is current walking in the opposite direction to that that they're facing
	public bool IsWalkingBackwards() {
		return Math.Sign(this.velocity.x) != this.SCALEFACTOR.x;
	}

	//Flips the player to face the opposite direction
	private void Flip() {
		this.Scale = this.SCALEFACTOR;
	}

	private void CalcMovement() {
		this.velocity = new Vector2();
		
		if (Input.IsActionPressed(this.ACTION_RIGHT))
			this.velocity.x += SPEED;
			
		if (Input.IsActionPressed(this.ACTION_LEFT))
			this.velocity.x -= SPEED;
	}
	
	Node2D CameraTrack.Trackable.GetTrackingNode() {
		//Gets camera to play nice
		return this.nodeCollision;
		//return this;
	}

	public class Stance {
		public String sprite;

		public Stance(String sprite) {
			this.sprite = sprite;
		}
	}
}
