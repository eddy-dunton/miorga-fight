using Godot;

//Used to control things regardless of scene
//all input events for command should start "com_"
public class Command : Node {
    //Used for debug
    public static Player p1, p2;

    public static InputEvent CreateInputEventAction(string action, bool pressed) {
        InputEventAction newEvent = new InputEventAction();
        newEvent.Action = action;
        newEvent.Pressed = pressed;
        return newEvent;
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

    //List of all joystick mappings
    private JoystickMapping[] joystickMappings;

    public Command() {
        this.joystickMappings = new JoystickMapping[] {
            new JoystickMapping("ctrlr_down", "p1_down"),
            new JoystickMapping("ctrlr_up", "p1_up"),
            new JoystickMapping("ctrlr_left", "p1_left"),
            new JoystickMapping("ctrlr_right", "p1_right")
        };
    }

    public override void _Ready() {
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

        else if (inputEvent.IsActionPressed("com_fs")) { //Swaps to and from fullscreen
            OS.WindowFullscreen = !OS.WindowFullscreen;
            GetTree().SetInputAsHandled();
        }
    }

    public override void _Process(float delta) {
        //Iterates through joystick mappings
        foreach (JoystickMapping jsm in this.joystickMappings) {
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
}