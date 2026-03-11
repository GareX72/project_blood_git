using Godot;
using System;

public partial class RpgManager : Node3D
{
	private PackedScene bullet = ResourceLoader.Load<PackedScene>("res://scene/rpg_bullet.tscn");
	private CharacterBody3D player;
    public override void _Ready()
    {
		player = GetNode<CharacterBody3D>("%player");
        SetProcess(false);
		SetPhysicsProcess(false);
    }
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("shoot"))
		{
			RpgBullet bullet_ins = bullet.Instantiate<RpgBullet>();
			GlobalPosition = player.GetNode<Node3D>("%rpg_pivot").GlobalPosition;
			AddChild(bullet_ins);
			//bullet_ins.GlobalPosition = player.GetNode<Node3D>("%rpg_pivot").GlobalPosition;
			bullet_ins.dir = (Vector3)player.Call("get_cam_basis");
		}
    }
}
