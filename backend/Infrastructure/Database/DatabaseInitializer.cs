using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Fourway.Api.Infrastructure.Database;

public static class DatabaseInitializer
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    public static async Task InitializeAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Databáze je připravena, migrace jsou aplikované.");
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && IsTransient(ex))
            {
                logger.LogWarning(
                    ex,
                    "Pokus o připojení k databázi {Attempt}/{MaxAttempts} selhal. " +
                    "Zkouším znovu za {Delay} s.",
                    attempt,
                    MaxAttempts,
                    RetryDelay.TotalSeconds);

                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }

    private static bool IsTransient(Exception exception) =>
        exception is TimeoutException or NpgsqlException { IsTransient: true };
}
