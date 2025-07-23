namespace FileStorage.ConsoleApp.Constants;

public static class AppConstants
{
    public static string FileStoragePath  = Path.Combine(Directory.GetCurrentDirectory(), "Chunks");
    public const int DefaultMinSizeInMb = 1 * 1024 * 1024;
    public const int DefaultMaxSizeInMb = 5 * 1024 * 1024;
}