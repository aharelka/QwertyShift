using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace QwertyShift
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ILogger logger = new FileLogger();

            // Глобальный перехват ошибок теперь здесь
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                logger.LogError(e.ExceptionObject as Exception, "FATAL CRASH (AppDomain)");
            Application.ThreadException += (s, e) =>
                logger.LogError(e.Exception, "FATAL CRASH (Thread)");

            // Собираем зависимости (Composition Root)
            var settingsService = new RegistrySettingsStore(logger);
            var startupManager = new WindowsStartupManager(logger);

            var eventManager = new WindowsEventManager();
            var layoutDetector = new LayoutDetector();
            var speechAnnouncer = new SpeechAnnouncer();
            var soundAnnouncer = new SoundAnnouncer();

            // Создаем ядро приложения
            using (var app = new QwertyShiftApplication(
                settingsService, startupManager, logger,
                layoutDetector, speechAnnouncer, soundAnnouncer, eventManager))
            {
                app.Initialize();

                bool startHidden = args.Contains(AppConstants.AutorunSwitch);

                // Форма ничего не знает о создании классов — только получает готовое API
                Application.Run(new MainForm(app, startHidden));
            }
        }
    }
}