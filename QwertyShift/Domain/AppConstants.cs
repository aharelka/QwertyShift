namespace QwertyShift
{
    /// <summary>
    /// Contains global constant values used throughout the application.
    /// </summary>
    public static class AppConstants
    {
        public const string AppName = "QwertyShift";
        public const string LogFileName = "QwertyShift_ErrorLog.txt";
        public const string CustomSoundOption = "<Выбрать свой файл...>";
        public const string AutorunSwitch = "-autorun";
        public const string WindowsMediaFolder = "Media";
        public const string DefaultSoundKey = "System Default";
    }

    /// <summary>
    /// Contains the Windows Registry paths used for saving application settings and autorun configuration.
    /// </summary>
    public static class RegistryKeys
    {
        public const string BasePath = @"SOFTWARE\" + AppConstants.AppName;
        public const string CustomSoundsSubKey = BasePath + @"\CustomSounds";
        public const string CustomNamesSubKey = BasePath + @"\CustomNames";
        public const string AutoRunPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    }

    /// <summary>
    /// Contains the specific keys used to identify individual settings within the application or registry.
    /// </summary>
    public static class SettingKeys
    {
        public const string UseVoice = "UseVoice";
        public const string TypingPauseMs = "TypingPauseMs";
        public const string IsFirstHide = "IsFirstHide";
        public const string SoundPrefix = "Sound_";
    }
}