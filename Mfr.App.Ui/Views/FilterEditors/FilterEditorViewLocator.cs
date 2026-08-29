using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Mfr.App.Ui.ViewModels.FilterEditors;

namespace Mfr.App.Ui.Views.FilterEditors
{
    /// <summary>
    /// Resolves a type-specific filter editor view from its view model by naming convention.
    /// <para>
    /// <c>Mfr.App.Ui.ViewModels.FilterEditors.FooViewModel</c> maps to
    /// <c>Mfr.App.Ui.Views.FilterEditors.FooView</c>.
    /// </para>
    /// </summary>
    public sealed class FilterEditorViewLocator : IDataTemplate
    {
        private const string ViewModelNamespace = "Mfr.App.Ui.ViewModels.FilterEditors";
        private const string ViewNamespace = "Mfr.App.Ui.Views.FilterEditors";
        private const string ViewModelSuffix = "ViewModel";

        /// <inheritdoc />
        public bool Match(object? data)
        {
            return data is FilterOptionsEditorViewModel;
        }

        /// <inheritdoc />
        public Control? Build(object? param)
        {
            if (param is not FilterOptionsEditorViewModel viewModel)
            {
                return null;
            }

            var viewModelType = viewModel.GetType();
            var viewType = _ResolveViewType(viewModelType);
            var view = (Control)Activator.CreateInstance(viewType)!;
            view.DataContext = viewModel;
            return view;
        }

        /// <summary>
        /// Maps <paramref name="viewModelType"/> to the paired editor view type.
        /// </summary>
        /// <param name="viewModelType">Editor view-model type.</param>
        /// <returns>Matching view type in <see cref="ViewNamespace"/>.</returns>
        /// <exception cref="InvalidOperationException">No view type follows the naming convention.</exception>
        private static Type _ResolveViewType(Type viewModelType)
        {
            ArgumentNullException.ThrowIfNull(viewModelType);

            if (
                viewModelType.Namespace != ViewModelNamespace
                || !viewModelType.Name.EndsWith(ViewModelSuffix, StringComparison.Ordinal)
            )
            {
                throw new InvalidOperationException(
                    $"Filter editor view model must live in {ViewModelNamespace} and end with {ViewModelSuffix}: {viewModelType.FullName}."
                );
            }

            var viewTypeName = $"{ViewNamespace}.{viewModelType.Name[..^ViewModelSuffix.Length]}View";
            return viewModelType.Assembly.GetType(viewTypeName)
                ?? throw new InvalidOperationException(
                    $"No filter editor view registered for {viewModelType.Name}. Expected type {viewTypeName}."
                );
        }
    }
}
