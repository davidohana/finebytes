using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Misc;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Misc
{
    /// <summary>
    /// Filter Configuration editor for <see cref="StripParenthesesFilter"/>.
    /// </summary>
    internal sealed partial class StripParenthesesFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        private bool _isLoading;

        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public StripParenthesesFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the bracket pair type to strip.
        /// </summary>
        [ObservableProperty]
        private ParenthesisType _type = ParenthesisType.Round;

        /// <summary>
        /// Gets or sets whether bracketed contents are removed with the delimiters.
        /// </summary>
        [ObservableProperty]
        private bool _removeContents = true;

        /// <summary>
        /// Gets or sets whether round parentheses are selected.
        /// </summary>
        public bool IsTypeRound
        {
            get => Type == ParenthesisType.Round;
            set
            {
                if (value)
                {
                    Type = ParenthesisType.Round;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether square brackets are selected.
        /// </summary>
        public bool IsTypeSquare
        {
            get => Type == ParenthesisType.Square;
            set
            {
                if (value)
                {
                    Type = ParenthesisType.Square;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether curly braces are selected.
        /// </summary>
        public bool IsTypeCurly
        {
            get => Type == ParenthesisType.Curly;
            set
            {
                if (value)
                {
                    Type = ParenthesisType.Curly;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether angle brackets are selected.
        /// </summary>
        public bool IsTypeAngle
        {
            get => Type == ParenthesisType.Angle;
            set
            {
                if (value)
                {
                    Type = ParenthesisType.Angle;
                }
            }
        }

        partial void OnTypeChanged(ParenthesisType value)
        {
            OnPropertyChanged(nameof(IsTypeRound));
            OnPropertyChanged(nameof(IsTypeSquare));
            OnPropertyChanged(nameof(IsTypeCurly));
            OnPropertyChanged(nameof(IsTypeAngle));
            _ApplyOptions();
        }

        partial void OnRemoveContentsChanged(bool value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not StripParenthesesFilter filter)
            {
                return;
            }

            _isLoading = true;
            try
            {
                Type = filter.Options.Type;
                RemoveContents = filter.Options.RemoveContents;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void _ApplyOptions()
        {
            if (_isLoading || Step.Filter is not StripParenthesesFilter filter)
            {
                return;
            }

            var options = new StripParenthesesOptions(Type: Type, RemoveContents: RemoveContents);
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
