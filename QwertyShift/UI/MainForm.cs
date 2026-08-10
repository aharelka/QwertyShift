using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QwertyShift
{
    /// <summary>
    /// Represents the main graphical user interface for the QwertyShift application.
    /// Handles user interactions, layout configurations, and background tray execution.
    /// </summary>
    public partial class MainForm : Form
    {
        // --- WINDOWS 11 DWM API ---
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_CAPTION_COLOR = 35;

        private readonly QwertyShiftApplication _app;
        private bool _startHidden;

        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private Timer _saveDebounceTimer;

        private bool _allowExit = false;
        private bool _isUpdatingUI = false;
                
        private string _currentLang = "en";       
        private readonly string[] _langCodes = { "ru", "en", "fr", "es", "de", "it", "pt", "zh", "ja", "ar" };        
        private readonly string[] _langNames = {
            "Русский", "English", "Français", "Español", "Deutsch",
            "Italiano", "Português", "中文", "日本語", "العربية"
        };

        // UI Elements
        private DataGridView layoutGrid;
        private FluentToggle tglUseVoice;
        private Label lblVoiceTxt;
        private ComboBox soundSelector;
        private Label soundSelectLabel;
        private ComboBox languageSelector; 

        // Localization labels
        private Label mainTitle;
        private Label lblLayouts;
        private Label lblNotify;
        private Label lblPause;
        private Label lblMin;
        private TrackBar timingTrackBar;
        private Label lblMax;
        private Label timingValueLabel;
        private Label lblStartupIcon;
        private Label lblStartupTxt;
        private FluentToggle tglAutoStart;
        private Label statusLabel;

        /// <summary>
        /// Represents a sound option available for selection in the UI.
        /// </summary>
        private class SoundItem
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public override string ToString() => DisplayName;
        }

        /// <summary>
        /// Initializes a new instance of the MainForm.
        /// </summary>       
        public MainForm(QwertyShiftApplication app, bool startHidden)
        {
            _app = app;
            _startHidden = startHidden;            

            InitializeComponent();

            GenerateAndSetMicroQIcon();

            // Apply Windows 11 rounded corners and custom caption color
            int roundedCorners = 2;
            DwmSetWindowAttribute(this.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref roundedCorners, sizeof(int));
            int captionColor = 0x00F3F3F3;
            DwmSetWindowAttribute(this.Handle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

            SetupFluentUI();
            SetupLogic();
        }

        protected override void SetVisibleCore(bool value)
        {
            if (_startHidden) { value = false; if (!this.IsHandleCreated) CreateHandle(); }
            base.SetVisibleCore(value);
        }

        /// <summary>
        /// Configures the visual appearance of the form and initializes custom UI controls.
        /// </summary>
        private void SetupFluentUI()
        {
            this.Text = "QwertyShift";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(243, 243, 243);

            Font fontTitle = new Font("Segoe UI", 21f, FontStyle.Regular);
            Font fontSection = new Font("Segoe UI Semibold", 10.5f, FontStyle.Regular);
            Font fontBody = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            Font fontSecondary = new Font("Segoe UI", 9f, FontStyle.Regular);
            Font fontIcon = new Font("Segoe MDL2 Assets", 12f, FontStyle.Regular);

            Color colorTitle = Color.FromArgb(26, 26, 26);
            Color colorBody = Color.FromArgb(26, 26, 26);
            Color colorSec = Color.FromArgb(93, 93, 93);
            Color colorSecLight = Color.FromArgb(140, 140, 140);

            int marginX = 24;
            int rightEdge = 460 - marginX;
            int toggleX = rightEdge - 40;
            int comboX = rightEdge - 130;

            // --- HEADER ---
            mainTitle = new Label { Font = fontTitle, Location = new Point(marginX, 24), AutoSize = true, ForeColor = colorTitle };
            this.Controls.Add(mainTitle);

            // --- LANGUAGE SELECTOR ---
            languageSelector = new ComboBox
            {
                Location = new Point(comboX, 36),
                Size = new Size(130, 24),
                Font = fontSecondary,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.System
            };

            languageSelector.Items.AddRange(_langNames);
                        
            int langIndex = Array.IndexOf(_langCodes, _currentLang);
            languageSelector.SelectedIndex = langIndex >= 0 ? langIndex : 0;

            this.Controls.Add(languageSelector);

            // --- Layouts ---
            lblLayouts = new Label { Font = fontSection, Location = new Point(marginX, 76), AutoSize = true, ForeColor = colorTitle };

            FluentSurface gridSurface = new FluentSurface { Location = new Point(marginX, 104), Size = new Size(412, 128), Padding = new Padding(4) };

            layoutGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.None,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                RowHeadersVisible = false,
                ScrollBars = ScrollBars.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EnableHeadersVisualStyles = false,
                GridColor = Color.White,
                RowTemplate = { Height = 40 }
            };

            layoutGrid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                Font = fontSecondary,
                ForeColor = colorSecLight,
                SelectionBackColor = Color.White,
                Alignment = DataGridViewContentAlignment.BottomLeft
            };
            layoutGrid.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = fontBody,
                SelectionBackColor = Color.FromArgb(243, 243, 243),
                SelectionForeColor = colorBody,
                ForeColor = colorBody,
                Padding = new Padding(4, 0, 4, 0),
                BackColor = Color.White
            };

            gridSurface.Controls.Add(layoutGrid);
            this.Controls.Add(lblLayouts);
            this.Controls.Add(gridSurface);

            // --- Notifications ---
            lblNotify = new Label { Font = fontSection, Location = new Point(marginX, 264), AutoSize = true, ForeColor = colorTitle };

            Label lblVoiceIcon = new Label { Text = "\uE15D", Font = fontIcon, Location = new Point(marginX, 296), Size = new Size(24, 24), ForeColor = colorSec };
            lblVoiceTxt = new Label { Font = fontBody, Location = new Point(56, 298), AutoSize = true };
            tglUseVoice = new FluentToggle { Location = new Point(toggleX, 298), Size = new Size(40, 20) };

            soundSelectLabel = new Label { Font = fontSecondary, Location = new Point(56, 336), AutoSize = true, ForeColor = colorSec };
            soundSelector = new ComboBox { Location = new Point(comboX, 332), Size = new Size(130, 24), Font = fontSecondary, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.System };

            this.Controls.Add(lblNotify); this.Controls.Add(lblVoiceIcon); this.Controls.Add(lblVoiceTxt); this.Controls.Add(tglUseVoice);
            this.Controls.Add(soundSelectLabel); this.Controls.Add(soundSelector);

            // --- Pauses ---
            lblPause = new Label { Font = fontSection, Location = new Point(marginX, 384), AutoSize = true, ForeColor = colorTitle };
            lblMin = new Label { Font = fontSecondary, Location = new Point(marginX, 420), AutoSize = true, ForeColor = colorSec };
            timingTrackBar = new TrackBar { Location = new Point(62, 416), Size = new Size(240, 24), Minimum = 0, Maximum = 10, TickFrequency = 1, SmallChange = 1, LargeChange = 1, TickStyle = TickStyle.None };
            lblMax = new Label { Font = fontSecondary, Location = new Point(302, 420), AutoSize = true, ForeColor = colorSec };
            timingValueLabel = new Label { Font = fontBody, Location = new Point(toggleX - 30, 418), Size = new Size(70, 20), TextAlign = ContentAlignment.TopRight, ForeColor = colorTitle };

            this.Controls.Add(lblPause); this.Controls.Add(lblMin); this.Controls.Add(timingTrackBar);
            this.Controls.Add(lblMax); this.Controls.Add(timingValueLabel);

            // --- Startup ---
            lblStartupIcon = new Label { Text = "\uE7E8", Font = fontIcon, Location = new Point(marginX, 476), Size = new Size(24, 24), ForeColor = colorSec };
            lblStartupTxt = new Label { Font = fontBody, Location = new Point(56, 478), AutoSize = true };
            tglAutoStart = new FluentToggle { Location = new Point(toggleX, 478), Size = new Size(40, 20) };

            this.Controls.Add(lblStartupIcon); this.Controls.Add(lblStartupTxt); this.Controls.Add(tglAutoStart);

            // --- FOOTER ---
            statusLabel = new Label { Font = fontSecondary, ForeColor = colorSec, Location = new Point(marginX, 532), AutoSize = true };
            this.Controls.Add(statusLabel);

            this.ClientSize = new Size(460, 566);
        }

        /// <summary>
        /// Hooks up event handlers and initializes background logic such as auto-saving.
        /// </summary>
        private void SetupLogic()
        {
            _saveDebounceTimer = new Timer { Interval = 1000 };
            _saveDebounceTimer.Tick += async (s, e) =>
            {
                _saveDebounceTimer.Stop();
                _app.Settings.SaveSettings();
                foreach (var kvp in _app.SoundAnnouncer.LayoutSoundMap)
                    _app.Settings.SaveLayoutSound(kvp.Key, kvp.Value);
                await ShowSavedFeedback();
            };

            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("Settings", null, (s, e) => RestoreWindow());
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

            _trayIcon = new NotifyIcon { Icon = SystemIcons.Application, ContextMenuStrip = _trayMenu, Text = "QwertyShift", Visible = true };
            _trayIcon.DoubleClick += (s, e) => RestoreWindow();

            this.Resize += (s, e) => { if (this.WindowState == FormWindowState.Minimized) this.Hide(); };
            this.FormClosing += MainForm_FormClosing;

            SetupGridColumns();
            UpdateLocalization(); 

            LoadSoundsIntoSelector();
            PopulateGridData();
            LoadGeneralSettings();
            UpdateVoiceUIState();

            // EVENTS SUBSCRIPTIONS
            layoutGrid.SelectionChanged += LayoutGrid_SelectionChanged;
            layoutGrid.CellValueChanged += LayoutGrid_CellValueChanged;
            soundSelector.SelectedIndexChanged += SoundSelector_SelectedIndexChanged;

            languageSelector.SelectedIndexChanged += (s, e) =>
            {
                if (_isUpdatingUI || languageSelector.SelectedIndex < 0) return;

                _currentLang = _langCodes[languageSelector.SelectedIndex];                

                UpdateLocalization();
                
                _isUpdatingUI = true;
                string currentSoundId = (soundSelector.SelectedItem as SoundItem)?.Id;
                LoadSoundsIntoSelector();
                if (currentSoundId != null)
                {
                    soundSelector.SelectedItem = soundSelector.Items.OfType<SoundItem>().FirstOrDefault(i => i.Id == currentSoundId);
                }
                _isUpdatingUI = false;

                ScheduleSettingsSave();
            };

            tglUseVoice.CheckedChanged += (s, e) =>
            {
                _app.Settings.UseVoice = tglUseVoice.Checked;
                UpdateVoiceUIState();
                ScheduleSettingsSave();
            };

            timingTrackBar.ValueChanged += (s, e) =>
            {
                int seconds = timingTrackBar.Value;
                bool isRu = _currentLang == "ru";
                timingValueLabel.Text = $"{seconds} " + (isRu ? "сек" : "sec");
                _app.Settings.TypingPauseMs = seconds * 1000;
                _app.EventManager.TypingPauseMs = _app.Settings.TypingPauseMs;
                ScheduleSettingsSave();
            };

            tglAutoStart.CheckedChanged += (s, e) =>
            {
                if (!_app.StartupManager.SetAutorun(tglAutoStart.Checked))
                {
                    bool isRu = _currentLang == "ru";
                    MessageBox.Show(
                        isRu ? "Сбой изменения автозагрузки." : "Failed to change autostart settings.",
                        isRu ? "Ошибка" : "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    tglAutoStart.Checked = !tglAutoStart.Checked;
                }
                else ScheduleSettingsSave();
            };
        }

        // ==========================================
        // UI LOCALIZATION
        // ==========================================

        private readonly Dictionary<string, Dictionary<string, string>> _translations = new Dictionary<string, Dictionary<string, string>>
        {
            ["Settings"] = new Dictionary<string, string> {
        {"ru", "Настройки"}, {"en", "Settings"}, {"fr", "Paramètres"}, {"es", "Configuración"}, {"de", "Einstellungen"},
        {"it", "Impostazioni"}, {"pt", "Configurações"}, {"zh", "设置"}, {"ja", "設定"}, {"ar", "الإعدادات"}
    },
            ["RecognizedLayouts"] = new Dictionary<string, string> {
        {"ru", "Распознанные раскладки"}, {"en", "Recognized layouts"}, {"fr", "Dispositions reconnues"}, {"es", "Distribuciones reconocidas"}, {"de", "Erkannte Layouts"},
        {"it", "Layout riconosciuti"}, {"pt", "Layouts reconhecidos"}, {"zh", "已识别的布局"}, {"ja", "認識されたレイアウト"}, {"ar", "تخطيطات معترف بها"}
    },
            ["System"] = new Dictionary<string, string> {
        {"ru", "Система"}, {"en", "System"}, {"fr", "Système"}, {"es", "Sistema"}, {"de", "System"},
        {"it", "Sistema"}, {"pt", "Sistema"}, {"zh", "系统"}, {"ja", "システム"}, {"ar", "النظام"}
    },
            ["PronounceAs"] = new Dictionary<string, string> {
        {"ru", "Произносить как"}, {"en", "Pronounce as"}, {"fr", "Prononcer comme"}, {"es", "Pronunciar como"}, {"de", "Aussprechen als"},
        {"it", "Pronuncia come"}, {"pt", "Pronunciar como"}, {"zh", "发音为"}, {"ja", "発音"}, {"ar", "تنطق كـ"}
    },
            ["NotificationSettings"] = new Dictionary<string, string> {
        {"ru", "Параметры оповещения"}, {"en", "Notification settings"}, {"fr", "Paramètres de notification"}, {"es", "Ajustes de notificación"}, {"de", "Benachrichtigungseinstellungen"},
        {"it", "Impostazioni di notifica"}, {"pt", "Configurações de notificação"}, {"zh", "通知设置"}, {"ja", "通知設定"}, {"ar", "إعدادات الإشعارات"}
    },
            ["AnnounceNames"] = new Dictionary<string, string> {
        {"ru", "Озвучивать названия голосом"}, {"en", "Announce names via voice"}, {"fr", "Annoncer les noms vocalement"}, {"es", "Anunciar nombres por voz"}, {"de", "Namen per Sprache ansagen"},
        {"it", "Annuncia i nomi a voce"}, {"pt", "Anunciar nomes por voz"}, {"zh", "通过语音播报名称"}, {"ja", "音声で名前を読み上げる"}, {"ar", "نطق الأسماء بالصوت"}
    },
            ["SoundOnSwitch"] = new Dictionary<string, string> {
        {"ru", "Звук при переключении"}, {"en", "Sound on switch"}, {"fr", "Son lors du basculement"}, {"es", "Sonido al cambiar"}, {"de", "Ton beim Umschalten"},
        {"it", "Suono al cambio"}, {"pt", "Som ao alternar"}, {"zh", "切换时的声音"}, {"ja", "切り替え時の音"}, {"ar", "الصوت عند التبديل"}
    },
            ["PauseBeforeNext"] = new Dictionary<string, string> {
        {"ru", "Пауза перед новым оповещением"}, {"en", "Pause before next announcement"}, {"fr", "Pause avant la prochaine annonce"}, {"es", "Pausa antes del siguiente anuncio"}, {"de", "Pause vor der nächsten Ansage"},
        {"it", "Pausa prima del prossimo annuncio"}, {"pt", "Pausa antes do próximo anúncio"}, {"zh", "下一次播报前的暂停"}, {"ja", "次の読み上げまでの間隔"}, {"ar", "توقف قبل الإشعار التالي"}
    },
            ["ZeroSec"] = new Dictionary<string, string> {
        {"ru", "0 сек"}, {"en", "0 sec"}, {"fr", "0 s"}, {"es", "0 seg"}, {"de", "0 Sek"},
        {"it", "0 sec"}, {"pt", "0 seg"}, {"zh", "0 秒"}, {"ja", "0 秒"}, {"ar", "0 ثانية"}
    },
            ["TenSec"] = new Dictionary<string, string> {
        {"ru", "10 сек"}, {"en", "10 sec"}, {"fr", "10 s"}, {"es", "10 seg"}, {"de", "10 Sek"},
        {"it", "10 sec"}, {"pt", "10 seg"}, {"zh", "10 秒"}, {"ja", "10 秒"}, {"ar", "10 ثوان"}
    },
            ["Sec"] = new Dictionary<string, string> {
        {"ru", "сек"}, {"en", "sec"}, {"fr", "s"}, {"es", "seg"}, {"de", "Sek"},
        {"it", "sec"}, {"pt", "seg"}, {"zh", "秒"}, {"ja", "秒"}, {"ar", "ثانية"}
    },
            ["StartWithWindows"] = new Dictionary<string, string> {
        {"ru", "Запускать вместе с Windows"}, {"en", "Start with Windows"}, {"fr", "Démarrer avec Windows"}, {"es", "Iniciar con Windows"}, {"de", "Mit Windows starten"},
        {"it", "Avvia con Windows"}, {"pt", "Iniciar com o Windows"}, {"zh", "随 Windows 启动"}, {"ja", "Windows と一緒に起動"}, {"ar", "البدء مع ويندوز"}
    },
            ["AllSavedAuto"] = new Dictionary<string, string> {
        {"ru", "Все изменения сохраняются автоматически."}, {"en", "All changes are saved automatically."}, {"fr", "Toutes les modifications sont enregistrées automatiquement."}, {"es", "Todos los cambios se guardan automáticamente."}, {"de", "Alle Änderungen werden automatisch gespeichert."},
        {"it", "Tutte le modifiche vengono salvate automaticamente."}, {"pt", "Todas as alterações são salvas automaticamente."}, {"zh", "所有更改都会自动保存。"}, {"ja", "すべての変更は自動的に保存されます。"}, {"ar", "يتم حفظ جميع التغييرات تلقائيًا."}
    },
            ["SavedAuto"] = new Dictionary<string, string> {
        {"ru", "Сохранено автоматически."}, {"en", "Saved automatically."}, {"fr", "Enregistré automatiquement."}, {"es", "Guardado automáticamente."}, {"de", "Automatisch gespeichert."},
        {"it", "Salvato automaticamente."}, {"pt", "Salvo automaticamente."}, {"zh", "已自动保存。"}, {"ja", "自動的に保存されました。"}, {"ar", "تم الحفظ تلقائيًا."}
    },
            ["Exit"] = new Dictionary<string, string> {
        {"ru", "Выход"}, {"en", "Exit"}, {"fr", "Quitter"}, {"es", "Salir"}, {"de", "Beenden"},
        {"it", "Esci"}, {"pt", "Sair"}, {"zh", "退出"}, {"ja", "終了"}, {"ar", "خروج"}
    },
            ["Custom"] = new Dictionary<string, string> {
        {"ru", "[Свой]"}, {"en", "[Custom]"}, {"fr", "[Personnalisé]"}, {"es", "[Personalizado]"}, {"de", "[Benutzerdefiniert]"},
        {"it", "[Personalizzato]"}, {"pt", "[Personalizado]"}, {"zh", "[自定义]"}, {"ja", "[カスタム]"}, {"ar", "[مخصص]"}
    },
            ["AddCustom"] = new Dictionary<string, string> {
        {"ru", "+ Добавить свой..."}, {"en", "+ Add custom..."}, {"fr", "+ Ajouter..."}, {"es", "+ Añadir..."}, {"de", "+ Hinzufügen..."},
        {"it", "+ Aggiungi..."}, {"pt", "+ Adicionar..."}, {"zh", "+ 添加自定义..."}, {"ja", "+ カスタム追加..."}, {"ar", "+ إضافة مخصص..."}
    }
        };

        /// <summary>
        /// Retrieves the localized string for a specific key based on the currently selected language.
        /// </summary>
        private string GetLocalizedString(string key)
        {            
            if (_translations.TryGetValue(key, out var langDict))
            {
                if (langDict.TryGetValue(_currentLang, out var text))
                    return text;

                if (langDict.TryGetValue("en", out var fallback))
                    return fallback;
            }

            return key;
        }

        /// <summary>
        /// Updates all UI text elements with the correct translation for the current language.
        /// </summary>
        private void UpdateLocalization()
        {
            mainTitle.Text = GetLocalizedString("Settings");

            lblLayouts.Text = GetLocalizedString("RecognizedLayouts");
            if (layoutGrid.Columns.Count >= 3)
            {
                layoutGrid.Columns[1].HeaderText = GetLocalizedString("System");
                layoutGrid.Columns[2].HeaderText = GetLocalizedString("PronounceAs");
            }

            lblNotify.Text = GetLocalizedString("NotificationSettings");
            lblVoiceTxt.Text = GetLocalizedString("AnnounceNames");
            soundSelectLabel.Text = GetLocalizedString("SoundOnSwitch");

            lblPause.Text = GetLocalizedString("PauseBeforeNext");
            lblMin.Text = GetLocalizedString("ZeroSec");
            lblMax.Text = GetLocalizedString("TenSec");
            timingValueLabel.Text = $"{timingTrackBar.Value} {GetLocalizedString("Sec")}";

            lblStartupTxt.Text = GetLocalizedString("StartWithWindows");

            if (statusLabel.Text != GetLocalizedString("SavedAuto"))
            {
                statusLabel.Text = GetLocalizedString("AllSavedAuto");
            }

            if (_trayMenu != null && _trayMenu.Items.Count >= 3)
            {
                _trayMenu.Items[0].Text = GetLocalizedString("Settings");
                _trayMenu.Items[2].Text = GetLocalizedString("Exit");
            }
        }

        /// <summary>
        /// Adjusts the visibility and positioning of UI elements based on the voice toggle state.
        /// </summary>
        private void UpdateVoiceUIState()
        {
            bool useVoice = tglUseVoice.Checked;

            lblVoiceTxt.ForeColor = useVoice ? Color.FromArgb(26, 26, 26) : Color.FromArgb(140, 140, 140);
            soundSelectLabel.Visible = !useVoice;
            soundSelector.Visible = !useVoice;

            int yOffset = useVoice ? -36 : 0;

            lblPause.Top = 384 + yOffset;
            lblMin.Top = 420 + yOffset;
            timingTrackBar.Top = 416 + yOffset;
            lblMax.Top = 420 + yOffset;
            timingValueLabel.Top = 418 + yOffset;

            lblStartupIcon.Top = 476 + yOffset;
            lblStartupTxt.Top = 478 + yOffset;
            tglAutoStart.Top = 478 + yOffset;

            statusLabel.Top = 532 + yOffset;

            this.ClientSize = new Size(460, 566 + yOffset);
        }

        /// <summary>
        /// Temporarily changes the status label to indicate a successful save, then reverts back.
        /// </summary>
        private async Task ShowSavedFeedback()
        {
            bool isRu = _currentLang == "ru";
            statusLabel.Text = isRu ? "Сохранено автоматически." : "Saved automatically.";
            statusLabel.ForeColor = Color.FromArgb(93, 93, 93);

            await Task.Delay(3000);
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    statusLabel.Text = isRu ? "Все изменения сохраняются автоматически." : "All changes are saved automatically.";
                }));
            }
        }

        private void SetupGridColumns()
        {
            layoutGrid.Columns.Clear();
            layoutGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Handle", Visible = false });
            layoutGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SystemName", HeaderText = "Система", ReadOnly = true, FillWeight = 40 });

            var customCol = new DataGridViewTextBoxColumn { Name = "CustomName", HeaderText = "Произносить как", FillWeight = 60 };
            layoutGrid.Columns.Add(customCol);
        }

        private void LoadSoundsIntoSelector()
        {
            soundSelector.Items.Clear();
            bool isRu = _currentLang == "ru";
            string customPrefix = isRu ? "[Свой]" : "[Custom]";
            string addCustomText = isRu ? "+ Добавить свой..." : "+ Add custom...";

            var registeredSounds = _app.SoundAnnouncer.GetRegisteredSounds();
            foreach (var kvp in registeredSounds)
            {
                soundSelector.Items.Add(new SoundItem
                {
                    Id = kvp.Key,
                    DisplayName = kvp.Key.StartsWith("Cust_") ? $"{customPrefix} {Path.GetFileNameWithoutExtension(kvp.Value)}" : kvp.Key
                });
            }
            soundSelector.Items.Add(new SoundItem { Id = "ADD_CUSTOM", DisplayName = addCustomText });
        }

        private void PopulateGridData()
        {
            _isUpdatingUI = true;
            var registeredSounds = _app.SoundAnnouncer.GetRegisteredSounds();
            string[] soundKeys = registeredSounds.Keys.ToArray();
            int soundIndex = 0;

            foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
            {
                IntPtr hkl = (IntPtr)(lang.Handle.ToInt64() & 0xFFFFFFFF);
                var info = _app.LayoutDetector.GetLayoutInfo(hkl);
                layoutGrid.Rows.Add(hkl, info.SystemName, info.CustomName);

                string savedSound = _app.Settings.GetLayoutSound(hkl);
                _app.SoundAnnouncer.LayoutSoundMap[hkl] = (savedSound != null && registeredSounds.ContainsKey(savedSound)) ? savedSound : soundKeys[soundIndex % Math.Max(1, soundKeys.Length)];
                soundIndex++;
            }
            _isUpdatingUI = false;
        }

        private void LoadGeneralSettings()
        {
            tglUseVoice.Checked = _app.Settings.UseVoice;
            timingTrackBar.Value = Math.Max(0, Math.Min(10, (int)Math.Round(_app.Settings.TypingPauseMs / 1000.0)));

            bool isRu = _currentLang == "ru";
            timingValueLabel.Text = $"{timingTrackBar.Value} " + (isRu ? "сек" : "sec");

            tglAutoStart.Checked = _app.StartupManager.IsAutorunEnabled();
            UpdateVoiceUIState();
        }

        private void LayoutGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI || layoutGrid.SelectedRows.Count == 0) return;
            IntPtr selectedHkl = (IntPtr)layoutGrid.SelectedRows[0].Cells[0].Value;
            _isUpdatingUI = true;
            soundSelector.Enabled = true;
            if (_app.SoundAnnouncer.LayoutSoundMap.TryGetValue(selectedHkl, out string mappedId))
            {
                var itemToSelect = soundSelector.Items.OfType<SoundItem>().FirstOrDefault(i => i.Id == mappedId);
                if (itemToSelect != null) soundSelector.SelectedItem = itemToSelect;
            }
            _isUpdatingUI = false;
            TestAnnounce(selectedHkl);
        }

        private void LayoutGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isUpdatingUI || e.RowIndex < 0 || e.ColumnIndex != 2) return;
            IntPtr hkl = (IntPtr)layoutGrid.Rows[e.RowIndex].Cells[0].Value;
            string newName = layoutGrid.Rows[e.RowIndex].Cells[2].Value?.ToString() ?? "";
            _app.LayoutDetector.SetCustomName(hkl, newName);
            _app.Settings.SaveCustomName(hkl, newName);
            if (_app.Settings.UseVoice) TestAnnounce(hkl);
            ScheduleSettingsSave();
        }

        private void SoundSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI || soundSelector.SelectedItem == null || layoutGrid.SelectedRows.Count == 0) return;
            var chosenItem = (SoundItem)soundSelector.SelectedItem;
            IntPtr selectedHkl = (IntPtr)layoutGrid.SelectedRows[0].Cells[0].Value;
            bool isRu = _currentLang == "ru";

            if (chosenItem.Id == "ADD_CUSTOM")
            {
                using (var ofd = new OpenFileDialog
                {
                    Filter = "WAV (*.wav)|*.wav",
                    Title = isRu ? "Выберите звуковой файл" : "Select sound file"
                })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        string newId = "Cust_" + Guid.NewGuid().ToString("N");
                        _app.SoundAnnouncer.RegisterSound(newId, ofd.FileName);
                        _app.Settings.SaveCustomSoundPath(newId, ofd.FileName);

                        string customPrefix = isRu ? "[Свой]" : "[Custom]";
                        var newItem = new SoundItem { Id = newId, DisplayName = $"{customPrefix} {Path.GetFileNameWithoutExtension(ofd.FileName)}" };

                        soundSelector.Items.Insert(soundSelector.Items.Count - 1, newItem);
                        _isUpdatingUI = true; soundSelector.SelectedItem = newItem; _app.SoundAnnouncer.LayoutSoundMap[selectedHkl] = newId; _isUpdatingUI = false;
                        ScheduleSettingsSave(); TestAnnounce(selectedHkl);
                    }
                    else
                    {
                        _isUpdatingUI = true;
                        soundSelector.SelectedItem = soundSelector.Items.OfType<SoundItem>().FirstOrDefault(i => i.Id == _app.SoundAnnouncer.LayoutSoundMap[selectedHkl]);
                        _isUpdatingUI = false;
                    }
                }
            }
            else { _app.SoundAnnouncer.LayoutSoundMap[selectedHkl] = chosenItem.Id; ScheduleSettingsSave(); TestAnnounce(selectedHkl); }
        }

        private void TestAnnounce(IntPtr hkl)
        {
            var info = _app.LayoutDetector.GetLayoutInfo(hkl);
            if (_app.Settings.UseVoice) _app.SpeechAnnouncer.Announce(info); else _app.SoundAnnouncer.Announce(info);
        }

        private void ScheduleSettingsSave() { _saveDebounceTimer?.Stop(); _saveDebounceTimer?.Start(); }
        private void RestoreWindow()
        {
            _startHidden = false;
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }
        private void ExitApplication() { _allowExit = true; Application.Exit(); }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !_allowExit)
            {
                e.Cancel = true; this.Hide();
                if (_app.Settings.IsFirstHide)
                {
                    bool isRu = _currentLang == "ru";
                    _trayIcon?.ShowBalloonTip(3000, "QwertyShift", isRu ? "Работает в фоне." : "Running in background.", ToolTipIcon.Info);
                    _app.Settings.IsFirstHide = false;
                    ScheduleSettingsSave();
                }
                return;
            }
            _saveDebounceTimer?.Stop(); _app.Settings.SaveSettings(); _saveDebounceTimer?.Dispose();
            _trayIcon?.Dispose(); _trayMenu?.Dispose();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        private void GenerateAndSetMicroQIcon()
        {
            
            int[,] logo = {
                {1, 1, 1, 1, 1},
                {1, 0, 0, 0, 1},
                {1, 0, 0, 0, 1},
                {1, 0, 0, 2, 1},
                {1, 1, 1, 1, 2}
             };

            int scale = 3; 

            using (Bitmap bmp = new Bitmap(16, 16))
            using (Graphics g = Graphics.FromImage(bmp))
            {
               
                Color dark = Color.FromArgb(30, 30, 30); 
                Color white = Color.White;               
                Color blue = Color.DodgerBlue;           

                for (int y = 0; y < 5; y++)
                {
                    for (int x = 0; x < 5; x++)
                    {
                        Color c = Color.Transparent;
                        if (logo[y, x] == 1) c = dark;
                        else if (logo[y, x] == 0) c = white;
                        else if (logo[y, x] == 2) c = blue;

                        if (c != Color.Transparent)
                        {
                            using (SolidBrush brush = new SolidBrush(c))
                            {
                                g.FillRectangle(brush, x * scale, y * scale, scale, scale);
                            }
                        }
                    }
                }

                IntPtr hIcon = bmp.GetHicon();

                Icon generatedIcon = (Icon)Icon.FromHandle(hIcon).Clone();

                this.Icon = generatedIcon;

                if (_trayIcon != null)
                {
                    _trayIcon.Icon = generatedIcon;
                }

                DestroyIcon(hIcon);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }

    // ==========================================
    // CUSTOM FLUENT DESIGN ELEMENTS
    // ==========================================

    /// <summary>
    /// A custom panel control styled to match Windows 11 Fluent Design surfaces.
    /// </summary>
    public class FluentSurface : Panel
    {
        public FluentSurface() { DoubleBuffered = true; BackColor = Color.White; }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent.BackColor);

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = GetRoundRectangle(rect, 8))
            {
                using (SolidBrush brush = new SolidBrush(BackColor))
                    e.Graphics.FillPath(brush, path);

                using (Pen pen = new Pen(Color.FromArgb(229, 229, 229), 1))
                    e.Graphics.DrawPath(pen, path);
            }
        }

        private GraphicsPath GetRoundRectangle(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    /// <summary>
    /// A custom toggle switch control styled to match Windows 11 Fluent Design toggles.
    /// </summary>
    public class FluentToggle : Control
    {
        public bool Checked { get; set; }
        public event EventHandler CheckedChanged;

        public FluentToggle() { DoubleBuffered = true; Cursor = Cursors.Hand; Size = new Size(40, 20); }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            CheckedChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent.BackColor);

            Color onColor = Color.FromArgb(0, 95, 184);
            Color offColor = Color.FromArgb(200, 200, 200);

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = GetRoundRectangle(rect, Height / 2))
            using (SolidBrush brush = new SolidBrush(Checked ? onColor : offColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            int circleSize = Height - 6;
            int circleX = Checked ? Width - circleSize - 3 : 3;
            using (SolidBrush circleBrush = new SolidBrush(Color.White))
            {
                e.Graphics.FillEllipse(circleBrush, circleX, 3, circleSize, circleSize);
            }
        }

        private GraphicsPath GetRoundRectangle(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    };
   
    }