using Godot;
using System;
using System.Collections.Generic;

public class CameraTrack : Node2D
{
	//Stops the camera from moving, usefull for testing animations
	[Export] bool moving = true;

	private List<Node2D> tracking;

	public CameraTrack() {
		this.tracking = new List<Node2D>();
	}

	public override void _Ready()
	{
		if (! moving) this.CalculatePosition();
	}

	//Find a better way to do this
	public override void _PhysicsProcess(float delta) {
		if (moving) this.CalculatePosition();
		//GD.Print(this.Position);
	}

	//Recalculates this nodes position, based on the average position of the elements of tracking and its offset
	public void CalculatePosition() {
		if (this.tracking.Count > 0) {
			Vector2 newpos = new Vector2(this.Position);
			newpos.x = 0;

			foreach (Node2D node in this.tracking) {
				newpos.x += node.GlobalPosition.x;
			}

			newpos.x /= this.tracking.Count;
			this.Position = newpos;
		}
	}

	public void Track(NodePath path) {
		Trackable parent = GetNode(path) as Trackable;
		//Check that parent actually exists
		if (parent == null) {
			GD.Print("CameraTrack.Track(NodePath): NodePath passed did not point to Trackable");	
			return;
		}

		this.Track(parent);
	}

	public void Track(Trackable trackable) {
		//Ensure node exists
		Node2D node = trackable.GetTrackingNode();
		if (node == null) {
			GD.Print("CameraTrack.Track(Trackable): Trackable.GetTrackingNode() did not return a valid Node2D");
			return;
		}

		GD.Print("Adding tracking node");

		//Add node to track
		this.tracking.Add(node);
	}

	public void StopTrack(Trackable trackable) {
		//Ensure node exists
		Node2D node = trackable.GetTrackingNode();
		if (node == null) {
			GD.Print("CameraTrack.StopTracking(Trackable): Trackable.GetTrackingNode() did not return a valid Node2D");
			return;
		}

		if (! this.tracking.Remove(node)) {
			GD.Print("CameraTrack.StopTracking(Trackable): Unable to find Trackable.GetTrackingNode() in this.tracking");
		}
	}

	public interface Trackable {
	//Returns the node that the camera should track
		Node2D GetTrackingNode();
	}
}
