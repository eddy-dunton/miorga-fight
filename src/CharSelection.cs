using Godot;
using System;

public class CharSelection : Control
{

    //Stores all the data for a specific player
    protected class PlayerData {
        //This data holders current selection
        public int selection;
        //The button to select this data holder
        public Button button;
        //The other players data holder
        public PlayerData other;

        public string name;

        public PlayerData(string name, Button button) {
            this.selection = -1;
            this.name = name;
            this.button = button;
        }
    }

    private PlayerData p1, p2;

    //Lobby which will be called back to once the players have decided which classes they will play as
    private Lobby callback;

    private Button nodeP1Button, nodeP2Button, nodePlayButton;
    private ItemList nodeCharList;
    private Sprite nodeAbilityTreeSprite, nodePlayerSprite;

    public override void _Ready() {
        //Get nodes
        this.nodeP1Button = GetNode<Button>("pa_bottom/bt_p1");
        this.nodeP2Button = GetNode<Button>("pa_bottom/bt_p2");
        this.nodePlayButton = GetNode<Button>("pa_bottom/bt_play");

        this.p1 = new PlayerData("Player 1", this.nodeP1Button);
        this.p2 = new PlayerData("Player 2", this.nodeP2Button);
        this.p1.other = this.p2;
        this.p2.other = this.p1;
        
        this.nodeCharList = GetNode<ItemList>("pa_selection/il_selection");

        this.nodeAbilityTreeSprite = GetNode<Sprite>("pa_char/sp_ability_tree");
        this.nodePlayerSprite = GetNode<Sprite>("pa_char/sp_player");

        //Connect player buttons
        this.nodeP1Button.Connect("pressed", this, nameof(this._OnPlayerButtonPressed), new Godot.Collections.Array(new byte[] {1}));
        
        this.nodeP2Button.Connect("pressed", this, nameof(this._OnPlayerButtonPressed), new Godot.Collections.Array(new byte[] {2}));

        this.nodeCharList.Connect("item_selected", this, nameof(this._OnCharSelected));
    }

    void _OnPlayerButtonPressed(byte pressed) {
        PlayerData d = (pressed == 1) ? this.p1 : this.p2;

        d.button.Pressed = true;
        d.other.button.Pressed = false;
        
        //Change the itemlist to the current selected item if there is one
        if (d.selection != -1) {this.nodeCharList.Select(d.selection);}
        //Otherwise remove selection
        else if (this.nodeCharList.GetSelectedItems().Length > 0) {
            this.nodeCharList.Unselect(this.nodeCharList.GetSelectedItems()[0]);
        }
    }

    void _OnCharSelected(int index) {
        PlayerData d = this.nodeP1Button.Pressed ? this.p1 : this.p2;

        d.selection = index;
        d.button.Text = d.name + ": " + this.nodeCharList.GetItemText(index);

        if (this.p1.selection != -1 && this.p2.selection != -1) this.nodePlayButton.Disabled = false;
    }

    //Sets the call back lobby to the one provided
    public void SetCallback(Lobby l) {
        this.callback = l;
    }

    
}
