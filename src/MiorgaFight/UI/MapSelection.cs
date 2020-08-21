using Godot;
using System;

namespace MiorgaFight {

public class MapSelection : Control
{
    [Export] private Texture[] mapTextures;
    [Export] private PackedScene[] maps;

    private TextureButton[] buttons;

    private Control nodeButtonPanel;
    private TextureButton nodeButtonPlay;
    private Sprite nodeMapSprite;

    //Function to call once map selection is finished
    private Func<MapSelection, PackedScene, int> callback;

    //The currently pressed button, or -1 if none are pressed
    private int pressed;

    public override void _Ready()
    {
        this.pressed = -1;

        this.buttons = new TextureButton[6];

        this.nodeButtonPanel = this.GetNode<Control>("pa_buttons");
        this.nodeButtonPlay = this.GetNode<TextureButton>("bt_play");
        this.nodeMapSprite = this.GetNode<Sprite>("sp_map");

        this.nodeButtonPlay.Connect("pressed", this, nameof(_OnPlayPressed));

        //Populates button array
        TextureButton button;
        int i = 0;
        //Gets all children of the button panel
        foreach (Node child in this.nodeButtonPanel.GetChildren()) {
            button = child as TextureButton;
            //Check that child is a button
            if (button == null) continue;

            //Button is valid, set up
            this.buttons[i] = button;
            //Call _OnButtonPress with this buttons index on press
            button.Connect("pressed", this, nameof(this._OnButtonPressed), new Godot.Collections.Array(new int[] {i}));

            i ++;
            //Something has gone very wrong if there's more than 6 buttons
            if (i == 6) {
                GD.Print("ERROR: MAP SELECTION: More than 6 maps");
                break;
            }
        }
    }

    void _OnButtonPressed(int index) {
        //Reable last pressed button
        if (this.pressed != -1) {
            this.buttons[this.pressed].Disabled = false;
            this.buttons[this.pressed].Pressed = false;
        }
        this.pressed = index;
        //Diable this button (so it can't be unselected)
        this.buttons[index].Disabled = true;

        this.nodeButtonPlay.Disabled = false;
        this.ShowMap(index);
    }

    //When the play button is pressed
    void _OnPlayPressed() {
        if (this.callback == null) {
            GD.Print("Fatal Error: No callback set for Character Selection, unable to proceed");
            return;
        }
    
        this.callback(this, this.maps[this.pressed]);
    }

    private void ShowMap(int index) {
        this.nodeMapSprite.Texture = this.mapTextures[index];
    }

    public void SetCallback(Func<MapSelection, PackedScene, int> c) {
        this.callback = c;
    }
}}
