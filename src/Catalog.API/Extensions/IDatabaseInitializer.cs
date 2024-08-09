
public interface IDatabaseInitializer
{
    Task SeedAsync();
    Task EnsureAdminIdentitiesAsync();
}
