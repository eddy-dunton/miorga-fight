using Godot;
using System;

namespace MiorgaFight {

public class LevelSelection : Control
{
    private struct LevelData {
        public PackedScene packed;
        //Button for this level
        public TextureButton button;
        //This levels map
        public Level level;
        //the cameras y position
        public Vector2 cameraPos;
        //How much sideways movement the level allows
        public float movement;
    }

    [Export] private PackedScene[] packedMaps;
    [Export] private int[] mapWidth;

    private LevelData[] levels;

    private Control nodeButtonPanel;
    private TextureButton nodeButtonPlay;
    private Sprite nodeMapSprite;
    private Viewport nodeMapViewport;
    private Camera2D nodeMapCamera;
    private HSlider nodeMapSlider;

    //Function to call once map selection is finished
    private Func<LevelSelection, PackedScene, int> callback;

    //The currently selected level
    private LevelData? level;

    public override void _Ready() {
        this.level = null;

        this.nodeButtonPanel = this.GetNode<Control>("pa_buttons");
        this.nodeButtonPlay = this.GetNode<TextureButton>("bt_play");

        this.nodeMapSprite = this.GetNode<Sprite>("sp_map");
        this.nodeMapViewport = this.GetNode<Viewport>("vp_map");
        this.nodeMapCamera = this.GetNode<Camera2D>("vp_map/camera");
        this.nodeMapSlider = this.GetNode<HSlider>("sl_map");

        this.nodeButtonPlay.Connect("pressed", this, nameof(_OnPlayPressed));

        this.nodeMapSlider.Connect("value_changed", this, nameof(_OnSliderChanged));
    
        if (this.packedMaps.Length != this.nodeButtonPanel.GetChildren().Count) {
            GD.Print("ERROR: MAP SELECTION: Unequal number of buttons and maps, aborting");
            return;
        }

        this.levels = new LevelData[this.packedMaps.Length];

        //Temp looping values
        int i = 0;
        Node2D cameraTrack;
        LevelData data;
        foreach (Node child in this.nodeButtonPanel.GetChildren()) {
            data = new LevelData();
            
            data.packed = this.packedMaps[i];

            //Get level
            data.level = data.packed.Instance() as Level;
            if (data.level == null) {
                GD.Print("Error: Processing level from pack");
                continue;
            }

            cameraTrack = data.level.GetNode<Node2D>("camera_track");
            //Get camera position
            data.cameraPos = cameraTrack.Position;

            //Set up movement allowances
            data.movement = (this.mapWidth[i] - 780) / 2;

            //Remove hud and camera track
            data.level.RemoveChild(data.level.GetNode("hud"));
            data.level.RemoveChild(cameraTrack);
        
            //Gets all children of the button panel
            data.button = child as TextureButton;

            //Call _OnButtonPress with this buttons index on press
            data.button.Connect("pressed", this, nameof(this._OnButtonPressed), 
                    new Godot.Collections.Array(new int[] {i}));

            this.levels[i] = data;

            i ++;
            //Something has gone very wrong if there's more than 6 buttons
            if (i == 6) {
                GD.Print("ERROR: MAP SELECTION: More than 6 maps");
                break;
            }
        }
    }

    void _OnSliderChanged(float value) {
        this.nodeMapCamera.Position = new Vector2(value, this.nodeMapCamera.Position.y);
    }

    void _OnButtonPressed(int i) {
        LevelData newlevel = this.levels[i];
        //Reable last pressed button, if one was pressed
        if (this.level.HasValue) {
            this.level.Value.button.Disabled = false;
            this.level.Value.button.Pressed = false;
        }

        //Diable this button (so it can't be unselected)
        newlevel.button.Disabled = true;

        this.nodeButtonPlay.Disabled = false;
        this.ShowMap(newlevel, this.level);
        this.level = newlevel;
    }

    //When the play button is pressed
    void _OnPlayPressed() {
        if (this.callback == null) {
            GD.Print("Fatal Error: No callback set for Character Selection, unable to proceed");
            return;
        }
    
        if (this.level.HasValue) {
            this.callback(this, this.level.Value.packed);
        }
    }

    private void ShowMap(LevelData newlevel, LevelData? oldlevel) {
        if (oldlevel.HasValue) {
            this.nodeMapViewport.RemoveChild(oldlevel.Value.level);
        }
        
        this.nodeMapSlider.Value = 0f;
        this.nodeMapSlider.MinValue = -newlevel.movement;
        this.nodeMapSlider.MaxValue = newlevel.movement;
        this.nodeMapCamera.Position = newlevel.cameraPos;
        
        this.nodeMapViewport.AddChild(newlevel.level);
    }

    public void SetCallback(Func<LevelSelection, PackedScene, int> c) {
        this.callback = c;
    }
}}
