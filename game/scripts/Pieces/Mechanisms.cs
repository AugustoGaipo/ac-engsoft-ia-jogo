using Godot;
using Swapping.GameLogic;
using Swapping.Levels.Model;

namespace Swapping.Game.Pieces;

/// <summary>Anything a button can feel: its mass and whether it is the player.</summary>
public interface IHasWeight
{
    WeightOnButton Weight { get; }
}

/// <summary>
/// A floor plate (<see cref="ButtonDef"/>, position = center of its bottom face). It is solid ground, and a
/// sensor just above it adds up what rests on it; <see cref="ButtonState"/> decides if it is pressed.
/// </summary>
public partial class FloorButton : Node3D
{
    private static readonly Color Up = new(0.75f, 0.2f, 0.2f);
    private static readonly Color Down = new(0.25f, 0.75f, 0.3f);

    private Area3D _sensor = null!;
    private StandardMaterial3D _material = null!;

    public ButtonDef Def { get; private set; } = null!;

    public bool IsPressed { get; private set; }

    public static FloorButton Create(ButtonDef def)
    {
        var size = def.Size.ToGodot();
        var button = new FloorButton { Name = def.Id, Def = def, Position = def.Position.ToGodot() };
        button._material = new StandardMaterial3D { AlbedoColor = Up };

        var plate = new StaticBody3D { CollisionLayer = Layers.World, CollisionMask = 0, Position = new Vector3(0, size.Y / 2, 0) };
        plate.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size, Material = button._material } });
        plate.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        button.AddChild(plate);

        button._sensor = new Area3D
        {
            CollisionLayer = 0,
            CollisionMask = Layers.Player | Layers.Objects,
            Monitorable = false,
            Position = new Vector3(0, size.Y + 0.2f, 0),
        };
        button._sensor.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(size.X, 0.4f, size.Z) } });
        button.AddChild(button._sensor);
        return button;
    }

    /// <summary>Reads what is on the plate now (call once per physics step).</summary>
    public void Refresh()
    {
        var weights = _sensor.GetOverlappingBodies().OfType<IHasWeight>().Select(b => b.Weight);
        IsPressed = ButtonState.IsPressed(Def.RequiredMassKg, Def.Detects, weights);
        _material.AlbedoColor = IsPressed ? Down : Up;
    }
}

/// <summary>A door (<see cref="DoorDef"/>): solid while closed; open = no collision and hidden.</summary>
public partial class Door : StaticBody3D
{
    private CollisionShape3D _shape = null!;

    public DoorDef Def { get; private set; } = null!;

    public bool IsOpen { get; private set; }

    public static Door Create(DoorDef def)
    {
        var door = new Door { Name = def.Id, Def = def, Position = def.Box.Center(), CollisionLayer = Layers.World, CollisionMask = 0 };
        door.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = def.Box.Size(), Material = new StandardMaterial3D { AlbedoColor = new Color(0.3f, 0.3f, 0.35f) } },
        });
        door._shape = new CollisionShape3D { Shape = new BoxShape3D { Size = def.Box.Size() } };
        door.AddChild(door._shape);
        return door;
    }

    public void SetOpen(bool open)
    {
        if (open == IsOpen)
            return;
        IsOpen = open;
        _shape.SetDeferred(CollisionShape3D.PropertyName.Disabled, open);
        Visible = !open;
    }
}

/// <summary>
/// A barrier (<see cref="BarrierDef"/>). Blocking objects or the player is a solid body on the
/// <c>barrier-objects</c> / <c>barrier-player</c> layer, so only that kind collides with it. Blocking
/// variables is measured by the swap controller (Task 6) from <see cref="Def"/>.
/// </summary>
public partial class Barrier : StaticBody3D
{
    public BarrierDef Def { get; private set; } = null!;
    /// <summary>
    /// The "forbidden" look: thin diagonal stripes in a darker shade of the barrier's color, sliding from right to
    /// left forever. Back faces are culled, so only the side facing the viewer is drawn. The stripe size is in meters on every face, and the direction is "to the viewer's left"
    /// whichever side of the wall is seen.
    /// </summary>
    private static readonly Shader StripeShader = new()
    {
        Code = @"
shader_type spatial;
render_mode unshaded, cull_back, blend_mix, depth_draw_never;

uniform vec3 base_color = vec3(1.0, 0.4, 0.8);
uniform vec3 stripe_color = vec3(0.4, 0.16, 0.32);
uniform float base_alpha = 0.16;
uniform float stripe_alpha = 0.6;
uniform float period = 0.5;
uniform float speed = 0.5;

varying vec3 local_pos;
varying vec3 local_normal;

void vertex() {
    local_pos = VERTEX;
    local_normal = NORMAL;
}

void fragment() {
    vec3 n = local_normal;
    vec3 a = abs(n);
    float u; // to the viewer's right
    float v; // up
    if (a.y > a.x && a.y > a.z) {
        u = local_pos.x; v = local_pos.z;
    } else if (a.x > a.z) {
        u = (n.x > 0.0 ? -1.0 : 1.0) * local_pos.z; v = local_pos.y;
    } else {
        u = (n.z > 0.0 ? 1.0 : -1.0) * local_pos.x; v = local_pos.y;
    }
    float band = fract((u - v) / period + speed * TIME);
    float stripe = step(0.65, band); // thin stripes: 35% of the period
    ALBEDO = mix(base_color, stripe_color, stripe);
    ALPHA = mix(base_alpha, stripe_alpha, stripe);
}
",
    };


    public static Barrier Create(BarrierDef def)
    {
        var filter = def.Filter;
        var barrier = new Barrier
        {
            Name = def.Id,
            Def = def,
            Position = def.Box.Center(),
            CollisionLayer = (filter.BlocksObjects ? Layers.BarrierObjects : 0) | (filter.BlocksPlayer ? Layers.BarrierPlayer : 0),
            CollisionMask = 0,
        };
        var (r, g, b) = BarrierPalette.For(def.Filter);
        var color = new Color((float)r, (float)g, (float)b, BaseAlpha);
        if (def.IsExitGrid)
            ExitPortal.Decorate(barrier, def.Box.Size());   // frosted pane with a frame (user, 2026-11-05)
        else
            barrier.AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh
                {
                    Size = def.Box.Size(),
                    Material = StripeMaterial(color),
                },
            });
        barrier.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = def.Box.Size() } });
        if (def.IsExitGrid)
        {
            // Draft of the crossed icons of the ideas bank: no boxes, no swaps through here.
            barrier.AddChild(new Label3D
            {
                Text = "sem caixas · sem trocas",
                FontSize = 48,
                PixelSize = 0.004f,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                Position = new Vector3(0, def.Box.Size().Y / 2 + 0.3f, 0),
            });
        }
        return barrier;
    }

    /// <summary>Stripes are the barrier's color made darker (not black), a bit more opaque than the base.</summary>
    public static ShaderMaterial StripeMaterial(Color color)
    {
        var material = new ShaderMaterial { Shader = StripeShader };
        material.SetShaderParameter("base_color", new Vector3(color.R, color.G, color.B));
        material.SetShaderParameter("stripe_color", new Vector3(color.R, color.G, color.B) * StripeDarkness);
        material.SetShaderParameter("base_alpha", color.A);
        material.SetShaderParameter("stripe_alpha", StripeAlpha);
        return material;
    }

    /// <summary>Stripe color = base color × this (0 would be black).</summary>
    public const float StripeDarkness = 0.35f;

    /// <summary>Opacity of the see-through wall and of its stripes.</summary>
    public const float BaseAlpha = 0.16f;

    public const float StripeAlpha = 0.6f;
}

/// <summary>The exit of a room: the player crossing it starts the next room (Task 8).</summary>
public partial class Exit : Area3D
{
    public Box Box { get; private set; }

    public static Exit Create(string roomId, Box box)
    {
        var exit = new Exit { Name = $"exit-{roomId}", Box = box, Position = box.Center(), CollisionLayer = 0, CollisionMask = Layers.Player, Monitorable = false };
        exit.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = box.Size() } });
        return exit;
    }
}
