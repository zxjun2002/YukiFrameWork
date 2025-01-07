namespace MIKUFramework.IOC
{
    public static class IoCHelper
    {
        public static MIKUIoC Instance;

        public static void Initialize()
        {
            Instance = new MIKUIoC();
        }
    }
}