using System;
using System.Collections.Concurrent;
using System.IO;
using System.Media;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace QwertyShift
{
    /// <summary>
    /// Uses Windows Text-to-Speech to announce the current keyboard layout aloud.
    /// </summary>
    public class SpeechAnnouncer : ILayoutAnnouncer
    {
        private SpeechSynthesizer _speech;

        /// <summary>
        /// Initializes a new instance of the SpeechAnnouncer class and configures the synthesizer volume and rate.
        /// </summary>
        public SpeechAnnouncer()
        {
            _speech = new SpeechSynthesizer { Volume = 100, Rate = 1 };
        }

        /// <summary>
        /// Cancels any currently playing speech and announces the newly selected keyboard layout.
        /// </summary>
        /// <param name="info">Information about the current keyboard layout, including the text to speak.</param>
        public void Announce(KeyboardLayoutInfo info)
        {
            try
            {
                _speech.SpeakAsyncCancelAll();
                _speech.SpeakAsync(info.SpokenText);
            }
            catch { }
        }

        /// <summary>
        /// Releases all resources used by the speech synthesizer.
        /// </summary>
        public void Dispose()
        {
            _speech?.SpeakAsyncCancelAll();
            _speech?.Dispose();
        }
    }

    /// <summary>
    /// Manages and plays specific audio files associated with different keyboard layouts.
    /// </summary>
    public class SoundAnnouncer : ILayoutAnnouncer
    {
        /// <summary>
        /// A thread-safe dictionary that maps a keyboard layout pointer (handle) to a specific sound ID.
        /// </summary>
        public ConcurrentDictionary<IntPtr, string> LayoutSoundMap { get; } = new ConcurrentDictionary<IntPtr, string>();

        private readonly ConcurrentDictionary<string, SoundPlayer> _players = new ConcurrentDictionary<string, SoundPlayer>();
        private readonly ConcurrentDictionary<string, string> _filePaths = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Loads and registers an audio file in memory to be played later.
        /// </summary>
        /// <param name="id">The unique identifier for the sound.</param>
        /// <param name="filePath">The physical path to the audio file on the computer.</param>
        public void RegisterSound(string id, string filePath)
        {
            if (filePath == null) return;
            var player = new SoundPlayer(filePath);
            player.LoadAsync();
            _players[id] = player;
            _filePaths[id] = filePath;
        }

        /// <summary>
        /// Retrieves a dictionary containing all registered sound IDs and their corresponding file paths.
        /// </summary>
        /// <returns>A dictionary of registered sounds.</returns>
        public System.Collections.Generic.Dictionary<string, string> GetRegisteredSounds()
        {
            return new System.Collections.Generic.Dictionary<string, string>(_filePaths);
        }

        /// <summary>
        /// Plays the audio file associated with the provided keyboard layout. 
        /// If no sound is found or an error occurs, it plays a default Windows system sound.
        /// </summary>
        /// <param name="info">Information about the current keyboard layout.</param>
        public void Announce(KeyboardLayoutInfo info)
        {
            if (LayoutSoundMap.TryGetValue(info.Handle, out string soundId) && _players.TryGetValue(soundId, out SoundPlayer player))
            {
                try
                {
                    if (player.IsLoadCompleted) player.Play();
                    else Task.Run(() => { try { player.Play(); } catch { SystemSounds.Asterisk.Play(); } });
                }
                catch { SystemSounds.Asterisk.Play(); }
            }
            else { SystemSounds.Asterisk.Play(); }
        }

        /// <summary>
        /// Releases all resources and safely disposes of the audio players.
        /// </summary>
        public void Dispose()
        {
            foreach (var player in _players.Values) player?.Dispose();
            _players.Clear();
            _filePaths.Clear();
        }
    }
}