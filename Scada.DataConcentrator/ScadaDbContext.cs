using Microsoft.EntityFrameworkCore;

namespace Scada.DataConcentrator;

// Represents our SQLite database. Each DbSet below is one table.
public class ScadaDbContext : DbContext
{
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Alarm> Alarms => Set<Alarm>();
    public DbSet<ActivatedAlarm> ActivatedAlarms => Set<ActivatedAlarm>();

    // Tell EF which database to use and where to find it.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=scada.db");
    }
}
