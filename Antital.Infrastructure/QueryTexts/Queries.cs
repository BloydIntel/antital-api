namespace Antital.Infrastructure.QueryTexts;

public static class Queries
{

    private static string GetQuery(string name)
    {
#if DEBUG
        return File.ReadAllText($"../Antital.Infrastructure/QueryTexts/{name}.sql");
#else
        return File.ReadAllText($"QueryTexts/{name}.sql");
#endif
    }

}
