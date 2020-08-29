using Godot;
using System;

namespace MiorgaFight {

public class MobileControls : Control
{
    private Button nodeUp, nodeLeft, nodeRight, nodeDown, nodeMain, nodeAlt;

    public override void _Ready() {
        //Left panel
        this.nodeUp = this.SetupButton("pa_left/bt_up", "p1_up");
        this.nodeLeft = this.SetupButton("pa_left/bt_left", "p1_left");
        this.nodeRight = this.SetupButton("pa_left/bt_right", "p1_right");
        this.nodeDown = this.SetupButton("pa_left/bt_down", "p1_down");
 
        //Right panel
        this.nodeMain = this.SetupButton("pa_right/bt_main", "p1_action_main");
        this.nodeAlt = this.SetupButton("pa_right/bt_alt", "p1_action_alt");

        //Connect buttons up

        //If not on mobile, remove and delete
        if (!Command.IsMobile()) {
            this.NotMobile();
            return;
        }
    }

    //Gets button at path
    //Connects button to EmulateInput, passing input and true / false depending on whether the button goes up or down
    //Returns the button
    private Button SetupButton(string path, string input) {
        Button b = this.GetNode<Button>(path);
        b.Connect("button_down", this, nameof(EmulateInput), new Godot.Collections.Array(new object[] {input, true}));
        b.Connect("button_up", this, nameof(EmulateInput), new Godot.Collections.Array(new object[] {input, false}));
        return b;
    }

    private void EmulateInput(string input, bool pressed) {
        InputEventAction inp = new InputEventAction();
        inp.Action = input;
        inp.Pressed = pressed;
        Input.ParseInputEvent(inp);
    }

    //Called if not actually playing on a mobile device, removes from parent and queues destruction
    private void NotMobile() {
        this.Visible = false;
        Node parent = this.GetParent();
        parent.CallDeferred(nameof(parent.RemoveChild), new object[] {this});
        this.QueueFree();
    }
}}
