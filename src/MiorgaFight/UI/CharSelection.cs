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
        //The button to select this data holder
        public TextureButton button;
        //The other players data holder
        public PlayerData other;
        //This players button icon
        public Sprite icon;

        public string name;

        public PlayerData(string name, TextureButton button, Sprite icon) {
            this.selection = -1;
            this.name = name;
            this.button = button;
            this.icon = icon;
        }
    }

    private Lobby.MultiplayerRole mpRole;

    //MP status variables
    public bool mpP1Connected, mpP2Connected, mpP1Confirmed, mpP2Confirmed;

    public PlayerData p1, p2;

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

    private TextureButton nodeP1Button, nodeP2Button, nodePlayButton;
    private Sprite nodeP1Icon, nodeP2Icon, nodeP1Confirmed, nodeP2Confirmed, nodeP1Present, nodeP2Present;
    private ItemList nodeCharList;
    private CharSelectionDataPanel nodeDataPanel;
    private Label nodeSpectators;

    public override void _Ready() {
        //Set mp values to false
        this.mpP1Connected = false;
        this.mpP2Connected = false;
        
        this.mpP1Confirmed = false;
        this.mpP2Confirmed = false;

        //Default to offline
        this.mpRole = Lobby.MultiplayerRole.OFFLINE;

        //Get nodes
        this.nodeP1Button = GetNode<TextureButton>("pa_player_buttons/bt_p1");
        this.nodeP1Icon = GetNode<Sprite>("pa_player_buttons/sp_p1");
        this.nodeP1Confirmed = GetNode<Sprite>("pa_player_buttons/sp_ready_p1");
        this.nodeP1Present = GetNode<Sprite>("pa_player_buttons/sp_present_p1");
        
        this.nodeP2Button = GetNode<TextureButton>("pa_player_buttons/bt_p2");
        this.nodeP2Icon = GetNode<Sprite>("pa_player_buttons/sp_p2");
        this.nodeP2Confirmed = GetNode<Sprite>("pa_player_buttons/sp_ready_p2");
        this.nodeP2Present = GetNode<Sprite>("pa_player_buttons/sp_present_p2");

        this.nodePlayButton = GetNode<TextureButton>("bt_play");
        this.nodeSpectators = GetNode<Label>("la_mp_spectators");

        //This is removed the moment the scene is opened
        //However I left it in the scene as it works as a good visual guide as to where the everything is in engine
        //And also means that this.nodeDataPanel will not be left null
        this.nodeDataPanel = GetNode<CharSelectionDataPanel>("pa_char_data");

        this.p1 = new PlayerData("Player 1", this.nodeP1Button, this.nodeP1Icon);
        this.p2 = new PlayerData("Player 2", this.nodeP2Button, this.nodeP2Icon);
        this.p1.other = this.p2;
        this.p2.other = this.p1;
        
        this.nodeCharList = GetNode<ItemList>("il_selection");

        //Connect player buttons
        this.nodeP1Button.Connect("pressed", this, nameof(this._OnPlayerPressed), 
                new Godot.Collections.Array(new byte[] {1}));
        this.nodeP2Button.Connect("pressed", this, nameof(this._OnPlayerPressed), 
                new Godot.Collections.Array(new byte[] {2}));
        this.nodePlayButton.Connect("pressed", this, nameof(this._OnPlayPressed));
    
        this.nodeCharList.Connect("item_selected", this, nameof(this._OnCharSelected));

        //Map PackedScenes in charScenes into Control nodes in chars
        this.chars = charScenes.Select(character => character.Instance() as CharSelectionDataPanel).ToArray();

        this.RpcConfig(nameof(this.Confirm), MultiplayerAPI.RPCMode.Remotesync);

        this.ShowChar(-1);
    }

    //When one of the player buttons is pressed
    void _OnPlayerPressed(byte pressed) {
        PlayerData d = (pressed == 1) ? this.p1 : this.p2;

        d.button.Pressed = true;
        d.other.button.Pressed = false;
        
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
        if (this.mpRole != Lobby.MultiplayerRole.OFFLINE) {
            Rpc(nameof(this.Confirm), new object[] {this.mpRole, this.GetSelectedPlayer().selection});
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
        //Don't change anything for spectators or host
        if (this.mpRole == Lobby.MultiplayerRole.SPECTATOR || this.mpRole == Lobby.MultiplayerRole.HOST) return;

        PlayerData d = this.GetSelectedPlayer();

        d.selection = index;
        d.icon.Texture = this.nodeCharList.GetItemIcon(index);

        //Enable button if both players have selected, or mp (== P1 or P2, specs don't get this far in the function, as long as both players are in the game)
        if ((this.p1.selection != -1 && this.p2.selection != -1) || (this.mpP1Connected && this.mpP2Connected))
            this.nodePlayButton.Disabled = false;
    }

    //Sets a player selection to be confirmed
    public void Confirm(Lobby.MultiplayerRole p, int selection) {
        if (p == Lobby.MultiplayerRole.P1) {
            this.nodeP1Confirmed.Visible = true;
            this.p1.selection = selection; 
            this.mpP1Confirmed = true;
        } else if (p == Lobby.MultiplayerRole.P2) {
            this.nodeP2Confirmed.Visible = true;
            this.p2.selection = selection;
            this.mpP2Confirmed = true;
        } else {} //Cry I guess?

        //Both are selected, start the game
        if (this.mpP1Confirmed && this.mpP2Confirmed) {
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

        //if index is -1 then the data panel is supposed to just be removed
        if (index != -1) { 
            //Set the pointer to the new data panel
            this.nodeDataPanel = this.chars[index];
            //Add the new data panel
            this.AddChild(this.nodeDataPanel);
            this.nodeDataPanel.Reset();
        }
    }

    //Sets the lobby up for different multiplayer scenarios
    public void SetMp(Lobby.MultiplayerRole role) {
        this.mpRole = role;
        if (this.mpRole == Lobby.MultiplayerRole.SPECTATOR ||this.mpRole == Lobby.MultiplayerRole.HOST) {
            //Disable all buttons
            this.nodeP1Button.Disabled = true;
            this.nodeP2Button.Disabled = true;
            this.nodePlayButton.Disabled = true;
            this.nodePlayButton.Visible = false;
            this.nodeP1Icon.Texture = this.iconUnknown;
            this.nodeP1Icon.Visible = false;
            this.nodeP2Icon.Texture = this.iconUnknown;
            this.nodeP2Icon.Visible = false;
        } else if (this.mpRole == Lobby.MultiplayerRole.P1) {
            this.nodeP1Button.Pressed = true;
            this.nodeP2Button.Pressed = false;
            this.nodeP2Button.Disabled = true;
            this.nodeP2Icon.Texture = this.iconUnknown;
            this.nodeP2Icon.Visible = false;
        } else if (this.mpRole == Lobby.MultiplayerRole.P2) {
            this.nodeP1Button.Pressed = false;
            this.nodeP2Button.Pressed = true;
            this.nodeP1Button.Disabled = true;
            this.nodeP1Icon.Texture = this.iconUnknown;
            this.nodeP1Icon.Visible = false;
        }

        //Turn these off (as players updated should be called after set MP)
        this.nodeP1Present.Texture = this.iconPlayerNotPresent;
        this.nodeP2Present.Texture = this.iconPlayerNotPresent;
        
        this.mpP1Connected = false;
        this.mpP2Connected = false;
        
        this.mpP1Confirmed = false;
        this.mpP2Confirmed = false;

        //Set spectator numbers
        this.nodeSpectators.Visible = true;
        this.SetSpectators(Command.lobby.CalcSpectators());
    }

    //Sets present buttons correctly
    //p1 & p2 should be whether the player is now connected
    public void PlayersUpdated(bool p1, bool p2) {
        this.nodeP1Present.Texture = p1 ? this.iconPlayerPresent : this.iconPlayerNotPresent;
        this.nodeP2Present.Texture = p2 ? this.iconPlayerPresent : this.iconPlayerNotPresent;

        this.nodeP1Icon.Visible = p1;
        this.nodeP2Icon.Visible = p2;

        this.mpP1Connected = p1;
        this.mpP2Connected = p2;
    }

    //Sets the current number of spectators
    public void SetSpectators(int number) {
        this.nodeSpectators.Text = "Spectators: " + number.ToString();
    }

    //Sets the call back lobby to the one provided
    public void SetCallback(Func<CharSelection, PackedScene, PackedScene, LevelSelection> c) {
        this.callback = c;
    }

    //Returns the currently selected player data
    private PlayerData GetSelectedPlayer() {
        return this.nodeP1Button.Pressed ? this.p1 : this.p2;
    }
}}
