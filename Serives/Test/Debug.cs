namespace GameServices.GameBasic
{
    public class Debug
    {
        public static void Log(string input)
        {
#if DEBUG
            System.Diagnostics.Debug.Write(input);
#endif
        }
        public static void LogLine(string input)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(input);
#endif
        }
    }
}
