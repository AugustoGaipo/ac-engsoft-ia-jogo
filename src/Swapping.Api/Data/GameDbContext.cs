using Microsoft.EntityFrameworkCore;

namespace Swapping.Api.Data;

/// <summary>One database for the whole API (profiles + progress tables).</summary>
public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
}
