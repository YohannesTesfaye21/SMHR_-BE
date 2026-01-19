namespace SMHFR_BE.Services;

public interface IConnectionStringService
{
    string GetConnectionString();
    void PersistConnectionString(string connectionString);
    bool HasPersistedConnectionString();
}
