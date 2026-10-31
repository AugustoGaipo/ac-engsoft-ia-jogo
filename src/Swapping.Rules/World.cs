using Swapping.Rules.Model;
using Swapping.Rules.Physics;

namespace Swapping.Rules;

/// <summary>
/// Holds the containers and their variables and tracks who uses which.
/// Invariant: every variable is used by exactly one container, and every container uses exactly one
/// variable per property.
/// </summary>
public sealed class World
{
    private readonly List<Container> _order = new();
    private readonly Dictionary<string, Container> _containers = new();
    private readonly Dictionary<(string Container, IPropertyKind Kind), Variable> _uses = new();
    private readonly Dictionary<Variable, string> _users = new();

    public IReadOnlyList<Container> Containers => _order;

    /// <summary>Registers a container and creates one owned variable per property.</summary>
    public void Add(Container container, Vec3 position, double massKg, Vec3 velocity)
    {
        if (_containers.ContainsKey(container.Id))
            throw new ArgumentException($"Container '{container.Id}' already exists.", nameof(container));

        _containers.Add(container.Id, container);
        _order.Add(container);

        var energy = Formulas.KineticEnergy(massKg, velocity.Length);
        Use(container.Id, new Variable(PropertyKind.Position, container.Id, VariableValue.OfPosition(position)));
        Use(container.Id, new Variable(PropertyKind.Mass, container.Id, VariableValue.OfMass(massKg)));
        Use(container.Id, new Variable(PropertyKind.KineticEnergy, container.Id, VariableValue.OfEnergy(energy, velocity)));
    }

    public Container Get(string id) =>
        _containers.TryGetValue(id, out var container)
            ? container
            : throw new KeyNotFoundException($"Unknown container '{id}'.");

    public Variable VariableUsedBy(string containerId, IPropertyKind kind)
    {
        Get(containerId);
        return _uses[(containerId, kind)];
    }

    public string UserOf(Variable variable) => _users[variable];

    /// <summary>The two containers exchange the variables of one property.</summary>
    public void ExchangeVariables(string first, string second, IPropertyKind kind)
    {
        var a = VariableUsedBy(first, kind);
        var b = VariableUsedBy(second, kind);
        Use(first, b);
        Use(second, a);
    }

    public bool CheckInvariant()
    {
        var used = new HashSet<Variable>();
        foreach (var container in _order)
            foreach (var kind in PropertyKind.All)
            {
                if (!_uses.TryGetValue((container.Id, kind), out var variable)) return false;
                if (!ReferenceEquals(variable.Kind, kind)) return false;
                if (!used.Add(variable)) return false;
                if (_users[variable] != container.Id) return false;
            }

        return used.Count == _users.Count && used.Count == _order.Count * PropertyKind.All.Count;
    }

    private void Use(string containerId, Variable variable)
    {
        _uses[(containerId, variable.Kind)] = variable;
        _users[variable] = containerId;
    }
}
