using Avalonia;
using Avalonia.Controls;

namespace Mfr.App.Ui.Views.Controls
{
    /// <summary>
    /// Literal / Wildcard / Regex mode radios for Replacer and Replace List editors.
    /// </summary>
    public partial class ReplacerModeFieldset : UserControl
    {
        /// <summary>
        /// Defines the <see cref="GroupName"/> property.
        /// </summary>
        public static readonly StyledProperty<string> GroupNameProperty = AvaloniaProperty.Register<
            ReplacerModeFieldset,
            string
        >(nameof(GroupName), defaultValue: "ReplacerMode");

        /// <summary>
        /// Initializes the mode fieldset and applies <see cref="GroupName"/> to the radios.
        /// </summary>
        public ReplacerModeFieldset()
        {
            InitializeComponent();
            _ApplyGroupName();
        }

        /// <summary>
        /// Gets or sets the radio <see cref="RadioButton.GroupName"/> shared by the three modes.
        /// </summary>
        public string GroupName
        {
            get => GetValue(GroupNameProperty);
            set => SetValue(GroupNameProperty, value);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == GroupNameProperty)
            {
                _ApplyGroupName();
            }
        }

        /// <summary>
        /// Copies <see cref="GroupName"/> onto each mode radio.
        /// </summary>
        private void _ApplyGroupName()
        {
            var groupName = GroupName;
            LiteralRadio.GroupName = groupName;
            WildcardRadio.GroupName = groupName;
            RegexRadio.GroupName = groupName;
        }
    }
}
