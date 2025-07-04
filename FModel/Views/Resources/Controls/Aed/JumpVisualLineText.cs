using System;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using FModel.Extensions;
using FModel.Services;
using FModel.ViewModels;
using ICSharpCode.AvalonEdit.Rendering;

namespace FModel.Views.Resources.Controls;

public class JumpVisualLineText : VisualLineText
{
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public delegate void JumpOnClick(string Jump);

    public event JumpOnClick OnJumpClicked;
    private readonly string _Jump;

    public JumpVisualLineText(string Jump, VisualLine parentVisualLine, int length) : base(parentVisualLine, length)
    {
        _Jump = Jump;
    }

    public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var relativeOffset = startVisualColumn - VisualColumn;
        var text = context.GetText(context.VisualLine.FirstDocumentLine.Offset + RelativeTextOffset + relativeOffset, DocumentLength - relativeOffset);

        if (text.Count != 2) // ": "
            TextRunProperties.SetForegroundBrush(Brushes.Plum);

        return new TextCharacters(text.Text, text.Offset, text.Count, TextRunProperties);
    }

    private bool JumpIsClickable() => !string.IsNullOrEmpty(_Jump) && Keyboard.Modifiers == ModifierKeys.None;

    protected override void OnQueryCursor(QueryCursorEventArgs e)
    {
        if (!JumpIsClickable())
            return;
        e.Handled = true;
        e.Cursor = Cursors.Hand;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || !JumpIsClickable())
            return;
        if (e.Handled || OnJumpClicked == null)
            return;

        OnJumpClicked(_Jump);
        e.Handled = true;
    }

    protected override VisualLineText CreateInstance(int length)
    {
        var a = new JumpVisualLineText(_Jump, ParentVisualLine, length);
        a.OnJumpClicked += async (Jump) =>
        {
            var lineNumber = a.ParentVisualLine.Document.Text.GetNameLineNumberText($"Label_{_Jump}:");

            if (lineNumber > -1)
            {
                var line = a.ParentVisualLine.Document.GetLineByNumber(lineNumber);
                AvalonEditor.YesWeEditor.Select(line.Offset, line.Length);
                AvalonEditor.YesWeEditor.ScrollToLine(lineNumber);
                return;
            }
        };
        return a;
    }

}
