using Godot;
using System;
using System.Net;
using System.Linq;

namespace MiorgaFight {

//Used to control things regardless of scene
//all input events for command should start "com_"
public class Command : Node {
    //Reference to Lobby
    public static Lobby lobby;

    private static bool mobile = OS.GetName() == "Android"; 
    public static bool IsMobile() {return mobile;}

    private static Random random = new Random();

    //Returns a pseudo random double between min and max (inclusive)
    public static double Random(double min, double max) {
        return min + (random.NextDouble() / (1 / (max - min)));
    }

    //Returns a pseudo int double between min and max (inclusive)
    public static int Random(int min, int max) {
        return min + (random.Next() % (max - min + 1));    
    }

    //Returns s mapped from range(a1, a2) to range(b1 , b2)
    public static double Map(double a1, double a2, double b1, double b2, double s) => 
        b1 + (s - a1) * (b2 - b1) / (a2 - a1);

    public static InputEvent CreateInputEventAction(string action, bool pressed) {
        InputEventAction newEvent = new InputEventAction();
        newEvent.Action = action;
        newEvent.Pressed = pressed;
        return newEvent;
    }

    public static string GetLocalIP()
    {
        if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return null;

        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

        return host
            .AddressList
            .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .ToString();
    }

    //Class used to map joysticks to keyboard commands
    private class JoystickMapping {
        public string inputAction; 
        public string outputAction;
        public bool pressed;

        public JoystickMapping(string inputAction, string outputAction) {
            this.inputAction = inputAction;
            this.outputAction = outputAction;
            this.pressed = false;
        }
    }

    //Pause menu scene
    private static PauseMenu pauseMenu = 
            (ResourceLoader.Load("res://scenes/ui/pause.tscn") as PackedScene).Instance() as PauseMenu;

    //List of all joystick mappings
    private static JoystickMapping[] joystickMappings = {
        new JoystickMapping("ctrlr_down", "p1_down"),
        new JoystickMapping("ctrlr_up", "p1_up"),
        new JoystickMapping("ctrlr_left", "p1_left"),
        new JoystickMapping("ctrlr_right", "p1_right")
    };

    public Command() {}

    public override void _Ready() {
        //Continue through pauses
        this.PauseMode = Node.PauseModeEnum.Process;

        //Cycle through commands
        foreach (string arg in OS.GetCmdlineArgs()) {
            if (arg == "--fullscreen") {
                 OS.WindowFullscreen = true;
            }
        }
    }

    public override void _Input(InputEvent inputEvent) {
		if (inputEvent.IsActionPressed("com_debug")) {	
			if (true) {}; //Debug breakpoint
		}

        else if (inputEvent.IsActionPressed("com_pause")) { //Opens pause menu (and pauses in local play)
            //Check if already paused (GetTree().Pause is not checked, as this is not changed when the pause menu is opened in mp
            if (GetTree().Root.HasNode("pause")) {
                this.PauseEnd();
            } else if (GetTree().Root.HasNode("level")) { //Check that game is in a level 
                this.PauseStart();
            }
        }

        else if (inputEvent.IsActionPressed("com_fs")) { //Swaps to and from fullscreen
            OS.WindowFullscreen = !OS.WindowFullscreen;
            GetTree().SetInputAsHandled();
        }
    }

    public override void _Process(float delta) {
        //Iterates through joystick mappings
        foreach (JoystickMapping jsm in joystickMappings) {
            //Checks for mappings which should trigger a pressed event
            if (Input.IsActionPressed(jsm.inputAction) && !jsm.pressed) {
                jsm.pressed = true;
                Input.ParseInputEvent(CreateInputEventAction(jsm.outputAction, true));
            //Checks for mappings which should trigger a released event
            } else if (!Input.IsActionPressed(jsm.inputAction) && jsm.pressed) {
                jsm.pressed = false;
                Input.ParseInputEvent(CreateInputEventAction(jsm.outputAction, false));
            } 
        }
    }

    //Starts pausing the game
    private void PauseStart() {
        //Pause if game is local
        if (Lobby.role == Lobby.MultiplayerRole.OFFLINE) {
            GetTree().Paused = true;
        }

        Input.SetMouseMode(Input.MouseMode.Visible);
    
        GetTree().Root.AddChild(Command.pauseMenu);
    }

    public void PauseEnd() {
        GetTree().Paused = false;

        Input.SetMouseMode(Input.MouseMode.Hidden);

        GetTree().Root.RemoveChild(Command.pauseMenu);
    }
}}