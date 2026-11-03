using Godot;
using Swapping.Game.Pieces;
using Swapping.GameLogic;
using Swapping.Levels;
using Swapping.Levels.Model;

namespace Swapping.Game;

/// <summary>
/// Editor-only preview of a level file (user's request): draws every piece of every room where the game
/// will build it, with the game's colors and a label per piece. It reads the same JSON as the game and
/// the API, so the preview cannot drift. In the running game it removes itself (the game builds the real
/// pieces). Nothing it draws is saved into the scene.
/// </summary>
[Tool]
public partial class LevelPreview : Node3D
{
    private static readonly Color BoxColor = new(0.8f, 0.5f, 0.25f);
    private static readonly Color BallColor = new(0.95f, 0.8f, 0.2f);
    private static readonly Color LockedColor = new(0.45f, 0.45f, 0.48f);

    /// <summary>Only for <c>--preview-shot</c> (the agent checks the preview in a picture, it cannot see the editor).</summary>
    public static bool ShowInGame { get; set; }

    /// <summary>The level file, e.g. <c>res://levels/level_1.json</c> or <c>res://sandbox/sandbox.json</c>.</summary>
    [Export]
    public string LevelFile { get; set; } = "";

    /// <summary>Rebuilds the preview (tick it in the Inspector after editing the JSON).</summary>
    [Export]
    public bool Refresh
    {
        get => false;
        set
        {
            if (value && IsInsideTree())
                Build();
        }
    }

    public override void _Ready()
    {
        if (!Engine.IsEditorHint() && !ShowInGame)
        {
            QueueFree();
            return;
        }
        Build();
    }

    private void Build()
    {
        foreach (var child in GetChildren())
            child.QueueFree();

        var path = ProjectSettings.GlobalizePath(LevelFile);
        if (!System.IO.File.Exists(path))
        {
            AddLabel($"{LevelFile} não encontrado (compile o jogo para copiar os níveis)", Vector3.Up * 3, Colors.Red);
            return;
        }
        var (level, errors) = LevelParser.Parse(System.IO.File.ReadAllText(path));
        if (level is null)
        {
            AddLabel("JSON com erro: " + string.Join("; ", errors.Select(e => $"{e.Path}: {e.Code}")), Vector3.Up * 3, Colors.Red);
            return;
        }

        foreach (var room in level.Rooms)
        {
            foreach (var obj in room.Objects)
            {
                var color = obj.Locked ? LockedColor : obj.Kind == ObjectKind.Ball ? BallColor : BoxColor;
                PrimitiveMesh mesh = obj.Kind == ObjectKind.Ball
                    ? new SphereMesh { Radius = (float)obj.Size.X / 2, Height = (float)obj.Size.X }
                    : new BoxMesh { Size = obj.Size.ToGodot() };
                AddMesh(mesh, obj.Position.ToGodot(), color);
                AddLabel($"{obj.Id}\n{obj.MassKg} kg{(obj.Locked ? " · travada" : "")}", obj.Position.ToGodot() + Vector3.Up * ((float)obj.Size.Y / 2 + 0.4f), Colors.White);
            }
            foreach (var button in room.Buttons)
            {
                var size = button.Size.ToGodot();
                AddMesh(new BoxMesh { Size = size }, button.Position.ToGodot() + Vector3.Up * size.Y / 2, new Color(0.75f, 0.2f, 0.2f));
                AddLabel($"{button.Id}\n≥ {button.RequiredMassKg} kg ({(button.Detects == ButtonDetects.Any ? "conta você" : "só objetos")})",
                    button.Position.ToGodot() + Vector3.Up * 0.6f, Colors.White);
            }
            foreach (var door in room.Doors)
            {
                AddMesh(new BoxMesh { Size = door.Box.Size() }, door.Box.Center(), new Color(0.3f, 0.3f, 0.35f));
                AddLabel($"{door.Id}\nabre com {string.Join(", ", door.OpensWith)}", door.Box.Center() + Vector3.Up * 2f, Colors.White);
            }
            foreach (var barrier in room.Barriers)
            {
                var (r, g, b) = BarrierPalette.For(barrier.Filter);
                var color = new Color((float)r, (float)g, (float)b, 0.3f);
                AddMesh(new BoxMesh { Size = barrier.Box.Size() }, barrier.Box.Center(), color);
            }
            foreach (var zone in room.KillZones)
            {
                AddMesh(new BoxMesh { Size = zone.Box.Size() }, zone.Box.Center(), new Color(1f, 0.1f, 0.1f, 0.25f));
                AddLabel($"{zone.Id} (morte)", zone.Box.Center() + Vector3.Up * 1.5f, new Color(1f, 0.5f, 0.5f));
            }
            AddMesh(new BoxMesh { Size = room.Exit.Size() }, room.Exit.Center(), new Color(0.2f, 1f, 0.3f, 0.3f));

            // Spawn: a green column at the feet and an arrow along the yaw (0° = looking along -Z).
            var feet = room.Spawn.Position.ToGodot();
            AddMesh(new CylinderMesh { TopRadius = 0.3f, BottomRadius = 0.3f, Height = 1.8f }, feet + Vector3.Up * 0.9f, new Color(0.2f, 0.9f, 0.3f, 0.5f));
            var yaw = Mathf.DegToRad((float)room.Spawn.YawDegrees);
            var forward = new Vector3(-Mathf.Sin(yaw), 0, -Mathf.Cos(yaw));
            AddMesh(new BoxMesh { Size = new Vector3(0.1f, 0.1f, 1f) }, feet + Vector3.Up * 1.6f + forward * 0.6f, new Color(0.2f, 0.9f, 0.3f),
                Basis.LookingAt(forward, Vector3.Up));
            AddLabel($"sala {room.Id}: entrada", feet + Vector3.Up * 2.3f, new Color(0.6f, 1f, 0.6f));
        }
    }

    private void AddMesh(PrimitiveMesh mesh, Vector3 position, Color color, Basis? basis = null)
    {
        var material = new StandardMaterial3D { AlbedoColor = color };
        if (color.A < 1)
            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        mesh.Material = material;
        AddChild(new MeshInstance3D { Mesh = mesh, Transform = new Transform3D(basis ?? Basis.Identity, position) });
    }

    private void AddLabel(string text, Vector3 position, Color color) =>
        AddChild(new Label3D
        {
            Text = text,
            Position = position,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            PixelSize = 0.004f,
            FontSize = 40,
            OutlineSize = 10,
            Modulate = color,
        });
}
