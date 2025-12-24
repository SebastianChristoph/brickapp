using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace brickapp.Components.Dialogs;

public partial class AddNewSetDialog
{
    [Parameter] public EventCallback OnClose { get; set; }
    public string? Brand { get; set; }
    public string? SetNo { get; set; }
    public string? SetName { get; set; }
    public IBrowserFile? UploadedImage { get; set; }
    public List<SetItemInput> Items { get; set; } = new();

    public class SetItemInput
    {
        public string? ItemIdOrName { get; set; }
        public int Quantity { get; set; }
        public string? Color { get; set; }
    }

    private void AddItem()
    {
        Items.Add(new SetItemInput());
    }

    private void OnImageUpload(InputFileChangeEventArgs e)
    {
        UploadedImage = e.File;
    }

    private async Task Submit()
    {
        await OnClose.InvokeAsync();
    }

    private void Cancel()
    {
        OnClose.InvokeAsync();
    }
}