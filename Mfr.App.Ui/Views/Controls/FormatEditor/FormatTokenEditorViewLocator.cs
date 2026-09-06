using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Mfr.App.Ui.ViewModels.Controls.FormatEditor;

namespace Mfr.App.Ui.Views.Controls.FormatEditor
{
    /// <summary>
    /// Resolves a type-specific format-token editor body from its view model by naming convention.
    /// <para>
    /// <c>Mfr.App.Ui.ViewModels.Controls.FormatEditor.TokenEditors[.Sub].FooViewModel</c> maps to
    /// <c>Mfr.App.Ui.Views.Controls.FormatEditor.TokenEditors[.Sub].FooView</c>.
    /// </para>
    /// </summary>
    public sealed class FormatTokenEditorViewLocator : IDataTemplate
    {
        private const string ViewModelNamespacePrefix = "Mfr.App.Ui.ViewModels.Controls.FormatEditor.TokenEditors";
        private const string ViewNamespacePrefix = "Mfr.App.Ui.Views.Controls.FormatEditor.TokenEditors";
        private const string ViewModelSuffix = "ViewModel";

        /// <inheritdoc />
        public bool Match(object? data)
        {
            return data is IFormatTokenEditorViewModel;
        }

        /// <inheritdoc />
        public Control? Build(object? param)
        {
            if (param is not IFormatTokenEditorViewModel viewModel)
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
        /// <returns>Matching view type under <see cref="ViewNamespacePrefix"/>.</returns>
        /// <exception cref="InvalidOperationException">No view type follows the naming convention.</exception>
        private static Type _ResolveViewType(Type viewModelType)
        {
            ArgumentNullException.ThrowIfNull(viewModelType);

            var viewModelNamespace = viewModelType.Namespace;
            var isUnderTokenEditors =
                viewModelNamespace == ViewModelNamespacePrefix
                || (
                    viewModelNamespace is not null
                    && viewModelNamespace.StartsWith(ViewModelNamespacePrefix + ".", StringComparison.Ordinal)
                );
            if (!isUnderTokenEditors || !viewModelType.Name.EndsWith(ViewModelSuffix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Format-token editor view model must live under {ViewModelNamespacePrefix} and end with {ViewModelSuffix}: {viewModelType.FullName}."
                );
            }

            var relativeNamespace = viewModelNamespace![ViewModelNamespacePrefix.Length..];
            var viewTypeName =
                $"{ViewNamespacePrefix}{relativeNamespace}.{viewModelType.Name[..^ViewModelSuffix.Length]}View";
            return viewModelType.Assembly.GetType(viewTypeName)
                ?? throw new InvalidOperationException(
                    $"No format-token editor view registered for {viewModelType.Name}. Expected type {viewTypeName}."
                );
        }
    }
}
