namespace SMHFR_BE.Services;

public interface IDatabaseInitializationService
{
    Task InitializeAsync();
    Task VerifyConnectionAsync();
}
