using Godot;
using System;
using System.Collections.Generic;

public class CameraTrack : Node2D
{
	//Array of names of all nodes to be tracked
	[Export] List<NodePath> trackNames;

	//Stops the camera from moving, usefull for testing animations
	[Export] bool moving;

	List<Node2D> tracking;

	public CameraTrack() {
		this.tracking = new List<Node2D>();
	}

	public override void _Ready()
	{
		this.PopulateTrackList();
		if (! moving) this.CalculatePosition();
	}

	//Find a better way to do this
	public override void _PhysicsProcess(float delta) {
		if (moving) this.CalculatePosition();
	}

	//Recalculates this nodes position, based on the average position of the elements of tracking and its offset
	public void CalculatePosition() {
		Vector2 newpos = new Vector2(this.Position);
		newpos.x = 0;

		foreach (Node2D node in this.tracking) {
			newpos.x += node.GlobalPosition.x;
		}

		newpos.x /= this.tracking.Count;
		this.Position = newpos;
	}

	private void PopulateTrackList() {
		Trackable parent;
		Node2D node;

		//Iterates through each 
		foreach (NodePath track in this.trackNames) {
			parent = GetNode(track) as Trackable;
			//Check that parent actually exists
			if (parent == null) continue;

			//Ensure node exists
			node = parent.GetTrackingNode();
			if (node == null) continue;

			//Add node to track
			tracking.Add(node);
		}
	}

	public interface Trackable {
	//Returns the node that the camera should track
		Node2D GetTrackingNode();
	}
}
