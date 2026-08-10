using System;
using System.Collections.Generic;

namespace QwertyShift
{
    public interface ISettingsService
    {
        bool UseVoice { get; set; }
        int TypingPauseMs { get; set; }
        bool IsFirstHide { get; set; }

        string Language { get; set; }

        void SaveSettings();

        void SaveCustomName(IntPtr hkl, string name);
        Dictionary<IntPtr, string> LoadCustomNames();

        void SaveLayoutSound(IntPtr hkl, string soundId);
        string GetLayoutSound(IntPtr hkl);

        void SaveCustomSoundPath(string soundId, string path);
        Dictionary<string, string> LoadCustomSounds();
    }
}