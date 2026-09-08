using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Opens a dialog height-to-content, then locks height so only width remains resizable.
    /// </summary>
    internal static class ModalDialogHorizontalResize
    {
        private static readonly ConditionalWeakTable<Window, object> s_attached = [];
        private static readonly ConditionalWeakTable<Window, object> s_relock = [];

        /// <summary>
        /// Attaches a one-shot open handler that measures content and locks height.
        /// </summary>
        /// <param name="window">Modal dialog window.</param>
        public static void Attach(Window window)
        {
            ArgumentNullException.ThrowIfNull(window);
            if (s_attached.TryGetValue(window, out _))
            {
                return;
            }

            s_attached.Add(window, string.Empty);

            window.Opened += _OnOpened;

            void _OnOpened(object? sender, EventArgs e)
            {
                window.Opened -= _OnOpened;
                // Child controls (e.g. FormatEditor auto-grow) need a laid-out width first.
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        window.UpdateLayout();
                        Dispatcher.UIThread.Post(() => LockHeightToContent(window), DispatcherPriority.Render);
                    },
                    DispatcherPriority.Loaded
                );
            }
        }

        /// <summary>
        /// Relocks height when <paramref name="window"/>'s <see cref="StyledElement.DataContext"/> raises
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> for one of <paramref name="propertyNames"/>.
        /// </summary>
        /// <param name="window">Dialog already using <see cref="Attach"/>.</param>
        /// <param name="propertyNames">View-model properties that change content height.</param>
        public static void RelockOnDataContextProperties(Window window, params string[] propertyNames)
        {
            ArgumentNullException.ThrowIfNull(window);
            ArgumentNullException.ThrowIfNull(propertyNames);
            if (propertyNames.Length == 0)
            {
                throw new ArgumentException("At least one property name is required.", nameof(propertyNames));
            }

            if (s_relock.TryGetValue(window, out _))
            {
                return;
            }

            s_relock.Add(window, string.Empty);

            var propertyNameToWatch = propertyNames.ToHashSet(StringComparer.Ordinal);
            INotifyPropertyChanged? source = null;

            void _OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName is null || !propertyNameToWatch.Contains(e.PropertyName))
                {
                    return;
                }

                Dispatcher.UIThread.Post(
                    () =>
                    {
                        window.UpdateLayout();
                        LockHeightToContent(window);
                    },
                    DispatcherPriority.Loaded
                );
            }

            void _BindSource()
            {
                source?.PropertyChanged -= _OnPropertyChanged;
                source = window.DataContext as INotifyPropertyChanged;
                source?.PropertyChanged += _OnPropertyChanged;
            }

            window.DataContextChanged += (_, _) => _BindSource();
            _BindSource();
        }

        /// <summary>
        /// Remeasures <paramref name="window"/> content and locks min/max height to that value
        /// (horizontal resize only).
        /// </summary>
        /// <param name="window">Dialog to resize-lock.</param>
        public static void LockHeightToContent(Window window)
        {
            ArgumentNullException.ThrowIfNull(window);
            if (window.Content is not Control root)
            {
                return;
            }

            var width = window.ClientSize.Width;
            if (width <= 0)
            {
                width = window.Bounds.Width;
            }

            if (width <= 0)
            {
                return;
            }

            root.InvalidateMeasure();
            root.Measure(new Size(width, double.PositiveInfinity));
            var contentHeight = root.DesiredSize.Height;
            if (contentHeight <= 0)
            {
                return;
            }

            var frameChrome = Math.Max(0, window.Bounds.Height - window.ClientSize.Height);
            var height = contentHeight + frameChrome;
            if (
                Math.Abs(window.Height - height) < 0.5
                && Math.Abs(window.MinHeight - height) < 0.5
                && Math.Abs(window.MaxHeight - height) < 0.5
                && window.SizeToContent == SizeToContent.Manual
            )
            {
                return;
            }

            window.SizeToContent = SizeToContent.Manual;
            window.Height = height;
            window.MinHeight = height;
            window.MaxHeight = height;
        }
    }
}
