using System.Runtime.InteropServices;

namespace MultiTaskingTest;

public static class CloseHandler
{
    [DllImport("Kernel32")]
    public static extern bool SetConsoleCtrlHandler(SetConsoleCtrlEventHandler handler, bool add);
    public delegate bool SetConsoleCtrlEventHandler(CtrlType sig);

    private static Action _cleanUpCode;
    
    public static void OnClose(Action cleanUpCode)
    {
        SetConsoleCtrlHandler(Handler, true);
        _cleanUpCode = cleanUpCode;
    }
    
    private static bool Handler(CtrlType signal)
    {
        switch (signal)
        {
            case CtrlType.CTRL_BREAK_EVENT:
            case CtrlType.CTRL_C_EVENT:
            case CtrlType.CTRL_LOGOFF_EVENT:
            case CtrlType.CTRL_SHUTDOWN_EVENT:
            case CtrlType.CTRL_CLOSE_EVENT:
                _cleanUpCode();
                Environment.Exit(0);
                return false;
            default:
                return false;
        }
    }
    public enum CtrlType
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1,
        CTRL_CLOSE_EVENT = 2,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT = 6
    }
    
}