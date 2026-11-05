using Godot;

namespace Swapping.Game.Pieces;

/// <summary>
/// The look of the exit grid (user's request, 2026-11-05): a frosted glass pane with soft vertical colour
/// streaks that drift slowly, a thin warm frame around the doorway and a pale light spilling on the floor.
/// Only the look: the grid still blocks objects and swaps and lets the player through (<see cref="Barrier"/>).
/// </summary>
public static class ExitPortal
{
    private static readonly Shader GlassShader = new()
    {
        Code = @"
shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never;

uniform vec2 size = vec2(2.0, 3.0);   // pane width and height (m)
uniform bool along_z = true;          // the pane spans z (else x)

varying vec3 local_pos;

float hash(float n) { return fract(sin(n * 91.345) * 47453.21); }

void vertex() { local_pos = VERTEX; }

void fragment() {
    // Across the pane: the wide horizontal axis of the box; up: y. Both from 0 to 1.
    float across = (along_z ? local_pos.z : local_pos.x) / size.x + 0.5;
    float up = local_pos.y / size.y + 0.5;
    float column = floor(across * 22.0);
    float h = hash(column);
    // Thin streaks of colour, brighter at the top, drifting down slowly.
    float streak = step(0.55, h) * (0.5 + 0.5 * sin(up * 9.0 + TIME * (0.6 + h) + h * 30.0));
    vec3 tint = mix(vec3(0.95, 0.45, 0.45), vec3(0.35, 0.85, 0.8), smoothstep(0.35, 0.75, 1.0 - up));
    tint = mix(tint, vec3(0.55, 0.9, 0.5), step(0.85, h));
    vec3 frost = vec3(0.92, 0.96, 0.95);
    float edge = smoothstep(0.0, 0.04, across) * smoothstep(1.0, 0.96, across);   // softer at the jambs
    ALBEDO = mix(frost, tint, streak * (0.55 + 0.45 * up));
    ALPHA = (0.28 + 0.4 * streak) * edge;
}
",
    };

    /// <summary>Adds the pane, the frame and the floor light to the barrier node (centred on the grid box).</summary>
    public static void Decorate(Node3D grid, Vector3 boxSize)
    {
        // The pane's thin axis is x or z (rooms run along +X, some doorways face z).
        var alongZ = boxSize.Z >= boxSize.X;
        var width = alongZ ? boxSize.Z : boxSize.X;
        var height = boxSize.Y;

        var glass = new ShaderMaterial { Shader = GlassShader };
        glass.SetShaderParameter("size", new Vector2(width, height));
        glass.SetShaderParameter("along_z", alongZ);
        grid.AddChild(new MeshInstance3D
        {
            Name = "Glass",
            Mesh = new BoxMesh { Size = alongZ ? new Vector3(0.04f, height, width) : new Vector3(width, height, 0.04f), Material = glass },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });

        // A thin warm frame: two jambs, a lintel and a sill, a bit deeper than the pane.
        var frame = new StandardMaterial3D { AlbedoColor = new Color(0.72f, 0.55f, 0.3f), Metallic = 0.6f, Roughness = 0.35f };
        const float t = 0.06f, d = 0.16f;
        Vector3 Box(float across, float up) => alongZ ? new Vector3(d, up, across) : new Vector3(across, up, d);
        Vector3 At(float across, float up) => alongZ ? new Vector3(0, up, across) : new Vector3(across, up, 0);
        void Piece(string name, Vector3 size, Vector3 at) =>
            grid.AddChild(new MeshInstance3D { Name = name, Mesh = new BoxMesh { Size = size, Material = frame }, Position = at });
        Piece("JambA", Box(t, height), At(-width / 2 + t / 2, 0));
        Piece("JambB", Box(t, height), At(width / 2 - t / 2, 0));
        Piece("Lintel", Box(width, t), At(0, height / 2 - t / 2));
        Piece("Sill", Box(width, 0.02f), At(0, -height / 2 + 0.01f));

        // Pale light spilling on the floor on both sides.
        grid.AddChild(new OmniLight3D
        {
            Name = "Spill",
            LightColor = new Color(0.85f, 1f, 0.95f),
            LightEnergy = 0.8f,
            OmniRange = 3f,
            Position = new Vector3(0, -height / 2 + 0.6f, 0),
        });
    }
}
