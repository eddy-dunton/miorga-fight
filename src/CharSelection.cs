using Godot;
using System;
using System.Collections;

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

    [Export] private System.Collections.Generic.List<PackedScene> chars; 
    //List of all Player sprites
    //The indexes of these must line up with the player indexes in il_selection
    [Export] private System.Collections.Generic.List<Texture> charSprites;
    //List of all player ability trees
    [Export] private System.Collections.Generic.List<Texture> charTrees;
 
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
        this.nodeP1Button.Connect("pressed", this, nameof(this._OnPlayerPressed), 
                new Godot.Collections.Array(new byte[] {1}));
        this.nodeP2Button.Connect("pressed", this, nameof(this._OnPlayerPressed), 
                new Godot.Collections.Array(new byte[] {2}));
        this.nodePlayButton.Connect("pressed", this, nameof(this._OnPlayPressed));


        this.nodeCharList.Connect("item_selected", this, nameof(this._OnCharSelected));

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
        if (this.callback == null) {
            GD.Print("Fatal Error: No callback set for Character Selection, unable to proceed");
            return;
        }

        this.callback.GameGoto();

        this.callback.AddPlayer("p1", 0, this.chars[this.p1.selection]);
        this.callback.AddPlayer("p2", 0, this.chars[this.p2.selection]);
        GetTree().Root.RemoveChild(this);
        this.callback.GameStart();
    }

    void _OnCharSelected(int index) {
        PlayerData d = this.nodeP1Button.Pressed ? this.p1 : this.p2;

        d.selection = index;
        d.button.Text = d.name + ": " + this.nodeCharList.GetItemText(index);

        if (this.p1.selection != -1 && this.p2.selection != -1) this.nodePlayButton.Disabled = false;
        this.ShowChar(index);
    }

    //Shows a character up on the panel on the right hand side
    //Index must be an index found in the itemlist, CharSprites and CharTrees or -1 for non
    private void ShowChar(int index) {
        if (index == -1) {
            this.nodePlayerSprite.Texture = null;
            this.nodeAbilityTreeSprite.Texture = null;
        } else {
            this.nodePlayerSprite.Texture = this.charSprites[index];
            this.nodeAbilityTreeSprite.Texture = this.charTrees[index];
        }
    }

    //Sets the call back lobby to the one provided
    public void SetCallback(Lobby l) {
        this.callback = l;
    }
}
