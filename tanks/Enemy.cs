using Godot;
using System;

public class Enemy : Tank
{

    [Export]
    protected int TurretSpeed;

    [Export]
    protected float DetectRadius;

    protected Node2D target;

    private int speed;

    [Puppet]
    public bool remotePrimaryWeapon;

    [Puppet]
    public bool remoteSecondaryWeapon;

    [Puppet]
    Vector2 remotePosition = new Vector2();

    [Puppet]
    float remoteRotation = 0.0f;

    [Puppet]
    int remoteHealth = 0;

    [Puppet]
    float remoteOffset = 0;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        CollisionShape2D detectRadius = (CollisionShape2D)(GetNode("DetectRadius").GetNode("CollisionShape2D"));

        CircleShape2D shape = new CircleShape2D();
        shape.Radius = DetectRadius;
        detectRadius.Shape = shape;

        base._Ready();

        remotePrimaryWeapon = false;
        remoteSecondaryWeapon = false;
    }

    public override void _Control(float delta)
    {
        if (target != null)
        {
            Vector2 targetDir = (target.GlobalPosition - GlobalPosition).Normalized();
            //Sprite turret = (Sprite)GetNode("Turret");
            //Vector2 currentDir = (new Vector2(1, 0)).Rotated(turret.GlobalRotation);
            //turret.GlobalRotation = currentDir.LinearInterpolate(targetDir, TurretSpeed * delta).Angle();

            Vector2 currentDir = (new Vector2(1, 0)).Rotated(GlobalRotation);
            GlobalRotation = currentDir.LinearInterpolate(targetDir, TurretSpeed * delta).Angle();
            if (targetDir.Dot(currentDir) > 0.9)
            {
                remotePrimaryWeapon = true;
                remotePrimaryWeapon = false;
                _shoot(remotePrimaryWeapon, remotePrimaryWeapon);
            }
            else
            {
                remotePrimaryWeapon = false;
                remoteSecondaryWeapon = false;
            }
        }

        RayCast2D lookAhead1 = (RayCast2D)GetNode("LookAhead1");
        RayCast2D lookAhead2 = (RayCast2D)GetNode("LookAhead2");
        if (lookAhead1.IsColliding() || lookAhead2.IsColliding())
        {
            speed = (int)Mathf.Lerp(speed, 0.0f, 0.1f);
        }
        else
        {
            speed = (int)Mathf.Lerp(speed, MaxSpeed, 0.05f);
        }

        if (typeof(PathFollow2D).IsInstanceOfType(GetParent()))
        {
            PathFollow2D pathFollow2D = (PathFollow2D)GetParent();
            pathFollow2D.Offset = pathFollow2D.Offset + speed * delta;

            remoteOffset = pathFollow2D.Offset;
        }
    }

    public override void _Process(float delta)
    {
        if (!Alive)
        {
            return;
        }

        if (GetTree().IsNetworkServer())
        {
            _Control(delta);

            RsetUnreliable(nameof(remotePosition), GlobalPosition);
            RsetUnreliable(nameof(remoteRotation), GlobalRotation);
            RsetUnreliable(nameof(remoteHealth), getHealth());
            RsetUnreliable(nameof(remotePrimaryWeapon), remotePrimaryWeapon);
            RsetUnreliable(nameof(remoteSecondaryWeapon), remoteSecondaryWeapon);
            RsetUnreliable(nameof(remoteOffset), remoteOffset);
        }
        else
        {
            _shoot(remotePrimaryWeapon, remoteSecondaryWeapon);

            //Position = remotePosition;
            Rotation = remoteRotation;
            setHealth(remoteHealth);

            if (typeof(PathFollow2D).IsInstanceOfType(GetParent()))
            {
                PathFollow2D pathFollow2D = (PathFollow2D)GetParent();
                pathFollow2D.Offset = remoteOffset;
            }
        }
    }

    private void _on_DetectRadius_body_entered(Node2D body)
    {
        if (body.Name.Contains("client_"))
        {
            target = body;
        }
    }


    private void _on_DetectRadius_body_exited(Node2D body)
    {
        if (body == target)
        {
            target = null;
        }
    }
}