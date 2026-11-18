using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using MediaBrush = System.Windows.Media.Brush;

namespace ImeHalfWidth;

public partial class MainWindow : Window
{
    private readonly ImeLockService _ime = new();
    private readonly DispatcherTimer _timer;
    private readonly AppSettings _settings;
    private readonly Forms.NotifyIcon _tray;
    private readonly Forms.ToolStripMenuItem _statusMenu;
    private readonly Forms.ToolStripMenuItem _enableMenu;
    private readonly Forms.ToolStripMenuItem _halfWidthMenu;
    private readonly Forms.ToolStripMenuItem _fullWidthMenu;
    private readonly Forms.ToolStripMenuItem _englishPunctMenu;
    private readonly Forms.ToolStripMenuItem _chinesePunctMenu;
    private readonly Forms.ToolStripMenuItem _autoStartMenu;
    private bool _ready;
    private bool _syncing;
    private bool _forceClose;

    public MainWindow()
    {
        InitializeComponent();
        _settings = AppSettings.Load();

        _statusMenu = new Forms.ToolStripMenuItem
        {
            Enabled = false,
            Font = new Drawing.Font("Microsoft YaHei UI", 10f, Drawing.FontStyle.Bold)
        };
        _enableMenu = CheckItem("启用锁定", (_, _) => ApplyFromTray(announce: true));
        _halfWidthMenu = ActionItem("半角", (_, _) => SetWidth(true));
        _fullWidthMenu = ActionItem("全角", (_, _) => SetWidth(false));
        _englishPunctMenu = ActionItem("英文标点", (_, _) => SetPunct(true));
        _chinesePunctMenu = ActionItem("中文标点", (_, _) => SetPunct(false));
        _autoStartMenu = CheckItem("开机启动", (_, _) =>
        {
            ImeLockService.SetAutoStart(_autoStartMenu.Checked);
            AutoStartBox.IsChecked = _autoStartMenu.Checked;
        });

        _tray = CreateTray();
        ApplySettingsToUi();

        EnableBox.Checked += OnSettingChanged;
        EnableBox.Unchecked += OnSettingChanged;
        HalfWidthBox.Checked += OnSettingChanged;
        HalfWidthBox.Unchecked += OnSettingChanged;
        EnglishPunctBox.Checked += OnSettingChanged;
        EnglishPunctBox.Unchecked += OnSettingChanged;
        AutoStartBox.Checked += OnAutoStart;
        AutoStartBox.Unchecked += OnAutoStart;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ImeLockService.PollMs) };
        _timer.Tick += (_, _) => RefreshStatus();
        Loaded += (_, _) =>
        {
            _ready = true;
            _timer.Start();
            RefreshStatus();
        };
        Closed += (_, _) =>
        {
            _timer.Stop();
            _tray.Visible = false;
            _tray.Dispose();
        };
    }

    private Forms.NotifyIcon CreateTray()
    {
        var menu = TrayMenu.Create();
        menu.Items.Add(_statusMenu);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("打开主窗口", null, (_, _) => RestoreFromTray());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_enableMenu);
        menu.Items.Add(_halfWidthMenu);
        menu.Items.Add(_fullWidthMenu);
        menu.Items.Add(_englishPunctMenu);
        menu.Items.Add(_chinesePunctMenu);
        menu.Items.Add(_autoStartMenu);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitFromTray());
        menu.Opening += (_, _) => SyncTrayFromSettings();

        var tray = new Forms.NotifyIcon
        {
            Visible = true,
            ContextMenuStrip = menu,
            Icon = LoadTrayIcon()
        };
        tray.DoubleClick += (_, _) => RestoreFromTray();
        return tray;
    }

    private static Forms.ToolStripMenuItem CheckItem(string text, EventHandler onClick)
    {
        var item = new Forms.ToolStripMenuItem(text) { CheckOnClick = true };
        item.Click += onClick;
        return item;
    }

    private static Forms.ToolStripMenuItem ActionItem(string text, EventHandler onClick)
    {
        var item = new Forms.ToolStripMenuItem(text);
        item.Click += onClick;
        return item;
    }

    private static Drawing.Icon LoadTrayIcon()
    {
        var stream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/App.ico"))?.Stream;
        return stream is not null ? new Drawing.Icon(stream) : SystemIcons.Application;
    }

    private void OnSettingChanged(object sender, RoutedEventArgs e)
    {
        if (!_ready || _syncing)
            return;
        SaveFromUi();
        SyncTrayFromSettings();
        RefreshStatus();
    }

    private void OnAutoStart(object sender, RoutedEventArgs e)
    {
        if (!_ready || _syncing)
            return;
        ImeLockService.SetAutoStart(AutoStartBox.IsChecked == true);
        _autoStartMenu.Checked = AutoStartBox.IsChecked == true;
    }

    private void SetWidth(bool halfWidth)
    {
        _settings.HalfWidth = halfWidth;
        _settings.Save();
        ApplySettingsToUi();
        RefreshStatus();
        Announce();
    }

    private void SetPunct(bool english)
    {
        _settings.EnglishPunctuation = english;
        _settings.Save();
        ApplySettingsToUi();
        RefreshStatus();
        Announce();
    }

    private void ApplyFromTray(bool announce)
    {
        _settings.Enabled = _enableMenu.Checked;
        _settings.Save();
        ApplySettingsToUi();
        RefreshStatus();
        if (announce)
            Announce();
    }

    private void ApplySettingsToUi()
    {
        _syncing = true;
        EnableBox.IsChecked = _settings.Enabled;
        HalfWidthBox.IsChecked = _settings.HalfWidth;
        EnglishPunctBox.IsChecked = _settings.EnglishPunctuation;
        AutoStartBox.IsChecked = ImeLockService.IsAutoStartEnabled();
        SyncTrayFromSettings();
        _syncing = false;
    }

    private void SyncTrayFromSettings()
    {
        _enableMenu.Checked = _settings.Enabled;
        _halfWidthMenu.Checked = _settings.HalfWidth;
        _fullWidthMenu.Checked = !_settings.HalfWidth;
        _englishPunctMenu.Checked = _settings.EnglishPunctuation;
        _chinesePunctMenu.Checked = !_settings.EnglishPunctuation;
        _autoStartMenu.Checked = AutoStartBox.IsChecked == true;
        _statusMenu.Text = StatusLine();
    }

    private string StatusLine()
    {
        var run = _settings.Enabled ? "锁定中" : "已暂停";
        var width = _settings.HalfWidth ? "半角" : "全角";
        var punct = _settings.EnglishPunctuation ? "英文标点" : "中文标点";
        return $"当前：{run} · {width} · {punct}";
    }

    private void Announce()
    {
        _tray.BalloonTipTitle = "半角输入锁定";
        _tray.BalloonTipText = StatusLine();
        _tray.BalloonTipIcon = Forms.ToolTipIcon.Info;
        _tray.ShowBalloonTip(1800);
    }

    private void SaveFromUi()
    {
        _settings.Enabled = EnableBox.IsChecked == true;
        _settings.HalfWidth = HalfWidthBox.IsChecked == true;
        _settings.EnglishPunctuation = EnglishPunctBox.IsChecked == true;
        _settings.Save();
    }

    private void RefreshStatus()
    {
        if (!_ready)
            return;

        if (!_syncing)
            SaveFromUi();

        var status = _ime.Apply(_settings);
        var ok = (MediaBrush)FindResource("OkBrush");
        var muted = (MediaBrush)FindResource("MutedBrush");
        var accent = (MediaBrush)FindResource("AccentBrush");
        var line = StatusLine();

        StatusRunText.Text = _settings.Enabled ? "锁定中" : "已暂停";
        StatusRunText.Foreground = _settings.Enabled ? ok : muted;
        StatusWidthText.Text = "宽度：" + (_settings.HalfWidth ? "半角" : "全角");
        StatusWidthText.Foreground = accent;
        StatusPunctText.Text = "标点：" + (_settings.EnglishPunctuation ? "英文标点" : "中文标点");
        StatusPunctText.Foreground = accent;
        StatusTimeText.Text = status.UpdatedAt.ToString("HH:mm:ss");
        _statusMenu.Text = line;
        _tray.Text = line.Length <= 63 ? line : line[..63];
    }

    private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_forceClose)
            return;

        e.Cancel = true;
        var prompt = new ClosePromptWindow { Owner = this };
        prompt.ShowDialog();
        if (prompt.Choice == CloseChoice.Tray)
            Hide();
        else if (prompt.Choice == CloseChoice.Exit)
            ExitFromTray();
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitFromTray()
    {
        _forceClose = true;
        _tray.Visible = false;
        System.Windows.Application.Current.Shutdown();
    }
}
