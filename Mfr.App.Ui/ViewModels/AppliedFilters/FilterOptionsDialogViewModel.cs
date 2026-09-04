using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Filters;
using Mfr.Models.Filters;
using Mfr.Models.Tags.Id3v2;

namespace Mfr.App.Ui.ViewModels.AppliedFilters
{
    /// <summary>
    /// Draft state for the Filter Options dialog (name, Apply-To targets, and apply scope).
    /// </summary>
    public sealed partial class FilterOptionsDialogViewModel : ViewModelBase
    {
        private bool _isLoadingTarget;

        /// <summary>
        /// Initializes draft fields from the selected applied-filter step.
        /// </summary>
        /// <param name="step">Applied list row being edited.</param>
        public FilterOptionsDialogViewModel(AppliedFilterStepViewModel step)
        {
            ArgumentNullException.ThrowIfNull(step);

            Name = step.DisplayName;
            HasApplyTo = step.Filter is StringTargetFilter;
            if (step.Filter is StringTargetFilter stringFilter)
            {
                _LoadFromStringFilter(stringFilter);
            }
        }

        /// <summary>
        /// Gets Apply-To groups shown in Filter Options.
        /// </summary>
        public IReadOnlyList<FilterTargetGroupOption> TargetGroups => FilterTargetCatalog.Groups;

        /// <summary>
        /// Gets anchor choices for substring scope endpoints.
        /// </summary>
        public IReadOnlyList<StringScopeAnchorOption> ScopeAnchorOptions => StringScopeAnchorOption.All;

        /// <summary>
        /// Gets or sets the substring start anchor choice.
        /// </summary>
        [ObservableProperty]
        private StringScopeAnchorOption _substringStartAnchorOption = StringScopeAnchorOption.All[0];

        /// <summary>
        /// Gets or sets the substring end anchor choice.
        /// </summary>
        [ObservableProperty]
        private StringScopeAnchorOption _substringEndAnchorOption = StringScopeAnchorOption.All[0];

        /// <summary>
        /// Gets or sets the ancestor-folder level (1 = parent folder).
        /// </summary>
        [ObservableProperty]
        private decimal _ancestorFolderLevel = 1;

        /// <summary>
        /// Gets or sets the filter instance name shown in the Applied list.
        /// </summary>
        [ObservableProperty]
        private string _name = string.Empty;

        /// <summary>
        /// Gets whether Apply-To editing is available for the selected filter.
        /// </summary>
        [ObservableProperty]
        private bool _hasApplyTo;

        /// <summary>
        /// Gets or sets the selected Apply-To group.
        /// </summary>
        [ObservableProperty]
        private FilterTargetGroupOption? _selectedTargetGroup;

        /// <summary>
        /// Gets targets for <see cref="SelectedTargetGroup"/>.
        /// </summary>
        [ObservableProperty]
        private IReadOnlyList<FilterTargetOption> _targetOptions = [];

        /// <summary>
        /// Gets or sets the selected Apply-To target.
        /// </summary>
        [ObservableProperty]
        private FilterTargetOption? _selectedTargetOption;

        /// <summary>
        /// Gets or sets the ID3v2 language for multi-instance frames.
        /// </summary>
        [ObservableProperty]
        private string _id3v2Language = string.Empty;

        /// <summary>
        /// Gets or sets the ID3v2 description for multi-instance frames.
        /// </summary>
        [ObservableProperty]
        private string _id3v2Description = string.Empty;

        /// <summary>
        /// Gets whether <see cref="AncestorFolderLevel"/> is shown.
        /// </summary>
        [ObservableProperty]
        private bool _hasAncestorFolderLevel;

        /// <summary>
        /// Gets whether ID3v2 language and description fields are shown.
        /// </summary>
        [ObservableProperty]
        private bool _hasId3v2MultiInstanceFields;

        /// <summary>
        /// Gets or sets the apply-scope mode.
        /// </summary>
        [ObservableProperty]
        private FilterApplyScopeMode _scopeMode = FilterApplyScopeMode.Whole;

        /// <summary>
        /// Gets whether substring scope controls are visible.
        /// </summary>
        [ObservableProperty]
        private bool _showSubstringOptions;

        /// <summary>
        /// Gets whether token scope controls are visible.
        /// </summary>
        [ObservableProperty]
        private bool _showTokenOptions;

        /// <summary>
        /// Gets or sets the substring start position (1-based inclusive).
        /// </summary>
        [ObservableProperty]
        private decimal _substringStartPosition = 1;

        /// <summary>
        /// Gets or sets the substring end position (1-based inclusive).
        /// </summary>
        [ObservableProperty]
        private decimal _substringEndPosition = 5;

        /// <summary>
        /// Gets or sets the token separator string.
        /// </summary>
        [ObservableProperty]
        private string _tokenSeparator = "-";

        /// <summary>
        /// Gets or sets the 1-based token index.
        /// </summary>
        [ObservableProperty]
        private decimal _tokenNumber = 1;

        /// <summary>
        /// Gets a short reason OK is disabled, or <see langword="null"/> when OK is allowed.
        /// </summary>
        /// <remarks>
        /// Token scope requires a non-empty separator (same rule as <see cref="StringApplyScopeTransform"/>).
        /// </remarks>
        public string? ConfirmDisabledReason =>
            ScopeMode == FilterApplyScopeMode.Token && string.IsNullOrEmpty(TokenSeparator)
                ? "Token Separator required"
                : null;

        /// <summary>
        /// Gets whether OK can accept the current draft.
        /// </summary>
        public bool CanConfirm => ConfirmDisabledReason is null;

        /// <summary>
        /// Builds the Apply-To target from the current draft fields.
        /// </summary>
        /// <returns>Target instance, or <see langword="null"/> when Apply-To is unavailable.</returns>
        public FilterTarget? BuildTarget()
        {
            if (!HasApplyTo || SelectedTargetOption is null)
            {
                return null;
            }

            return SelectedTargetOption.BuildTarget(
                ancestorFolderLevel: (int)AncestorFolderLevel,
                id3v2Language: HasId3v2MultiInstanceFields ? Id3v2Language : null,
                id3v2Description: HasId3v2MultiInstanceFields ? Id3v2Description : null
            );
        }

        /// <summary>
        /// Builds the apply scope from the current draft fields.
        /// </summary>
        /// <returns><see langword="null"/> for whole-field scope.</returns>
        public StringApplyScope? BuildApplyScope()
        {
            return ScopeMode switch
            {
                FilterApplyScopeMode.Whole => null,
                FilterApplyScopeMode.Substring => new SubstringApplyScope(
                    StartPosition: Math.Max(1, (int)SubstringStartPosition),
                    StartAnchor: SubstringStartAnchorOption.Anchor,
                    EndPosition: Math.Max(1, (int)SubstringEndPosition),
                    EndAnchor: SubstringEndAnchorOption.Anchor
                ),
                FilterApplyScopeMode.Token => new TokenApplyScope(
                    Separator: TokenSeparator,
                    TokenNumber: Math.Max(1, (int)TokenNumber)
                ),
                _ => null,
            };
        }

        partial void OnSelectedTargetGroupChanged(FilterTargetGroupOption? value)
        {
            if (_isLoadingTarget)
            {
                return;
            }

            TargetOptions = value?.Targets ?? [];
            SelectedTargetOption = TargetOptions.Count > 0 ? TargetOptions[0] : null;
        }

        partial void OnSelectedTargetOptionChanged(FilterTargetOption? value)
        {
            _UpdateTargetParameterVisibility(value);
        }

        private void _UpdateTargetParameterVisibility(FilterTargetOption? option)
        {
            HasAncestorFolderLevel = option?.Prototype is AncestorFolderTarget;
            HasId3v2MultiInstanceFields =
                option?.Prototype is Id3v2FrameTarget frame
                && Id3v2ModeledFrame.MultiInstanceFrameIds.Contains(frame.FrameId);
        }

        partial void OnScopeModeChanged(FilterApplyScopeMode value)
        {
            _UpdateScopeVisibility(value);
            OnPropertyChanged(nameof(ConfirmDisabledReason));
            OnPropertyChanged(nameof(CanConfirm));
        }

        partial void OnTokenSeparatorChanged(string value)
        {
            OnPropertyChanged(nameof(ConfirmDisabledReason));
            OnPropertyChanged(nameof(CanConfirm));
        }

        private void _LoadFromStringFilter(StringTargetFilter stringFilter)
        {
            var (group, option, ancestorLevel) = FilterTargetCatalog.Resolve(stringFilter.Target);
            _isLoadingTarget = true;
            try
            {
                SelectedTargetGroup = group;
                TargetOptions = group.Targets;
                SelectedTargetOption = option;
                AncestorFolderLevel = ancestorLevel;
                if (stringFilter.Target is Id3v2FrameTarget id3v2)
                {
                    Id3v2Language = id3v2.Language ?? string.Empty;
                    Id3v2Description = id3v2.Description ?? string.Empty;
                }
                else
                {
                    Id3v2Language = string.Empty;
                    Id3v2Description = string.Empty;
                }

                _UpdateTargetParameterVisibility(option);
            }
            finally
            {
                _isLoadingTarget = false;
            }

            switch (stringFilter.ApplyScope)
            {
                case SubstringApplyScope substring:
                    ScopeMode = FilterApplyScopeMode.Substring;
                    SubstringStartPosition = substring.StartPosition;
                    SubstringStartAnchorOption = StringScopeAnchorOption.FromAnchor(substring.StartAnchor);
                    SubstringEndPosition = substring.EndPosition;
                    SubstringEndAnchorOption = StringScopeAnchorOption.FromAnchor(substring.EndAnchor);
                    break;
                case TokenApplyScope token:
                    ScopeMode = FilterApplyScopeMode.Token;
                    TokenSeparator = token.Separator;
                    TokenNumber = token.TokenNumber;
                    break;
                default:
                    ScopeMode = FilterApplyScopeMode.Whole;
                    break;
            }

            _UpdateScopeVisibility(ScopeMode);
        }

        private void _UpdateScopeVisibility(FilterApplyScopeMode mode)
        {
            ShowSubstringOptions = mode == FilterApplyScopeMode.Substring;
            ShowTokenOptions = mode == FilterApplyScopeMode.Token;
        }
    }
}
