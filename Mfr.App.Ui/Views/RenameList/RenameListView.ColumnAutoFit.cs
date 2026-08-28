using Avalonia.Controls;
using Mfr.App.Ui.Views.GridColumnSizing;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Double-click column header splitter auto-fit for <see cref="RenameListView"/>.
    /// </summary>
    public partial class RenameListView
    {
        private void _WireColumnAutoFit()
        {
            DataGridColumnAutoFit.Attach(RenameGrid, _ResolveAutoFitWidth);
        }

        private int? _ResolveAutoFitWidth(DataGridColumn column)
        {
            if (_viewModel is null)
            {
                return null;
            }

            var fieldKey = RenameListGridColumns.GetFieldKey(column);
            if (fieldKey is null)
            {
                return null;
            }

            return RenameListColumnAutoFit.ResolveAutoFitWidth(_viewModel.Entries, fieldKey.Value);
        }
    }
}
