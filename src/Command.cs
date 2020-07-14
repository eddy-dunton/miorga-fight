using Godot;

//Used to control things regardless of scene
//all input events for command should start "com_"
public class Command : Node {
    public override void _Input(InputEvent inputEvent) {
        if (inputEvent.IsActionPressed("com_fs")) { //Swaps to and from fullscreen
            OS.WindowFullscreen = !OS.WindowFullscreen;
            GetTree().SetInputAsHandled();
        }
    }
}