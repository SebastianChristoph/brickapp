namespace brickapp.Components.Dialogs
{
    public class ApproveItemDialogResult
    {
        public string Name { get; set; } = string.Empty;
        public bool NameChanged { get; set; }
        public string OriginalName { get; set; } = string.Empty;
    }

    public class RejectDialogResult
    {
        public RejectAction Action { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public enum RejectAction
    {
        Reject,
        RejectToPending
    }
}
