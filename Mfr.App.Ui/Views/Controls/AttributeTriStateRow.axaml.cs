using Avalonia;
using Avalonia.Controls;
using Mfr.Filters.Attributes;

namespace Mfr.App.Ui.Views.Controls
{
    /// <summary>
    /// Labeled On / Off / Keep radio row bound to an <see cref="AttributeTriState"/> value.
    /// </summary>
    public partial class AttributeTriStateRow : UserControl
    {
        /// <summary>
        /// Defines the <see cref="Label"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<
            AttributeTriStateRow,
            string?
        >(nameof(Label));

        /// <summary>
        /// Defines the <see cref="GroupName"/> property.
        /// </summary>
        public static readonly StyledProperty<string> GroupNameProperty = AvaloniaProperty.Register<
            AttributeTriStateRow,
            string
        >(nameof(GroupName), defaultValue: "AttributeTriState");

        /// <summary>
        /// Defines the <see cref="Value"/> property.
        /// </summary>
        public static readonly StyledProperty<AttributeTriState> ValueProperty = AvaloniaProperty.Register<
            AttributeTriStateRow,
            AttributeTriState
        >(nameof(Value), defaultValue: AttributeTriState.Keep);

        /// <summary>
        /// Initializes the row and applies <see cref="GroupName"/> to the radios.
        /// </summary>
        public AttributeTriStateRow()
        {
            InitializeComponent();
            _ApplyGroupName();
        }

        /// <summary>
        /// Gets or sets the row label text (for example <c>Read-only:</c>).
        /// </summary>
        public string? Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        /// <summary>
        /// Gets or sets the radio <see cref="RadioButton.GroupName"/> shared by On / Off / Keep.
        /// </summary>
        public string GroupName
        {
            get => GetValue(GroupNameProperty);
            set => SetValue(GroupNameProperty, value);
        }

        /// <summary>
        /// Gets or sets the On / Off / Keep selection.
        /// </summary>
        public AttributeTriState Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
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
        /// Copies <see cref="GroupName"/> onto each tri-state radio.
        /// </summary>
        private void _ApplyGroupName()
        {
            var groupName = GroupName;
            OnRadio.GroupName = groupName;
            OffRadio.GroupName = groupName;
            KeepRadio.GroupName = groupName;
        }
    }
}
