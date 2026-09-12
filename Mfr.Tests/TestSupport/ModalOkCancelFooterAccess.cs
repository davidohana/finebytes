using Avalonia.Controls;
using Avalonia.VisualTree;
using Mfr.App.Ui.Views.Controls;

namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Locates shared <see cref="ModalOkCancelFooter"/> buttons inside modal dialogs.
    /// </summary>
    public static class ModalOkCancelFooterAccess
    {
        /// <summary>
        /// Returns the footer named <c>Footer</c>, or the first footer descendant.
        /// </summary>
        /// <param name="root">Dialog or host control.</param>
        /// <returns>The footer control.</returns>
        public static ModalOkCancelFooter RequireFooter(Control root)
        {
            var named = root.FindControl<ModalOkCancelFooter>("Footer");
            if (named is not null)
            {
                return named;
            }

            return root.GetVisualDescendants().OfType<ModalOkCancelFooter>().Single();
        }

        /// <summary>
        /// Returns the accept (OK/Save) button from the dialog footer.
        /// </summary>
        /// <param name="root">Dialog or host control.</param>
        /// <returns>Accept button.</returns>
        public static Button RequireAcceptButton(Control root)
        {
            return RequireFooter(root).AcceptButton;
        }

        /// <summary>
        /// Returns the Cancel button from the dialog footer.
        /// </summary>
        /// <param name="root">Dialog or host control.</param>
        /// <returns>Cancel button.</returns>
        public static Button RequireCancelButton(Control root)
        {
            return RequireFooter(root).DismissButton;
        }
    }
}
