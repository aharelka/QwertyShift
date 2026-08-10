using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace QwertyShift
{
    /// <summary>
    /// Provides an implementation of the settings service that stores application preferences and custom data in the Windows Registry.
    /// </summary>
    public class RegistrySettingsStore : ISettingsService
    {
        private readonly ILogger _logger;

        public bool UseVoice { get; set; }
        public int TypingPauseMs { get; set; }
        public bool IsFirstHide { get; set; }
        public string Language { get; set; }

        /// <summary>
        /// Initializes a new instance of the RegistrySettingsStore class and loads the default configuration from the registry.
        /// </summary>
        public RegistrySettingsStore(ILogger logger)
        {
            _logger = logger;
            UseVoice = Convert.ToInt32(LoadSetting(SettingKeys.UseVoice, 1)) == 1;
            TypingPauseMs = Convert.ToInt32(LoadSetting(SettingKeys.TypingPauseMs, 1500));
            IsFirstHide = Convert.ToInt32(LoadSetting(SettingKeys.IsFirstHide, 1)) == 1;
        }

        /// <summary>
        /// Saves the current core application settings (voice, pause duration, first hide status) to the registry.
        /// </summary>
        public void SaveSettings()
        {
            SaveSetting(SettingKeys.UseVoice, UseVoice ? 1 : 0);
            SaveSetting(SettingKeys.TypingPauseMs, TypingPauseMs);
            SaveSetting(SettingKeys.IsFirstHide, IsFirstHide ? 1 : 0);
        }

        /// <summary>
        /// Saves a custom readable name for a specific keyboard layout.
        /// </summary>
        public void SaveCustomName(IntPtr hkl, string name)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeys.CustomNamesSubKey))
                {
                    if (string.IsNullOrWhiteSpace(name)) key.DeleteValue(hkl.ToString(), false);
                    else key.SetValue(hkl.ToString(), name);
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Сохранение имени"); }
        }

        /// <summary>
        /// Loads all custom keyboard layout names previously saved in the registry.
        /// </summary>
        public Dictionary<IntPtr, string> LoadCustomNames()
        {
            var result = new Dictionary<IntPtr, string>();
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeys.CustomNamesSubKey))
                {
                    if (key != null)
                    {
                        foreach (string hklStr in key.GetValueNames())
                            if (long.TryParse(hklStr, out long val))
                                result[(IntPtr)val] = key.GetValue(hklStr) as string;
                    }
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Загрузка своих имен"); }
            return result;
        }

        public void SaveLayoutSound(IntPtr hkl, string soundId) => SaveSetting(SettingKeys.SoundPrefix + hkl.ToString(), soundId);

        public string GetLayoutSound(IntPtr hkl) => LoadSetting(SettingKeys.SoundPrefix + hkl.ToString(), null) as string;

        /// <summary>
        /// Saves the physical file path for a custom sound identifier.
        /// </summary>
        public void SaveCustomSoundPath(string soundId, string path)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeys.CustomSoundsSubKey))
                    key?.SetValue(soundId, path);
            }
            catch (Exception ex) { _logger.LogError(ex, "Сохранение кастомного звука"); }
        }

        /// <summary>
        /// Loads all custom sound identifiers and their corresponding file paths from the registry.
        /// </summary>
        public Dictionary<string, string> LoadCustomSounds()
        {
            var result = new Dictionary<string, string>();
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeys.CustomSoundsSubKey))
                {
                    if (key != null)
                    {
                        foreach (string soundId in key.GetValueNames())
                            result[soundId] = key.GetValue(soundId) as string;
                    }
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Загрузка кастомных звуков"); }
            return result;
        }

        /// <summary>
        /// Helper method to save a single setting value to the base registry path.
        /// </summary>
        private void SaveSetting(string name, object value)
        {
            try { using (var key = Registry.CurrentUser.CreateSubKey(RegistryKeys.BasePath)) key?.SetValue(name, value); }
            catch (Exception ex) { _logger.LogError(ex, $"Сохранение настройки {name}"); }
        }

        /// <summary>
        /// Helper method to load a single setting value from the base registry path.
        /// </summary>
        private object LoadSetting(string name, object defaultValue)
        {
            try { using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeys.BasePath)) return key?.GetValue(name) ?? defaultValue; }
            catch { return defaultValue; }
        }
    }
}