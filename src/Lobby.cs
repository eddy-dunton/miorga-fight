using Godot;
using System;
using System.Collections.Generic;

//Manages moving players in and out of the game
//And the lobby UI scene which the game starts on
public class Lobby : Control {
	public const int PORT = 6785;

    public static bool mp = false;
    public static bool started = false;

	LineEdit nodeAddr;
	Button nodeHostButton, nodeJoinButton, nodeLocalButton, nodeErrorButton;
	Level game;
	Panel nodeErrorPanel, nodeStartPanel;
	Label nodeErrorLabel;

	//Is the creation of the game object deferred
	//Used to work around the fact not enough of the rest of the scene will have loaded before Ready is run
	private bool deferred;

	private Player p1, p2;

	private NetworkedMultiplayerENet peer;

	public Lobby() {
		this.deferred = false;
		Command.lobby = this;
	}

	public override void _Ready()
	{
		this.nodeAddr = GetNode<LineEdit>("pa_start/tx_address");    
		
		this.nodeHostButton = GetNode<Button>("pa_start/bt_host");
		this.nodeJoinButton = GetNode<Button>("pa_start/bt_join");
		this.nodeLocalButton = GetNode<Button>("pa_start/bt_local");
		this.nodeErrorButton = GetNode<Button>("pa_error/bt_error");

		this.nodeErrorPanel = GetNode<Panel>("pa_error");
		this.nodeStartPanel = GetNode<Panel>("pa_start");

		this.nodeErrorLabel = GetNode<Label>("pa_error/la_error");

		this.nodeHostButton.Connect("pressed", this, nameof(_OnHostPressed));
		this.nodeJoinButton.Connect("pressed", this, nameof(_OnJoinPressed));
		this.nodeLocalButton.Connect("pressed", this, nameof(_OnLocalPressed));
		this.nodeErrorButton.Connect("pressed", this, nameof(_OnErrorPressed));

		GetTree().Connect("network_peer_connected", this, nameof(_PlayerConnected));
		GetTree().Connect("network_peer_disconnected", this, nameof(_PlayerDisconnected));
		GetTree().Connect("connected_to_server", this, nameof(_ConnectedToServer));
		GetTree().Connect("connection_failed", this, nameof(_ConnectionFailed));
		GetTree().Connect("server_disconnected", this, nameof(_ServerDisconnected));

		this.RpcConfig(nameof(AddPlayer), MultiplayerAPI.RPCMode.Puppetsync);

		//Host at start
		foreach (String arg in OS.GetCmdlineArgs()) {
			if (arg == "--host") {
				GD.Print("Server will run as host");
				this.deferred = true;
				_OnHostPressed();
			}
		}
	}

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
	}

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
	}

	void _ConnectedToServer() {
		this.GameCreate();
		Lobby.mp = true;

		this.nodeErrorPanel.Visible = false;
		this.nodeStartPanel.Visible = true;
		this.Visible = false;

		foreach (int id in GetTree().GetNetworkConnectedPeers()) {
			this._PlayerConnected(id);
		}

		this._PlayerConnected(GetTree().GetNetworkUniqueId());
	} 

	void _ConnectionFailed() {
		this.ShowError("Error: Unable to connect\nPerhaps there is no server running?");
	}

	void _ServerDisconnected() {
		this.GameQuit();
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
		GetTree().NetworkPeer = this.peer;
	
		this.Visible = false;

		if (! this.deferred)
			this.GameCreate();
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
		
		//Also will allow the connection of 
		this.ShowError("Connecting...", "Cancel");
	}

	//Host a regular, local game
	void _OnLocalPressed() {
		this.GameCreate();
		//this.AddPlayer("p1", 0);
		//this.AddPlayer("p2", 0);

		//Start game
		//this.GameStart();
		this.Visible = false;
		CharSelection cs = 
				(ResourceLoader.Load("res://scenes/ui/char_selection.tscn") as PackedScene).Instance() as CharSelection;
		cs.SetCallback(this);

		GetTree().Root.AddChild(cs);
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
		this.game = ((ResourceLoader.Load("res://scenes/level/holytree.tscn") as PackedScene).Instance()) as Level;
	}

	//Goes to the game, from the lobby
	public void GameGoto() {
		GetTree().Root.AddChild(this.game);
		this.Visible = false;
		Input.SetMouseMode(Input.MouseMode.Hidden);
	}


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
	}

	void AddPlayer(String name, int id) {
		this.AddPlayer(name, id, ResourceLoader.Load("res://scenes/player/regia.tscn") as PackedScene);
	}

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
		

		if (this.p1 != null && this.p2 != null) {
			this.GameStart();
		}
	}

	//Removes a player from the game
	//Player pointer must also then be set to null
	void RemovePlayer(Player p) {
		this.game.RemoveChild(p);
		(this.game.GetNode("camera_track") as CameraTrack).StopTrack(p);
		p.Dispose();
	}

	//Enable the error panel and have it display the given message
	private void ShowError(string msg, string buttonmsg = "Ok") {
		this.nodeStartPanel.Visible = false;
		this.nodeErrorPanel.Visible = true;
		this.nodeErrorLabel.Text = msg;
		this.nodeErrorButton.Text = buttonmsg;
	}
}
