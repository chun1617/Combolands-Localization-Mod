namespace UnityEngine.TextCore.LowLevel
{
    public enum FontEngineError
    {
        Success = 0
    }

    public static class FontEngine
    {
        public static FontEngineError InitializeFontEngine() => FontEngineError.Success;
    }
}
