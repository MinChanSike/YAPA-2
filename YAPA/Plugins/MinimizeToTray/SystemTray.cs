using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Plugins.MinimizeToTray
{
    public class SystemTrayPlugin : IPluginMeta
    {
        public string Title => "System tray";
        public string Id => "SystemTray";

        public Type Plugin => typeof(SystemTray);

        public Type Settings => typeof(SystemTraySettings);

        public Type SettingEditWindow => typeof(MinimizeToTraySettingWindow);
    }

    public class SystemTray : IPlugin
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern bool DestroyIcon(IntPtr handle);

        private readonly System.Windows.Forms.NotifyIcon _sysTrayIcon;
        private IntPtr _systemTrayIcon;


        private readonly IApplication _app;
        private readonly IMainViewModel _viewModel;
        private readonly SystemTraySettings _settings;
        private readonly ISettings _globalSettings;
        private readonly PomodoroEngineSettings _engineSettings;

        public SystemTray(IApplication app, IMainViewModel viewModel, SystemTraySettings settings, ISettings globalSettings, PomodoroEngineSettings engineSettings)
        {
            _app = app;
            _viewModel = viewModel;
            _settings = settings;
            _globalSettings = globalSettings;
            _engineSettings = engineSettings;

            _globalSettings.PropertyChanged += _globalSettings_PropertyChanged;

            _app.StateChanged += _app_StateChanged;

            _sysTrayIcon = new System.Windows.Forms.NotifyIcon
            {
                Text = @"YAPA 2",
                Icon = new System.Drawing.Icon(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\pomoTray.ico"), 40, 40),
                Visible = false
            };
            _sysTrayIcon.DoubleClick += SysTrayIcon_DoubleClick;

            _viewModel.Engine.PropertyChanged += _engine_PropertyChanged;

            _sysTrayIcon.ContextMenu = new System.Windows.Forms.ContextMenu(CreateNotifyIconContextMenu());
        }

        private void _globalSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == $"{nameof(SystemTray)}.{nameof(_settings.ShowInTaskbar)}")
            {
                _app.ShowInTaskbar = _settings.ShowInTaskbar;
            }
        }

        private void _engine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.Engine.Elapsed))
            {
                UpdateIcon();
            }
        }

        private System.Windows.Forms.MenuItem[] CreateNotifyIconContextMenu()
        {
            var startTask = new System.Windows.Forms.MenuItem { Text = @"Start" };
            startTask.Click += (o, s) =>
            {
                if (_viewModel.StartCommand.CanExecute(null))
                {
                    _viewModel.StartCommand.Execute(null);
                }
            };

            var stopTask = new System.Windows.Forms.MenuItem { Text = @"Stop" };
            stopTask.Click += (o, s) =>
            {
                if (_viewModel.ResetCommand.CanExecute(null))
                {
                    _viewModel.ResetCommand.Execute(null);
                }
            };

            var resetTask = new System.Windows.Forms.MenuItem { Text = @"Reset session" };
            resetTask.Click += (o, s) =>
            {
                if (_viewModel.ResetCommand.CanExecute(null))
                {
                    _viewModel.ResetCommand.Execute(null);
                }
            };

            var settings = new System.Windows.Forms.MenuItem { Text = @"Settings" };
            settings.Click += (o, s) =>
            {
                if (_viewModel.ShowSettingsCommand.CanExecute(null))
                {
                    _viewModel.ShowSettingsCommand.Execute(null);
                }
            };

            var close = new System.Windows.Forms.MenuItem { Text = @"Exit" };
            close.Click += (o, s) =>
            {
                _app.CloseApp();
            };

            return new[]
            {
                startTask,stopTask, resetTask, settings,close
            };
        }

        private void _app_StateChanged(ApplicationState state)
        {

            if (state == ApplicationState.Minimized && (_settings.MinimizeToTray || _settings.ShowInTaskbar == false))
            {
                _app.Hide();
                _sysTrayIcon.Visible = true;
            }

            if (state != ApplicationState.Minimized)
            {
                _app.ShowInTaskbar = _settings.ShowInTaskbar;
            }
        }

        private void SysTrayIcon_DoubleClick(object sender, EventArgs e)
        {
            _app.Show();
            _app.AppState = ApplicationState.Normal;
            _sysTrayIcon.Visible = false;
        }

        //http://blogs.msdn.com/b/abhinaba/archive/2005/09/12/animation-and-text-in-system-tray-using-c.aspx
        private void UpdateIcon()
        {
            System.Windows.Media.Color textColor;

            if (_viewModel.Engine.Phase == PomodoroPhase.Break || _viewModel.Engine.Phase == PomodoroPhase.BreakEnded)
            {
                textColor = _settings.BreakTrayIconColor;
            }
            else
            {
                textColor = _settings.WorkTrayIconColor;
            }

            if (_systemTrayIcon != IntPtr.Zero)
            {
                DestroyIcon(_systemTrayIcon);
            }

            var displayText = _viewModel.Engine.DisplayValue / 60;

            System.Drawing.Brush brush = new SolidBrush(System.Drawing.Color.FromArgb(textColor.A, textColor.R, textColor.G, textColor.B));

            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(16, 16);
            System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap);
            graphics.DrawString(displayText.ToString(), new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Bold), brush, 0, 0);

            _systemTrayIcon = bitmap.GetHicon();

            System.Drawing.Icon icon = System.Drawing.Icon.FromHandle(_systemTrayIcon);
            _sysTrayIcon.Icon = icon;
        }
    }

    public class SystemTraySettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public System.Windows.Media.Color WorkTrayIconColor
        {
            get { return _settings.Get(nameof(WorkTrayIconColor), System.Windows.Media.Colors.DarkGreen); }
            set { _settings.Update(nameof(WorkTrayIconColor), value); }
        }

        public System.Windows.Media.Color BreakTrayIconColor
        {
            get { return _settings.Get(nameof(BreakTrayIconColor), System.Windows.Media.Colors.DarkRed); }
            set { _settings.Update(nameof(BreakTrayIconColor), value); }
        }

        public bool ShowInTaskbar
        {
            get { return _settings.Get(nameof(ShowInTaskbar), true); }
            set { _settings.Update(nameof(ShowInTaskbar), value); }
        }

        public bool MinimizeToTray
        {
            get { return _settings.Get(nameof(MinimizeToTray), false); }
            set { _settings.Update(nameof(MinimizeToTray), value); }
        }

        public SystemTraySettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(new SystemTrayPlugin().Id);
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }
}