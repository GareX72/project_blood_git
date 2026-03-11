using Godot;
using System;
using System.Text.RegularExpressions;
public partial class Player1 : CharacterBody3D
{
	// UPDATE NOTE : add bazooka
	//bug count : 0 yay
	[Export] public float cam_sens = 6f;// dibagi 1000

	[ExportCategory("jump physics")]
	[Export] public float jump_height = 2f;
	[Export] public float jump_time_to_peak = 0.5f;
	[Export] public bool auto_bhop = true;
	private float jump_velocity;

	[ExportCategory("dash")]
	[Export] public float dash_cd = 1f;
	private float dash_timer = 0;
	[Export] public float dash_dur = 0.1f;
	private float dash_dur_timer = 0;
	private bool is_dashing = false;

	[ExportCategory("speed")]
	[Export] public float base_speed = 7f;
	[Export] public float run_speed = 8.5f;
	[Export] public float air_move_speed = 5f;
	[Export] public float air_speed = 500f;
	[Export] public float dash_speed = 100f;

	[ExportCategory("air physics")]
	[Export] public float air_cap = 0.05f;
	[Export] public float air_acc = 800f;
	[Export] public float air_dcc = 28f;
	[Export] public int air_jump_allowed = 1;

	[ExportCategory("ground physics")]
	[Export] public float ground_acc = 14f;
	[Export] public float ground_dcc = 10f;
	[Export] public float ground_friction = 6f;

	[ExportCategory("wall run")]
	[Export] public float max_tilt_deg = 45f;

	[ExportCategory("gravity")]
	[Export] public float fall_gravity = 10f;
	private float jump_gravity;
	private float jump_to_fall;
	[Export] float jump_to_fall_time = 5f;

	//other
	public Vector3 wish_dir = new Vector3(0,0,0);
	public const float bobbing_value = 0.06f;
	public const float bobbing_speed = 2.4f;
	public float bobbing_time = 0f;

	//reference
	private Node3D camPivot;
	private Camera3D cam;
	private RayCast3D rayR;
	private RayCast3D rayL;
	private Control speedline;
	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		camPivot = GetNode<Node3D>("%camPivot");
		cam = GetNode<Camera3D>("%cam");
		rayR = GetNode<RayCast3D>("rayR");
		rayL = GetNode<RayCast3D>("rayL");
		speedline = GetNode<Control>("%speedline");

		cam_sens /= 1000f;// 0.00x
		jump_gravity = 2f * jump_height / (jump_time_to_peak * jump_time_to_peak);
		jump_velocity = jump_gravity * jump_time_to_peak;
		jump_to_fall = jump_gravity;
	}

    public override void _Input(InputEvent @event)
	{
	}
    public override void _UnhandledInput(InputEvent @event)
    {
		if (@event.IsActionPressed("escape"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		if (Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			if (@event is InputEventMouseMotion motion)
			{
				RotateY(-motion.Relative.X * cam_sens);
				cam.RotateX(-motion.Relative.Y * cam_sens);
				cam.Rotation = new Vector3(Mathf.Clamp(cam.Rotation.X, Mathf.DegToRad(-90), Mathf.DegToRad(90)),cam.Rotation.Y,cam.Rotation.Z);
			}
		}
		//
    }
	public void bobbing(double delta)
	{
		bobbing_time += (float)delta * Velocity.Length();
		Transform3D origin = cam.Transform;
		origin.Origin = new Vector3(Mathf.Cos(bobbing_time * bobbing_speed * 0.5f) * bobbing_value,Mathf.Sin(bobbing_time * bobbing_speed) * bobbing_value,0);
		cam.Transform = origin;
	}
	public float get_speed()
	{
		if (Input.IsActionPressed("run")){return run_speed;}else{return base_speed;}
	}
	public void calc_movement(float delta, float acc, float speed)
	{
		float cur_speed_wish_dir = Velocity.Dot(wish_dir);
		float add_speed_to_cap = get_speed() - cur_speed_wish_dir;
		if (add_speed_to_cap > 0)
		{
			float acc_speed = acc * delta * speed;
			acc_speed = Mathf.Min(acc_speed, add_speed_to_cap);
			Velocity += acc_speed * wish_dir;
		}
	}
	public void friction(float delta)
	{
		float control = Mathf.Max(Velocity.Length(), ground_dcc);
		float drop = control * ground_friction * (float)delta;
		float new_speed = Mathf.Max(Velocity.Length() - drop, 0f);
		if (Velocity.Length() > 0)
		{
			new_speed /= Velocity.Length();
		}
		Velocity *= new_speed;
	}
	public void ground_physics(double delta)
	{
		calc_movement((float)delta, ground_acc, get_speed());

		//frictionnn
		friction((float)delta);

		bobbing(delta);
	}
	public void air_physics(double delta, float input_dir_y)
	{
		//gravity
		Vector3 velo = Velocity;
		if (Velocity.Y >= 0)// ganti gravitasi saat titik puncak
		{
			velo.Y -= jump_gravity * (float)delta;
		}
		else
		{
			jump_to_fall = (float)Mathf.MoveToward(jump_to_fall, fall_gravity, jump_to_fall_time);//transisi gravitasi
			velo.Y -= jump_to_fall * (float)delta;
		}
		Velocity = velo;

		//air movement
		calc_movement((float)delta, 28f, air_move_speed);

		if (input_dir_y > 0)
		{
			float cur_speed_wish_dir2 = Velocity.Dot(wish_dir);
			float speed_cap = Mathf.Min((air_speed * wish_dir).Length(), air_cap);
			float add_speed_to_cap2 = speed_cap - cur_speed_wish_dir2;
			if (add_speed_to_cap2 > 0)
			{
				float acc_speed = air_acc * air_speed * (float)delta;
				acc_speed = Mathf.Min(acc_speed, add_speed_to_cap2);
				Velocity += acc_speed * wish_dir;
			}
		}
	}
	public void jump(int dir)
	{
		Velocity = new Vector3(Velocity.X, jump_velocity * dir, Velocity.Z);
		jump_to_fall = jump_gravity;
	}
	public Vector3 get_cam_basis()
	{
		float rot_x = Rotation.Y;
		float rot_y = cam.Rotation.X;

		return new Vector3(-Mathf.Cos(rot_y) * Mathf.Sin(rot_x), Mathf.Sin(rot_y), -Mathf.Cos(rot_y) * Mathf.Cos(rot_x));
	}
	public void get_hit(float pos_Y)
	{
		Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
		if (GlobalPosition.Y >= pos_Y)
		{
			jump(1);
			return;
		}
		jump(-1);
	}
	public override void _PhysicsProcess(double delta)
    {
        Vector2 input_dir = Input.GetVector("right", "left", "down", "up").Normalized();
		wish_dir = GlobalTransform.Basis * new Vector3(-input_dir.X,0,-input_dir.Y);

		//dash
		if (Input.IsActionJustPressed("dash") && dash_timer >= dash_cd)
		{
			is_dashing = true;
			dash_dur_timer = 0;
			dash_timer = 0;
			speedline.Visible=true;
		}
		if (is_dashing && dash_dur_timer <= dash_dur)
		{
			dash_dur_timer += (float)delta;
			Velocity = dash_speed * wish_dir;
		}
		else if (dash_timer == 0)
		{
			is_dashing=false;
			Velocity = get_speed() * wish_dir;
			speedline.Visible=false;
		}
		if (dash_timer < dash_cd && !is_dashing)
		{
			dash_timer += (float)delta;
		}

		//movement
		if (IsOnFloor())
		{
			air_jump_allowed=1;
			if (Input.IsActionJustPressed("jump") || (auto_bhop && Input.IsActionPressed("jump")))
			{
				jump(1);
			}
			ground_physics(delta);
		}
		else
		{
			if (IsOnWallOnly() && (rayR.IsColliding() || rayL.IsColliding()) && input_dir != Vector2.Zero)
			{
				Vector3 velo = Velocity;
				Vector3 wall_normal = GetLastSlideCollision().GetNormal();
				int ray_dir =  rayR.IsColliding() ? 1 : -1;

				dash_dur_timer = dash_dur + 1;
				input_dir = new Vector2(0, 1);
				wish_dir = GlobalTransform.Basis * new Vector3(-input_dir.X,0,-input_dir.Y);
				velo.Y = 0;
				if (Input.IsActionJustPressed("jump"))
				{
					velo.Y = jump_velocity/2;
					velo += wall_normal * get_speed();
					jump_to_fall = jump_gravity;
				}
				Velocity = velo;
				camPivot.RotationDegrees = new Vector3(camPivot.RotationDegrees.X, camPivot.RotationDegrees.Y, Mathf.MoveToward(camPivot.RotationDegrees.Z, max_tilt_deg * ray_dir, (float)delta * 90));
				ground_physics(delta);
				MoveAndSlide();
				return;
			}
			if (Input.IsActionJustPressed("slam"))
			{
				Velocity = new Vector3(0, -jump_velocity*2, 0);
				air_jump_allowed=0;
				MoveAndSlide();
				return;
			}
			if (Input.IsActionJustPressed("jump") && air_jump_allowed != 0)
			{
				jump(1);
				air_jump_allowed--;
			}
			air_physics(delta, input_dir.Y);
		}
		if (camPivot.RotationDegrees.Z != 0)
		{
			Vector3 rot_deg = camPivot.RotationDegrees;
			camPivot.RotationDegrees = new Vector3(rot_deg.X, rot_deg.Y, Mathf.MoveToward(rot_deg.Z, 0, (float)delta * 90));
		}
		MoveAndSlide();
    }
}