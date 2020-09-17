using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiorgaFight {

public class CharSelection : Control
{
    //Stores all the data for a specific player
    public class PlayerData {
        //This data holders current selection
        public int selection;        
        //The other players data holder
        public PlayerData other;


        //NODESs
        //The button to select this data holder
        public TextureButton nodeButton;
        //This players button icon
        public Sprite nodeIcon;
        //This players confirmed sprite (used in mp)
        public Sprite nodeConfirmed;
        //The tick or cross to show if this player is connected
        public Sprite nodePresent;

        //MP status variables
        public bool mpConnected, mpConfirmed;

        public PlayerData() {
            this.selection = -1;

            //Start off with these at false
            this.mpConnected = false;
            this.mpConfirmed = false;
        }
    }

    //Character scenes in packed format
    //These are then instanced into the charScenes array 
    [Export] private List<PackedScene> charScenes; 
    
    //Texture used for other players in mp
    [Export] private Texture iconUnknown;

    [Export] private Texture iconPlayerPresent;
    [Export] private Texture iconPlayerNotPresent;

    private CharSelectionDataPanel[] chars;

    //Function to be called once the characters are selected
    private Func<CharSelection, PackedScene, PackedScene, LevelSelection> callback;

    private TextureButton nodePlayButton, nodeQuitButton, nodeRoleButton;
    private ItemList nodeCharList;
    private CharSelectionDataPanel nodeDataPanel;
    private Label nodeSpectators, nodeHostingOn;
    private Sprite nodeSpectatorIcon;

    //Player datas
    public PlayerData p1, p2;

    public override void _Ready() {
        this.p1 = new PlayerData();
        this.p2 = new PlayerData();

        //Get nodes
        this.p1.nodeButton = GetNode<TextureButton>("pa_player_buttons/bt_p1");
        this.p1.nodeIcon = GetNode<Sprite>("pa_player_buttons/sp_p1");
        this.p1.nodeConfirmed = GetNode<Sprite>("pa_player_buttons/sp_ready_p1");
        this.p1.nodePresent = GetNode<Sprite>("pa_player_buttons/sp_present_p1");
        
        this.p2.nodeButton = GetNode<TextureButton>("pa_player_buttons/bt_p2");
        this.p2.nodeIcon = GetNode<Sprite>("pa_player_buttons/sp_p2");
        this.p2.nodeConfirmed = GetNode<Sprite>("pa_player_buttons/sp_ready_p2");
        this.p2.nodePresent = GetNode<Sprite>("pa_player_buttons/sp_present_p2");

        this.nodePlayButton = GetNode<TextureButton>("bt_play");
        this.nodeQuitButton = GetNode<TextureButton>("bt_quit");
        this.nodeSpectators = GetNode<Label>("la_mp_spectators");
        this.nodeSpectatorIcon = GetNode<Sprite>("sp_mp_spectators");
        this.nodeHostingOn = GetNode<Label>("la_mp_hosting_on");
        this.nodeRoleButton = GetNode<TextureButton>("bt_role");

        //This is removed the moment the scene is opened
        //However I left it in the scene as it works as a good visual guide as to where the everything is in engine
        //And also means that this.nodeDataPanel will not be left null
        this.nodeDataPanel = GetNode<CharSelectionDataPanel>("pa_char_data");

        this.p1.other = this.p2;
        this.p2.other = this.p1;
        
        this.nodeCharList = GetNode<ItemList>("il_selection");

        //Connect player buttons
        this.p1.nodeButton.Connect("pressed", this, nameof(this._OnPlayerPressed), 
                new Godot.Collections.Array(new byte[] {1}));
        this.p2.nodeButton.Connect("pressed", this, nameof(this._OnPlayerPressed), 
                new Godot.Collections.Array(new byte[] {2}));
        this.nodePlayButton.Connect("pressed", this, nameof(this._OnPlayPressed));
        this.nodeQuitButton.Connect("pressed", Command.lobby, nameof(Command.lobby.Reset));
        this.nodeRoleButton.Connect("pressed", this, nameof(this._OnRolePressed));

        this.nodeCharList.Connect("item_selected", this, nameof(this._OnCharSelected));

        //Set mp specific bits to false
        this.nodeSpectators.Visible = false;
        this.nodeSpectatorIcon.Visible = false;
        this.nodeHostingOn.Visible = false;
        this.nodeRoleButton.Visible = false;

        //Map PackedScenes in charScenes into Control nodes in chars
        this.chars = charScenes.Select(character => character.Instance() as CharSelectionDataPanel).ToArray();

        this.RpcConfig(nameof(this.Confirm), MultiplayerAPI.RPCMode.Remotesync);

        this.ShowChar(-1);
    }

    //When one of the player buttons is pressed
    void _OnPlayerPressed(byte pressed) {
        PlayerData d = (pressed == 1) ? this.p1 : this.p2;

        d.nodeButton.Pressed = true;
        d.other.nodeButton.Pressed = false;
        
        //Change the itemlist to the current selected item if there is one
        if (d.selection != -1) {this.nodeCharList.Select(d.selection);}
        //Otherwise remove selection
        else if (this.nodeCharList.GetSelectedItems().Length > 0) {
            this.nodeCharList.Unselect(this.nodeCharList.GetSelectedItems()[0]);
        }
        this.ShowChar(d.selection);
    }

    //When the play button is pressed
    void _OnPlayPressed() {
        //If online, RPC confirmed instead
        if (Lobby.role != Lobby.MultiplayerRole.OFFLINE) {
            Rpc(nameof(this.Confirm), new object[] {Lobby.role, this.GetSelectedPlayer().selection});
            //this.Confirm(this.mp, this.GetSelectedPlayer().selection);
            this.nodePlayButton.Disabled = true;
            //Disable char list
            for (int i = 0; i < this.nodeCharList.GetItemCount(); i ++) {this.nodeCharList.SetItemSelectable(i, false);}
            return;
        }

        if (this.callback == null) {
            GD.Print("Fatal Error: No callback set for Character Selection, unable to proceed");
            return;
        }

        this.callback(this, this.chars[this.p1.selection].character, this.chars[this.p2.selection].character);
    }

    void _OnCharSelected(int index) {
        //This is incorrectly called even if an item should not be selectable
        //So check here that the item is actually selectable
        if (!this.nodeCharList.IsItemSelectable(index)) return;

        this.ShowChar(index);
        //Don't change anything for spectators
        if (Lobby.role == Lobby.MultiplayerRole.SPECTATOR) return;

        PlayerData d = this.GetSelectedPlayer();

        d.selection = index;
        d.nodeIcon.Texture = this.nodeCharList.GetItemIcon(index);

        //Enable button if both players have selected, or mp (== P1 or P2, specs don't get this far in the function, as long as both players are in the game)
        if ((this.p1.selection != -1 && this.p2.selection != -1) || (this.p1.mpConnected && this.p2.mpConnected))
            this.nodePlayButton.Disabled = false;
    }

    //Requests from the server that his player be moved to spectate
    void _OnRolePressed() {
        //Make the spectate request to the server
        Command.lobby.RpcId(1, nameof(Command.lobby.ChangeRole), new object[] {GetTree().GetNetworkUniqueId()});
    }

    //Sets a player selection to be confirmed
    public void Confirm(Lobby.MultiplayerRole p, int selection) {
        if (p == Lobby.MultiplayerRole.P1) {
            this.p1.nodeConfirmed.Visible = true;
            this.p1.selection = selection; 
            this.p1.mpConfirmed = true;
        } else if (p == Lobby.MultiplayerRole.P2) {
            this.p2.nodeConfirmed.Visible = true;
            this.p2.selection = selection;
            this.p2.mpConfirmed = true;
        } else {} //Cry I guess?

        //Both are selected, start the game
        if (this.p1.mpConfirmed && this.p2.mpConfirmed) {
            this.callback(this, this.chars[this.p1.selection].character, this.chars[this.p2.selection].character);
        }
    }

    //Shows a character up on the panel on the right hand side
    //Index must be an index found in the itemlist, CharSprites and CharTrees or -1 for non
    private void ShowChar(int index) {
        //Remove the data panel if it exists
        if (this.HasNode("pa_char_data")) {
            this.RemoveChild(this.nodeDataPanel);
        }

        //if index is -1 then the data panel is supposed to just be removed (so do not add a new one)
        if (index != -1) { 
            //Set the pointer to the new data panel
            this.nodeDataPanel = this.chars[index];
            
            //Add the new data panel
            this.AddChild(this.nodeDataPanel);
            this.nodeDataPanel.Reset(this.p1.nodeButton.Pressed);
        }
    }

    //Sets the lobby up for different multiplayer scenarios
    //Ensure Lobby.role is correct before calling this, as is depends on it
    public void SetMp() {
        if (Lobby.role == Lobby.MultiplayerRole.SPECTATOR) {
            //Disable all buttons
            this.p1.nodeButton.Disabled = true;
            this.p2.nodeButton.Disabled = true;
            this.nodePlayButton.Disabled = true;
            this.nodePlayButton.Visible = false;
            this.p1.nodeIcon.Texture = this.iconUnknown;
            this.p1.nodeIcon.Visible = false;
            this.p2.nodeIcon.Texture = this.iconUnknown;
            this.p2.nodeIcon.Visible = false;
        } else if (Lobby.role == Lobby.MultiplayerRole.P1) {
            this.p1.nodeButton.Pressed = true;
            this.p2.nodeButton.Pressed = false;
            this.p2.nodeButton.Disabled = true;
            this.p2.nodeIcon.Texture = this.iconUnknown;
            this.p2.nodeIcon.Visible = false;
        } else if (Lobby.role == Lobby.MultiplayerRole.P2) {
            this.p1.nodeButton.Pressed = false;
            this.p2.nodeButton.Pressed = true;
            this.p1.nodeButton.Disabled = true;
            this.p1.nodeIcon.Texture = this.iconUnknown;
            this.p1.nodeIcon.Visible = false;
        }

        //Turn these off (as players updated should be called after set MP)
        this.p1.nodePresent.Texture = this.iconPlayerNotPresent;
        this.p2.nodePresent.Texture = this.iconPlayerNotPresent;
        
        this.p1.mpConnected = false;
        this.p2.mpConnected = false;
        
        this.p1.mpConfirmed = false;
        this.p2.mpConfirmed = false;

        //Set spectator numbers
        this.nodeSpectators.Visible = true;
        this.nodeSpectatorIcon.Visible = true;
        this.nodeRoleButton.Visible = true;
        this.SetSpectators(Command.lobby.CalcSpectators());

        //Show "Hosting on: " text if for the host
        if (Lobby.IsHost()) {
            this.nodeHostingOn.Visible = true;
            this.nodeHostingOn.Text = "Hosting on: " + Command.GetLocalIP();
        }
    }

    //Sets present buttons correctly
    //p1 & p2 should be whether the player is now connected
    public void PlayersUpdated(bool p1, bool p2) {
        this.p1.nodePresent.Texture = p1 ? this.iconPlayerPresent : this.iconPlayerNotPresent;
        this.p2.nodePresent.Texture = p2 ? this.iconPlayerPresent : this.iconPlayerNotPresent;

        this.p1.nodeIcon.Visible = p1;
        this.p2.nodeIcon.Visible = p2;

        this.p1.mpConnected = p1;
        this.p2.mpConnected = p2;

        //Hide spectate button if no longer available
        if (Lobby.role == Lobby.MultiplayerRole.SPECTATOR) {
            //Hides play button if both players are connected, shows it if not
            this.nodeRoleButton.Visible = !(p1 && p2);
        }
    }

    //Sets the current number of spectators
    public void SetSpectators(int number) {
        this.nodeSpectators.Text = number.ToString();
    }

    //Sets the call back lobby to the one provided
    public void SetCallback(Func<CharSelection, PackedScene, PackedScene, LevelSelection> c) {
        this.callback = c;
    }

    //Returns the currently selected player data
    private PlayerData GetSelectedPlayer() {
        return this.p1.nodeButton.Pressed ? this.p1 : this.p2;
    }
}}
