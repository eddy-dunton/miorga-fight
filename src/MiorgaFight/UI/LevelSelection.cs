using Godot;
using System;

namespace MiorgaFight {

public class LevelSelection : Control
{
    /*
        Data for these levels is stored in 2 different structures, the LevelSelectionLevelData resources stores all
        static data about each level, including names, descriptions and packed scenes etc.

        This structure contains all instance specific data for a level (this is actually unnecessary as there will only
        ever be a single instance of any of these resources and just a resource could be used, but that would be bad
        practice)
    */
    private struct LevelData {
        //Button for this level
        public TextureButton button;
        //This levels map
        public Level level;
        //Raw data for this level
        public LevelSelectionLevelData resource;
    }

    [Export] LevelSelectionLevelData[] levelData;

    //Textures to use for different players (to be swapped in mp)
    [Export] private Texture p1ButtonHover, p1ButtonPressed, p2ButtonHover, p2ButtonPressed;

    private Lobby.MultiplayerRole mpRole;

    //Map indexes selected in multiplayer
    private int mpP1Selection, mpP2Selection, mpSelection;

    private LevelData[] levels;

    private Control nodeButtonPanel;
    private TextureButton nodeButtonPlay;
    private AnimationPlayer nodeButtonAnim;
    private Sprite nodeLevelSprite;
    private Viewport nodeLevelViewport;
    private Camera2D nodeLevelCamera;
    private HSlider nodeLevelSlider;
    private RichTextLabel nodeLevelText;

    //Function to call once map selection is finished
    private Func<LevelSelection, PackedScene, int> callback;

    //The currently selected level
    private LevelData? level;

    public override void _Ready() {
        this.mpRole = Lobby.MultiplayerRole.OFFLINE;
        this.level = null;

        this.mpP1Selection = -1;
        this.mpP2Selection = -1;
        this.mpSelection = -1;

        //Gets nodes and sets up connections
        this.nodeButtonPanel = this.GetNode<Control>("pa_buttons");
        this.nodeButtonPlay = this.GetNode<TextureButton>("bt_play");
        //Animations are only used in mulitplayer
        this.nodeButtonAnim = this.GetNode<AnimationPlayer>("an_buttons");

        this.nodeButtonAnim.Connect("animation_finished", this, nameof(_OnAnimFinished));


        this.nodeLevelSprite = this.GetNode<Sprite>("sp_level");
        this.nodeLevelViewport = this.GetNode<Viewport>("vp_level");
        this.nodeLevelCamera = this.GetNode<Camera2D>("vp_level/camera");
        this.nodeLevelSlider = this.GetNode<HSlider>("sl_level");
        this.nodeLevelText = this.GetNode<RichTextLabel>("tx_level");

        this.nodeButtonPlay.Connect("pressed", this, nameof(_OnPlayPressed));
        this.nodeLevelSlider.Connect("value_changed", this, nameof(_OnSliderChanged));

        //Checks that number of buttons and levels are the same
        Godot.Collections.Array buttonPanelChildren = this.nodeButtonPanel.GetChildren();

        if (this.levelData.Length != buttonPanelChildren.Count) {
            GD.Print("ERROR: LEVEL SELECTION: Unequal number of buttons and maps, aborting");
            return;
        }

        //Init levels array
        this.levels = new LevelData[this.levelData.Length];

        //Temp looping values
        int i = 0;
        LevelData level;
        //Iterate through level data provided
        foreach (LevelSelectionLevelData data in this.levelData) {
            //Create new resource for level
            level = new LevelData();
            level.resource = data;

            //Instance level
            level.level = level.resource.packed.Instance() as Level;
            if (level.level == null) {
                GD.Print("Error: Processing level from pack");
                continue;
            }

            //Remove hud and camera track
            level.level.RemoveChild(level.level.GetNode("hud"));
            level.level.RemoveChild(level.level.GetNode("camera_track"));
        
            //Gets all children of the button panel
            level.button = buttonPanelChildren[i] as TextureButton;

            //Call _OnButtonPress with this buttons index on press
            level.button.Connect("pressed", this, nameof(this._OnButtonPressed), 
                    new Godot.Collections.Array(new int[] {i}));

            this.levels[i] = level;

            i ++;
            //Something has gone very wrong if there's more than 6 buttons
            if (i == 6) {
                GD.Print("ERROR: LEVEL SELECTION: More than 6 maps");
                break;
            }
        }

        //Sends only to server
        RpcConfig(nameof(this.MpSelected), MultiplayerAPI.RPCMode.Remotesync);
		//Only server can call this
		RpcConfig(nameof(this.MpChosen), MultiplayerAPI.RPCMode.Remotesync);
    }

    void _OnSliderChanged(float value) {
        this.nodeLevelCamera.Position = new Vector2(value, this.nodeLevelCamera.Position.y);
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
        this.ShowLevel(newlevel, this.level);
        this.level = newlevel;
    }

    //When the play button is pressed
    void _OnPlayPressed() {
        if (this.callback == null) {
            GD.Print("Fatal Error: No callback set for Character Selection, unable to proceed");
            return;
        }
    
        if (this.level.HasValue) {
            if (this.mpRole == Lobby.MultiplayerRole.OFFLINE) {
                //Fire the callback when the animation is finished
                this.callback(this, this.level.Value.resource.packed);
            } else { //Online
				//Disable play button
				this.nodeButtonPlay.Disabled = true;

				//Disable all the other level buttons
				foreach (LevelData l in this.levels) {
					//Skip the current one
					if (l.Equals(this.level.Value)) continue;
					l.button.TextureDisabled = null;
					l.button.Disabled = true;
				}

				//Send selection to server
                Rpc(nameof(this.MpSelected), new object[] {Lobby.role, Array.IndexOf(this.levels, this.level)});
			}
		}
    }

    //Performs this object's callback, necessary as delegate functions (which callbacks are) cannot be connected to singals
    //Only used in mp
    void _OnAnimFinished(String name) {
        if (this.callback != null) {
            this.callback(this, this.levels[this.mpSelection].resource.packed);
        }
    }

    //Called remoted by a player when they have picked a map
    public void MpSelected(Lobby.MultiplayerRole role, int index) {
        if (role == Lobby.MultiplayerRole.P1) this.mpP1Selection = index;
        else if (role == Lobby.MultiplayerRole.P2) this.mpP2Selection = index;
        //else {wtf?}

        //Both players have chosen, pass choices through to clients, if host 
        if (this.mpP1Selection != -1 && this.mpP2Selection != -1 && Lobby.IsHost()) {
            //Randomly generate a winner
            Lobby.MultiplayerRole win = Command.Random(0, 1) == 0 ? Lobby.MultiplayerRole.P1 : Lobby.MultiplayerRole.P2; 
            Rpc(nameof(MpChosen), new object[] {win, this.mpP1Selection, this.mpP2Selection});
        }
    }

    //Called by the host once both p1 and p2 have chosen maps, plays the correct animation
    public void MpChosen(Lobby.MultiplayerRole winner, int p1, int p2) {
        //Set mp selection correctly, if they chose the same it doesn't matter who wins
        this.mpSelection = winner == Lobby.MultiplayerRole.P1 ? p1 : p2;

		//Hide the confirm buttons
		this.nodeButtonPlay.Visible = false;

		//Make all the buttons fuck off
		foreach (LevelData l in this.levels) {
			l.button.TextureDisabled = null;
			l.button.Disabled = true;
		}

        //Same level chosen       
        if (p1 == p2) {
            this.nodeButtonAnim.GetAnimation("same_chosen").TrackSetPath(0, this.levels[p1].button.GetPath() 
                    + ":texture_disabled");
            this.nodeButtonAnim.Play("same_chosen");
		} else {
			//Setup buttons
			this.levels[p1].button.TextureDisabled = this.p1ButtonPressed;
			this.levels[p2].button.TextureDisabled = this.p2ButtonPressed;
			
			String anim = this.mpSelection == p1 ? "diff_chosen_p1" : "diff_chosen_p2";
		
			//Set tracks to point to the correct position
			this.nodeButtonAnim.GetAnimation(anim).TrackSetPath(0, this.levels[p1].button.GetPath() + ":visible");
			this.nodeButtonAnim.GetAnimation(anim).TrackSetPath(1, this.levels[p2].button.GetPath() + ":visible");
			this.nodeButtonAnim.Play(anim);
		} 
    }

    //Sets up Level selection for multiplayer 
    public void SetMp(Lobby.MultiplayerRole role) {
        this.mpRole = role;
        if (role == Lobby.MultiplayerRole.P2) {
            //Set buttons to P2 style
            foreach (LevelData l in this.levels) {
                l.button.TextureHover = this.p2ButtonHover;
                l.button.TexturePressed = this.p2ButtonPressed;
                l.button.TextureDisabled = this.p2ButtonPressed;
            }

        } else if (role != Lobby.MultiplayerRole.P1) { //All none players
            this.nodeButtonPlay.Visible = false;
        }
    }

    public void SetCallback(Func<LevelSelection, PackedScene, int> c) {
        this.callback = c;
    }

    private void ShowLevel(LevelData newlevel, LevelData? oldlevel) {
        if (oldlevel.HasValue) {
            this.nodeLevelViewport.RemoveChild(oldlevel.Value.level);
        }
        
        this.nodeLevelSlider.Editable = true;
        this.nodeLevelSlider.Value = 0f;
        this.nodeLevelSlider.MinValue = -newlevel.resource.GetMovement();
        this.nodeLevelSlider.MaxValue = newlevel.resource.GetMovement();
        this.nodeLevelCamera.Position = newlevel.resource.GetCameraPos();
        this.nodeLevelText.BbcodeText = newlevel.resource.text;

        this.nodeLevelViewport.AddChild(newlevel.level);
    }
}}
