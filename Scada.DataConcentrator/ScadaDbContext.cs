using Microsoft.EntityFrameworkCore;

namespace Scada.DataConcentrator;

// Represents our SQLite database. Each DbSet below is one table.
public class ScadaDbContext : DbContext
{
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Alarm> Alarms => Set<Alarm>();
    public DbSet<ActivatedAlarm> ActivatedAlarms => Set<ActivatedAlarm>();
    public DbSet<TagValue> TagValues => Set<TagValue>();

    // Tell EF which database to use and where to find it.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // Store the database in a stable per-user location, so every part of
        // the app finds the same file no matter what folder it runs from.
        string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dir = Path.Combine(folder, "ScadaProject");
        Directory.CreateDirectory(dir);
        string dbPath = Path.Combine(dir, "scada.db");

        options.UseSqlite($"Data Source={dbPath}");
    }
}
