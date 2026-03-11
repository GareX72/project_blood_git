using Godot;
using System;

public partial class RpgBullet : Node3D
{
    [Export] public float time = 2f;
    private float timer = 0;
	[Export] public Vector3 dir;
    private bool is_hit = false;
    private GpuParticles3D explode;
    public override void _Ready()
    {
		SetPhysicsProcess(false);
        explode = GetNode<GpuParticles3D>("flash");
        //Rotation
    }
    public void _enter(Node3D body)
    {
        if (is_hit && body.HasMethod("get_hit"))
        {
            body.Call("get_hit", GlobalPosition.Y);
        }
        if (!body.IsInGroup("player")){is_hit =true;}
    }
    public void _end()
    {
        QueueFree();
    }
    public override void _Process(double delta)
    {
        if (is_hit)
        {
            explode.Emitting=true;
            explode.GetChild<GpuParticles3D>(0).Emitting=true;
            explode.GetChild<GpuParticles3D>(1).Emitting=false;
            GetNode<CollisionShape3D>("%coll").Shape.Set("radius", 2.5f);
            SetProcess(false);
            return;
        }
        GlobalPosition += dir * 35 * (float)delta;
        timer += (float)delta;
        if (timer >= time){is_hit=true;}
    }
}
