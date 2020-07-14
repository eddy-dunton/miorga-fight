using Godot;
using System;
using System.Collections.Generic;

public class Lobby : Control {
    public const int PORT = 6785;

    LineEdit nodeAddr;
    Button nodeHostButton, nodeJoinButton, nodeLocalButton;
    Level game;

    //Is the creation of the game object deferred
    //Used to work around godots stupid restrictions
    private bool deferred;

    //List of all players in the order they joined
    private List<int> playerQueue;

    private Player p1, p2;

    public Lobby() {
        this.deferred = false;
        this.playerQueue = new List<int>();
    }

    public override void _Ready()
    {
        GD.Print("Starting Lobby");

        this.nodeAddr = GetNode<LineEdit>("LobbyPanel/Address");    
        this.nodeHostButton = GetNode<Button>("LobbyPanel/HostButton");
        this.nodeJoinButton = GetNode<Button>("LobbyPanel/JoinButton");
        this.nodeLocalButton = GetNode<Button>("LobbyPanel/LocalButton");

        this.nodeHostButton.Connect("pressed", this, nameof(_OnHostPressed));
        this.nodeJoinButton.Connect("pressed", this, nameof(_OnJoinPressed));
        this.nodeLocalButton.Connect("pressed", this, nameof(_OnLocalPressed));

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
            this.CreateGame();
            this.deferred = false;
        }

        //Don't let the server connect
        if (id == 1) return;

        //If server
        if (GetTree().IsNetworkServer()) {
            //Add player to queue
            this.playerQueue.Add(id);

            //No p1, create p1
            if (! this.game.HasNode("p1")) {
                Rpc(nameof(AddPlayer), new object[] {"p1", id, true});
                GD.Print(id.ToString() + " is P1!");
                
                //if there is a node 2, add that
                if (this.game.HasNode("p2")) {
                    RpcId(id, nameof(AddPlayer), new object[] {"p2", this.game.GetNode("p2").GetNetworkMaster(), true});
                }

                return;
            } else {
                //if there is a p1, add it to that players game
                RpcId(id, nameof(AddPlayer), new object[] {"p1", this.game.GetNode("p1").GetNetworkMaster(), true});
            }
    
            if (! this.game.HasNode("p2")) {
                Rpc(nameof(AddPlayer), new object[] {"p2", id, true});
                GD.Print(id.ToString() + " is P2!");
                return;
            } else {
                RpcId(id, nameof(AddPlayer), new object[] {"p2", this.game.GetNode("p2").GetNetworkMaster(), true});
            }

            GD.Print(id.ToString() + " is spectator!");
        }
    }

    void _PlayerDisconnected(int id) {
        if (this.p1.GetNetworkMaster() == id) {
            //P1 has disconnected
            this.RemovePlayer(this.p1);
            GD.Print("P1 disconnected");
            return;
        } else if (this.p2.GetNetworkMaster() == id) {
            //P1 has disconnected
            this.RemovePlayer(this.p2);
            GD.Print("P2 disconnected");
            return;
        } 
    }

    void _ConnectedToServer() {
        GD.Print("Joined! ID: " + GetTree().GetNetworkUniqueId().ToString());
        foreach (int id in GetTree().GetNetworkConnectedPeers()) {
            this._PlayerConnected(id);
        }

        this._PlayerConnected(GetTree().GetNetworkUniqueId());
    } 

    void _ConnectionFailed() {
        GD.PrintErr("Unable to connnect");
    }

    void _ServerDisconnected() {
        _GameOver("Server Disconnected");
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
        NetworkedMultiplayerENet host = new NetworkedMultiplayerENet();
        host.CompressionMode = NetworkedMultiplayerENet.CompressionModeEnum.RangeCoder;    
        Error error = host.CreateServer(PORT, 8);

        if (error != Error.Ok) {
            GD.PrintErr("Error hosting server");
            return;
        }

        GetTree().NetworkPeer = host;
    
        this.Visible = false;

        if (! this.deferred)
            this.CreateGame();

        GD.Print("Hosting...");
    }

    void _OnJoinPressed() {
        GD.Print("Joining...");
        this.CreateGame();

        String ip = nodeAddr.Text;
        
        if (!ip.IsValidIPAddress()) {
            GD.PrintErr("Invalid Address");
            return;
        }

        NetworkedMultiplayerENet host = new NetworkedMultiplayerENet();
        host.CompressionMode = NetworkedMultiplayerENet.CompressionModeEnum.RangeCoder;
        host.CreateClient(ip, PORT);
        GetTree().NetworkPeer = host;
    }

    //Host a regular, local game
    void _OnLocalPressed() {
        this.CreateGame();
        this.AddPlayer("p1", 0);
        this.AddPlayer("p2", 0);

        //Start game
        this.StartGame();
    }
    
    private void StartGame() {
        this.p1.Start(this.p2, this.game.GetNode("ui/health_p1") as HPBar);
        this.p2.Start(this.p1, this.game.GetNode("ui/health_p2") as HPBar);
    }

    void AddPlayer(String name, int id, bool mp = false) {
        Player _new = ((ResourceLoader.Load("res://scenes/player.tscn") as PackedScene).Instance()) as Player;
        _new.mp = mp;
        _new.Name = name;
        _new.SetNetworkMaster(id);
        if (!mp) {
            _new.controls = (name == "p1") ? Player.ControlMethod.PLAYER1 : Player.ControlMethod.PLAYER2; 
        } else {
            _new.controls = (id == GetTree().GetNetworkUniqueId()) ? 
                    Player.ControlMethod.PLAYER1 : Player.ControlMethod.REMOTE;
        }
        _new.DIRECTION = (name == "p1" ? Player.Direction.RIGHT : Player.Direction.LEFT);
        _new.Position = this.game.GetPlayerPosition(_new.DIRECTION);
        this.game.AddChild(_new);
        (this.game.GetNode("camera_track") as CameraTrack).Track(_new);
        
        if (name == "p1") this.p1 = _new;
        if (name == "p2") this.p2 = _new;

        if (this.p1 != null && this.p2 != null) {
            this.StartGame();
        }
    }

    void RemovePlayer(Player p) {
        //End the game for both players
        this.p1.End();
        this.p2.End();

        this.game.RemoveChild(p);
        (this.game.GetNode("camera_track") as CameraTrack).StopTrack(p);
        p.Dispose();
        p = null;
    }

    void CreateGame() {
        //Load game
        this.game = ((ResourceLoader.Load("res://scenes/level.tscn") as PackedScene).Instance()) as Level;
        //game.Connect("game_finished", this, nameof(_GameOver));
        //if (deferred) {
            //GetParent().CallDeferred(nameof(AddChild), game);
        //}// else {
        GetParent().AddChild(game);
        //}
        this.Visible = false;
        GD.Print("Game world created!");
    }
}
