using System;
using System.IO;

namespace QwertyShift
{
    /// <summary>
    /// Represents the main application orchestrator that ties together settings, layout detection, and announcers.
    /// </summary>
    public class QwertyShiftApplication : IDisposable
    {
        /// <summary>Gets the service responsible for managing application settings.</summary>
        public ISettingsService Settings { get; }

        /// <summary>Gets the manager responsible for application startup behavior.</summary>
        public IStartupManager StartupManager { get; }

        /// <summary>Gets the logger used for recording application errors and events.</summary>
        public ILogger Logger { get; }

        /// <summary>Gets the component that detects keyboard layout changes.</summary>
        public LayoutDetector LayoutDetector { get; }

        /// <summary>Gets the component that announces layout changes using text-to-speech.</summary>
        public SpeechAnnouncer SpeechAnnouncer { get; }

        /// <summary>Gets the component that announces layout changes using audio files.</summary>
        public SoundAnnouncer SoundAnnouncer { get; }

        /// <summary>Gets the manager that handles low-level Windows events, such as typing pauses.</summary>
        public WindowsEventManager EventManager { get; }

        /// <summary>
        /// Initializes a new instance of the QwertyShiftApplication class with all necessary dependencies.
        /// </summary>
        public QwertyShiftApplication(
            ISettingsService settings,
            IStartupManager startupManager,
            ILogger logger,
            LayoutDetector layoutDetector,
            SpeechAnnouncer speechAnnouncer,
            SoundAnnouncer soundAnnouncer,
            WindowsEventManager eventManager)
        {
            Settings = settings;
            StartupManager = startupManager;
            Logger = logger;
            LayoutDetector = layoutDetector;
            SpeechAnnouncer = speechAnnouncer;
            SoundAnnouncer = soundAnnouncer;
            EventManager = eventManager;
        }

        /// <summary>
        /// Configures the application settings, loads external resources (sounds and names), 
        /// and subscribes to essential business logic events.
        /// </summary>
        public void Initialize()
        {
            // Set timings
            EventManager.TypingPauseMs = Settings.TypingPauseMs;

            // Load custom layout names
            var customNames = Settings.LoadCustomNames();
            foreach (var kvp in customNames)
                LayoutDetector.SetCustomName(kvp.Key, kvp.Value);

            // Load sounds
            LoadSystemSounds();
            var customSounds = Settings.LoadCustomSounds();
            foreach (var kvp in customSounds)
                SoundAnnouncer.RegisterSound(kvp.Key, kvp.Value);

            // Subscribe to business logic events
            EventManager.LayoutCheckRequired += isNewSession => LayoutDetector.TriggerCheck(isNewSession);
            LayoutDetector.LayoutChanged += OnLayoutChanged;
        }

        /// <summary>
        /// Scans the default Windows media folder and registers all .wav files into the sound announcer.
        /// </summary>
        private void LoadSystemSounds()
        {
            try
            {
                string mediaFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), AppConstants.WindowsMediaFolder);
                if (Directory.Exists(mediaFolder))
                {
                    foreach (string file in Directory.GetFiles(mediaFolder, "*.wav"))
                        SoundAnnouncer.RegisterSound(Path.GetFileNameWithoutExtension(file), file);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "System sounds loading failed");
            }
        }

        /// <summary>
        /// Handles the event triggered when the user changes the keyboard layout.
        /// Routes the announcement to either the speech or sound announcer based on current settings.
        /// </summary>
        private void OnLayoutChanged(KeyboardLayoutInfo info)
        {
            if (Settings.UseVoice)
                SpeechAnnouncer.Announce(info);
            else
                SoundAnnouncer.Announce(info);
        }

        /// <summary>
        /// Releases all external resources and unhooks events used by the application components.
        /// </summary>
        public void Dispose()
        {
            EventManager?.Dispose();
            SpeechAnnouncer?.Dispose();
            SoundAnnouncer?.Dispose();
        }
    }
}