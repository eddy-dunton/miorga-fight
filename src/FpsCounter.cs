using Godot;
using System;

public class FpsCounter : Label {
  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(float delta)  {
      this.Text = "FPS: " + Engine.GetFramesPerSecond().ToString();
  }
}
