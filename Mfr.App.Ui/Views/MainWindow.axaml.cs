using Avalonia;
using Avalonia.Controls;
using Mfr.App.Ui.Services;
using Mfr.App.Ui.ViewModels;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Main application window with the MFR 7.4 splitter layout.
    /// </summary>
    public partial class MainWindow : Window
    {
        private int _normalX;
        private int _normalY;
        private double _normalWidth;
        private double _normalHeight;
        private bool _hasNormalBounds;

        /// <summary>
        /// Initializes the main window.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Opened += _OnOpened;
            Closing += _OnClosing;
            PropertyChanged += _OnPropertyChanged;
            PositionChanged += _OnPositionChanged;
        }

        private void _OnOpened(object? sender, EventArgs e)
        {
            _CaptureNormalBoundsIfApplicable();
        }

        private void _OnClosing(object? sender, WindowClosingEventArgs e)
        {
            _CaptureNormalBoundsIfApplicable();
            UiSessionPersistence.SaveOnClose(
                this,
                DataContext as MainWindowViewModel,
                _hasNormalBounds,
                _normalX,
                _normalY,
                _normalWidth,
                _normalHeight);
        }

        private void _OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == WindowStateProperty
                || e.Property == WidthProperty
                || e.Property == HeightProperty)
                _CaptureNormalBoundsIfApplicable();
        }

        private void _OnPositionChanged(object? sender, PixelPointEventArgs e)
        {
            _CaptureNormalBoundsIfApplicable();
        }

        private void _CaptureNormalBoundsIfApplicable()
        {
            if (WindowState != WindowState.Normal)
                return;

            _normalX = Position.X;
            _normalY = Position.Y;
            _normalWidth = Width;
            _normalHeight = Height;
            _hasNormalBounds = true;
        }
    }
}
