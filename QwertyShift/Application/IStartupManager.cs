namespace QwertyShift
{
    public interface IStartupManager
    {
        bool IsAutorunEnabled();
        bool SetAutorun(bool enable);
    }
}