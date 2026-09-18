using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace Mfr.App.Ui.Views.Controls
{
    /// <summary>
    /// Shared catalog search field with an explicit clear (✕) control.
    /// <para>
    /// Used by the format-token picker and Rename List field shuttle (Columns + Sort).
    /// Hosts bind <see cref="Text"/>, <see cref="ClearCommand"/>, and <see cref="IsClearVisible"/>.
    /// </para>
    /// </summary>
    public partial class CatalogSearchBox : UserControl
    {
        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<
            CatalogSearchBox,
            string
        >(nameof(Text), defaultValue: string.Empty, defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="PlaceholderText"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> PlaceholderTextProperty = AvaloniaProperty.Register<
            CatalogSearchBox,
            string?
        >(nameof(PlaceholderText));

        /// <summary>
        /// Defines the <see cref="ClearCommand"/> property.
        /// </summary>
        public static readonly StyledProperty<ICommand?> ClearCommandProperty = AvaloniaProperty.Register<
            CatalogSearchBox,
            ICommand?
        >(nameof(ClearCommand));

        /// <summary>
        /// Defines the <see cref="IsClearVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsClearVisibleProperty = AvaloniaProperty.Register<
            CatalogSearchBox,
            bool
        >(nameof(IsClearVisible));

        /// <summary>
        /// Initializes the catalog search control.
        /// </summary>
        public CatalogSearchBox()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Bound search text (two-way).
        /// </summary>
        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Watermark shown when <see cref="Text"/> is empty.
        /// </summary>
        public string? PlaceholderText
        {
            get => GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        /// <summary>
        /// Clears the search when the ✕ is clicked.
        /// </summary>
        public ICommand? ClearCommand
        {
            get => GetValue(ClearCommandProperty);
            set => SetValue(ClearCommandProperty, value);
        }

        /// <summary>
        /// Whether the clear (✕) button is visible.
        /// </summary>
        public bool IsClearVisible
        {
            get => GetValue(IsClearVisibleProperty);
            set => SetValue(IsClearVisibleProperty, value);
        }

        /// <summary>
        /// Inner text box for focus, key handlers, and headless tests.
        /// </summary>
        public TextBox Input => SearchInput;

        /// <summary>
        /// Clear button for headless tests.
        /// </summary>
        public Button Clear => ClearButton;

        /// <summary>
        /// Moves keyboard focus into the search input.
        /// </summary>
        public void FocusInput()
        {
            SearchInput.Focus();
        }
    }
}
