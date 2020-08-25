using Godot;
using System;
using System.Collections.Generic;

namespace MiorgaFight {

//Manages moving players in and out of the game
//And the lobby UI scene which the game starts on
public class Lobby : Control {
	public enum MultiplayerRole {
		SPECTATOR, P1, P2, OFFLINE, HOST
	}

	public enum GameState {
		//Waiting for player data from server (upon joining)
		WAITING, 
		//Sat in character selection
		CHAR_SELECTION
	}

	public const int PORT = 6785;

    public static bool mp = false;
    public static bool started = false;

	LineEdit nodeAddr;
	Button nodeHostButton, nodeJoinButton, nodeLocalButton, nodeErrorButton, nodeQuitButton;
	Level game;
	Panel nodeErrorPanel, nodeStartPanel;
	Label nodeErrorLabel;

	//The current state of this client
	private GameState state;

	public MultiplayerRole role;

	//Is the creation of the game object deferred
	//Used to work around the fact not enough of the rest of the scene will have loaded before Ready is run
	//This should be deprecated soon
	private bool deferred;

	private PackedScene p1Scene, p2Scene;

	private Player p1, p2;

	//MP ID's of players p1 and p2, or -1 if waiting for server to set, 0 if none
	public int p1Id, p2Id;

	private NetworkedMultiplayerENet peer;

	public Lobby() {
		this.deferred = false;
		Command.lobby = this;
	}

	public override void _Ready()
	{
		this.role = MultiplayerRole.OFFLINE;
		this.state = GameState.WAITING;

		this.nodeAddr = GetNode<LineEdit>("pa_start/tx_address");    
		
		this.nodeHostButton = GetNode<Button>("pa_start/bt_host");
		this.nodeJoinButton = GetNode<Button>("pa_start/bt_join");
		this.nodeLocalButton = GetNode<Button>("pa_start/bt_local");
		this.nodeErrorButton = GetNode<Button>("pa_error/bt_error");
		this.nodeQuitButton = GetNode<Button>("bt_quit");

		this.nodeErrorPanel = GetNode<Panel>("pa_error");
		this.nodeStartPanel = GetNode<Panel>("pa_start");

		//Continue through pauses
		this.PauseMode = Node.PauseModeEnum.Process;

		this.nodeErrorLabel = GetNode<Label>("pa_error/la_error");

		this.nodeHostButton.Connect("pressed", this, nameof(_OnHostPressed));
		this.nodeJoinButton.Connect("pressed", this, nameof(_OnJoinPressed));
		this.nodeLocalButton.Connect("pressed", this, nameof(_OnLocalPressed));
		this.nodeErrorButton.Connect("pressed", this, nameof(_OnErrorPressed));
		this.nodeQuitButton.Connect("pressed", this, nameof(_OnQuitPressed));

		GetTree().Connect("network_peer_connected", this, nameof(_PlayerConnected));
		GetTree().Connect("network_peer_disconnected", this, nameof(_PlayerDisconnected));
		GetTree().Connect("connected_to_server", this, nameof(_ConnectedToServer));
		GetTree().Connect("connection_failed", this, nameof(_ConnectionFailed));
		GetTree().Connect("server_disconnected", this, nameof(_ServerDisconnected));

		this.RpcConfig(nameof(this.AddPlayer), MultiplayerAPI.RPCMode.Puppetsync);
		this.RpcConfig(nameof(this.SetPlayerId), MultiplayerAPI.RPCMode.Remotesync);

		//Host at start
		foreach (String arg in OS.GetCmdlineArgs()) {
			if (arg == "--host") {
				GD.Print("Server will run as host");
				this.deferred = true;
				_OnHostPressed();
			}
		}
	}


	//Called when a player connects (duh)	
	void _PlayerConnected(int id) {
		GD.Print(id.ToString() + " connected!");

		//Don't let the server get in here (I don't think it can anyway tbf)
		if (id == 1) return;

		if (GetTree().GetNetworkUniqueId() == 1) {
			//If p1 or p2 change, push changes to all clients
			if (this.p1Id == 0) {
				this.p1Id = id;
				Rpc(nameof(this.SetPlayerId), new object[] {GameState.CHAR_SELECTION, this.p1Id, this.p2Id});
			} else if (this.p2Id == 0) {
				this.p2Id = id;
				Rpc(nameof(this.SetPlayerId), new object[] {GameState.CHAR_SELECTION, this.p1Id, this.p2Id});
			}

			//Otherwise just current ids to new player
			RpcId(id, nameof(this.SetPlayerId), new object[] {GameState.CHAR_SELECTION, this.p1Id, this.p2Id});
			//Send the new player any confirmations that may have been made in char selection
			if (this.state == GameState.CHAR_SELECTION) {
				CharSelection cs = GetTree().Root.GetNode<CharSelection>("char_selection");
				if (cs.mpP1Confirmed) 
					cs.RpcId(id, nameof(cs.Confirm), new object[] {Lobby.MultiplayerRole.P1, cs.p1.selection});
				if (cs.mpP2Confirmed) 
					cs.RpcId(id, nameof(cs.Confirm), new object[] {Lobby.MultiplayerRole.P2, cs.p2.selection});
			}
		}
	}

	/*
	//Called when a player connects
	//Adds that player to the game
	void _PlayerConnected(int id) {
		GD.Print(id.ToString() + " connected!");

		if (this.deferred) {
			this.GameCreate();
			this.deferred = false;
		}

		//Don't let the server connect
		if (id == 1) return;

		//If server
		if (GetTree().IsNetworkServer()) {
			//No p1, create p1
			if (! this.game.HasNode("p1")) {
				Rpc(nameof(AddPlayer), new object[] {"p1", id});
				GD.Print(id.ToString() + " is P1!");
				
				//if there is a node 2, add that
				if (this.game.HasNode("p2")) {
					RpcId(id, nameof(AddPlayer), new object[] {"p2", this.game.GetNode("p2").GetNetworkMaster()});
				}

				return;
			} else {
				//if there is a p1, add it to that players game
				RpcId(id, nameof(AddPlayer), new object[] {"p1", this.game.GetNode("p1").GetNetworkMaster()});
			}
	
			if (! this.game.HasNode("p2")) {
				Rpc(nameof(AddPlayer), new object[] {"p2", id});
				GD.Print(id.ToString() + " is P2!");
				return;
			} else {
				RpcId(id, nameof(AddPlayer), new object[] {"p2", this.game.GetNode("p2").GetNetworkMaster()});
			}

			GD.Print(id.ToString() + " is spectator!");
		}
	}*/

	/*
	void _PlayerDisconnected(int id) {
		if (this.p1 != null && this.p1.GetNetworkMaster() == id) {
			//P1 has disconnected
			this.GameEnd();
			this.RemovePlayer(this.p1);
			this.p1 = null;
			GD.Print("P1 disconnected");
			return;
		} else if (this.p2 != null && this.p2.GetNetworkMaster() == id) {
			//P1 has disconnected
			this.GameEnd();
			this.RemovePlayer(this.p2);
			this.p2 = null;
			GD.Print("P2 disconnected");
			return;
		} 
	}*/

	void _PlayerDisconnected(int id) {
		if (GetTree().GetNetworkUniqueId() == 1) {
			//Force everyone back to lobby (if you're the server)
			if (id == this.p1Id) {
				Rpc(nameof(this.SetPlayerId), new object[] {GameState.CHAR_SELECTION, this.GetFirstSpectator(), this.p2Id});
			} else if (id == this.p2Id) {
				Rpc(nameof(this.SetPlayerId), new object[] {GameState.CHAR_SELECTION, this.p1Id, this.GetFirstSpectator()});
			}
		}
	}

	void _ConnectedToServer() {
		//Lobby.mp = true;
		//this.p1Id = -1;
		//this.p2Id = -1;

		this.nodeErrorPanel.Visible = false;
		this.nodeStartPanel.Visible = true;
		this.Visible = false;
		Lobby.mp = true;

		/*
		foreach (int id in GetTree().GetNetworkConnectedPeers()) {
			this._PlayerConnected(id);
		}

		this._PlayerConnected(GetTree().GetNetworkUniqueId());*/
	} 

	void _ConnectionFailed() {
		this.ShowError("Error: Unable to connect\nPerhaps there is no server running?");
	}

	void _ServerDisconnected() {
		this.Reset();
	}

	void _GameOver(String error = "") {
		Node game = GetNode("/root/game");
		if (game != null) {
			game.Free();
			this.Visible = true;
		}

		GetTree().NetworkPeer = null;
		GD.Print(error);
	}

	void _OnHostPressed() {
		//Create peer
		this.peer = new NetworkedMultiplayerENet();
		this.peer.CompressionMode = NetworkedMultiplayerENet.CompressionModeEnum.RangeCoder;    
		Error error = this.peer.CreateServer(PORT, 8);

		if (error != Error.Ok) {
			this.ShowError("Error: Unable to host server\nPerhaps there is alreay a server open?");
			return;
		}

		Lobby.mp = true;
		this.p1Id = 0;
		this.p2Id = 0;
		GetTree().NetworkPeer = this.peer;
		this.state = GameState.CHAR_SELECTION;
		this.role = MultiplayerRole.HOST;
	
		this.Visible = false;

		//Goto CS as spectator
		CharSelection cs = (ResourceLoader.Load("res://scenes/ui/char_selection/char_selection.tscn") as PackedScene)
				.Instance() as CharSelection;
		GetTree().Root.AddChild(cs);
		//Set the character selection up for multiplayer, with this as a spectator
		cs.SetMp(this.role);
		cs.SetCallback(this._MpCSCallback);
	}

	void _OnJoinPressed() {
		String ip = IP.ResolveHostname(nodeAddr.Text);
		
		if (ip == "") {
			this.ShowError("Error: Invalid host!");
			return;
		}

		//Create host
		this.peer = new NetworkedMultiplayerENet();
		this.peer.CompressionMode = NetworkedMultiplayerENet.CompressionModeEnum.RangeCoder;    
		this.peer.CreateClient(ip, PORT);
		GetTree().NetworkPeer = this.peer;
		 
		this.ShowError("Connecting...", "Cancel");
		this.state = GameState.WAITING;
		this.role = MultiplayerRole.SPECTATOR;
	}

	//Host a regular, local game
	void _OnLocalPressed() {
		Lobby.mp = false;
		//Move to char selection
		this.Visible = false;
		CharSelection cs = (ResourceLoader.Load("res://scenes/ui/char_selection/char_selection.tscn") as PackedScene)
				.Instance() as CharSelection;
		cs.SetCallback(this._LocalCSCallback);
		GetTree().Root.AddChild(cs);
	}

	//Multiplayer character selection callback, stores the selected characters and moves on to map selection
	LevelSelection _MpCSCallback(CharSelection cs, PackedScene p1, PackedScene p2) {
		LevelSelection ls = this._LocalCSCallback(cs, p1, p2);
		ls.SetMp(this.role);
		ls.SetCallback(this._MPLSCallback);

		return ls;
	}

	//Local character selection callback, stores the selected characters and moves on to level selection
	//Returns the new level selection
	LevelSelection _LocalCSCallback(CharSelection cs, PackedScene p1, PackedScene p2) {
		//Removes the charselection
		GetTree().Root.RemoveChild(cs);

		//Stores player choices
		this.p1Scene = p1;
		this.p2Scene = p2;

		//Moves to level selection
		LevelSelection ls = (ResourceLoader.Load("res://scenes/ui/level_selection.tscn") as PackedScene).Instance() 
				as LevelSelection;
		ls.SetCallback(this._LocalLSCallback);
		GetTree().Root.AddChild(ls);

		return ls;
	}

	int _MPLSCallback(LevelSelection ls, PackedScene level) {
		//Remove level selection
		GetTree().Root.RemoveChild(ls);
		
		//Create and goto level
		this.GameCreate(level);
		this.GameGoto();

		//Add the players
		this.AddPlayer("p1", this.p1Id, this.p1Scene);
		this.AddPlayer("p2", this.p2Id, this.p2Scene);

		//Start the game
		this.GameStart();
		return 0;
	}


	int _LocalLSCallback(LevelSelection ls, PackedScene level) {
		//Remove level selection
		GetTree().Root.RemoveChild(ls);
		
		//Create and goto level
		this.GameCreate(level);
		this.GameGoto();

		//Add the players
		this.AddPlayer("p1", 0, this.p1Scene);
		this.AddPlayer("p2", 0, this.p2Scene);

		//Start the game
		this.GameStart();
		return 0;
	}

	//Called when the error panel's button is called
	//Either to cancel a connection or to accept an error
	void _OnErrorPressed() {
		if (this.peer != null) {
			//Cancel the connection
			this.peer.CloseConnection();
			this.peer = null;
			GetTree().NetworkPeer = null;
		}
		
		this.nodeStartPanel.Visible = true;
		this.nodeErrorPanel.Visible = false;
	}

	//Called when the quit button is pressed
	//Closes the game
	void _OnQuitPressed() {
		//I wanted to wire this directly, but it wouldn't let me
		GetTree().Quit();
	}

	//Called by the server once a client has connected, telling them how to set up their game
	private void SetPlayerId(GameState state, int p1Id, int p2Id) {
		if (p1Id == GetTree().GetNetworkUniqueId()) this.role = MultiplayerRole.P1;
		else if (p2Id == GetTree().GetNetworkUniqueId()) this.role = MultiplayerRole.P2;
		else if (1 == GetTree().GetNetworkUniqueId()) this.role = MultiplayerRole.HOST;
		else this.role = MultiplayerRole.SPECTATOR;

		//One of the players has disconnected, move back to lobby
		if ((this.p1Id != p1Id) || (this.p2Id != p2Id)) {
			this.state = state;
			this.p1Id = p1Id;	
			this.p2Id = p2Id;
			this.ResetToCharSelection();
		
		} else if (this.state == GameState.WAITING) { //This player has just joined
				
			//Goto CS as spectator
			CharSelection cs = (ResourceLoader.Load("res://scenes/ui/char_selection/char_selection.tscn") as PackedScene)
					.Instance() as CharSelection;
			GetTree().Root.AddChild(cs);
			//Set the character selection up for multiplayer, with this as a spectator
			cs.SetMp(this.role);
			cs.SetCallback(this._MpCSCallback);
		} 

		if (state == GameState.CHAR_SELECTION) { //if in char selection, pass through
			GetTree().Root.GetNode<CharSelection>("char_selection")
					.PlayersUpdated(p1Id != 0 ? true : false, p2Id != 0 ? true : false);
		}

		this.state = state;
		this.p1Id = p1Id;	
		this.p2Id = p2Id;
	}

	//Starts the game
	//Once both players are already in the game
	public void GameStart() {
		Lobby.started = true;

		this.p1.Start(this.p2, this.game.GetNode("hud/gr_p1") as PlayerHUD);
		this.p2.Start(this.p1, this.game.GetNode("hud/gr_p2") as PlayerHUD);
	}

	//Ends the game, leaving both players in the game
	private void GameEnd() {
		//If the game has started, end it
		if (Lobby.started) {
			this.p1.End();
			this.p2.End();
			Lobby.started = false;
		}
	}

	//Creates a game
	private void GameCreate() {
		//Load game
		this.game = ((ResourceLoader.Load("res://scenes/level/marketharbour.tscn") as PackedScene).Instance()) as Level;
	}

	private void GameCreate(PackedScene map) {
		this.game = (map.Instance()) as Level;
	}

	//Goes to the game, from the lobby
	public void GameGoto() {
		GetTree().Root.AddChild(this.game);
		this.Visible = false;
		Input.SetMouseMode(Input.MouseMode.Hidden);
	}

	/*
	//Quits the game
	public void GameQuit() {
		this.GameEnd();
		if (this.p1 != null) {
			this.RemovePlayer(this.p1);
			this.p1 = null;
		}
		if (this.p2 != null) {
			this.RemovePlayer(this.p2);

			this.p2 = null;
		}

		if (Lobby.mp) {
			Lobby.mp = false;
			//Close the mulitplayer connection
			this.peer.CloseConnection();
			this.peer = null;
			GetTree().NetworkPeer = null;
		}

		//Remove the game
		GetTree().Root.RemoveChild(this.game);
		this.game.Dispose();
		this.game = null;

		this.Visible = true;

		(GetNode("/root/Command") as Command).PauseEnd();
		Input.SetMouseMode(Input.MouseMode.Visible);
	}*/

	public void AddPlayer(String name, int id, PackedScene player) {
		Player _new = player.Instance() as Player;
		_new.Name = name;
		
		_new.SetNetworkMaster(id);
		
		if (! Lobby.mp) {
			_new.controls = (name == "p1") ? Player.ControlMethod.PLAYER1 : Player.ControlMethod.PLAYER2; 
		} else {
			_new.controls = (id == GetTree().GetNetworkUniqueId()) ? 
					Player.ControlMethod.PLAYER1 : Player.ControlMethod.REMOTE;
		}

		//Position specific stuff
		if (name == "p1") { 
			this.p1 = _new;
			_new.DIRECTION = Player.Direction.RIGHT;
		} else if (name == "p2") {
			this.p2 = _new;
			_new.DIRECTION = Player.Direction.LEFT;
		}
		
		_new.Position = this.game.GetPlayerPosition(_new.DIRECTION);
		
		this.game.AddChild(_new);
		(this.game.GetNode("camera_track") as CameraTrack).Track(_new);
	}

	//Scrap everything currently going on
	//Keep the lobby 
	public void ResetToCharSelection() {
		this.RemoveAll();

		//Reset state and action as if this client has just connected
		this.state = GameState.WAITING;
		this.SetPlayerId(GameState.CHAR_SELECTION, this.p1Id, this.p2Id);
	}

	//Reset all the way back to title screen
	public void Reset() {
		this.RemoveAll();

		//Reset everything
		GetTree().ChangeScene("res://scenes/ui/lobby.tscn");
	}

	//Returns the id of the first spectator connected to this game, or 0 if there are none
	private int GetFirstSpectator() {
		foreach (int id in GetTree().GetNetworkConnectedPeers()) {
			if (id != 1 && id != this.p1Id && id != this.p2Id) return id;
		}

		//None, return 0
		return 0;
	}

	//Removes everything other than this and Command from the root node
	private void RemoveAll() {
		foreach (Node n in GetTree().Root.GetChildren()) {
			if (n is Command || n == this) continue;
			GetTree().Root.RemoveChild(n);
			n.Free();
		}
	}

	//Enable the error panel and have it display the given message
	private void ShowError(string msg, string buttonmsg = "Ok") {
		this.nodeStartPanel.Visible = false;
		this.nodeErrorPanel.Visible = true;
		this.nodeErrorLabel.Text = msg;
		this.nodeErrorButton.Text = buttonmsg;
	}
}}
