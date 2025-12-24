using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace brickapp.Components.Shared.PartsListUpload;

public partial class PartsListUploader
{
    [Parameter] public string Title { get; set; } = "Upload parts list";
    [Parameter] public string? HelpText { get; set; }
    [Parameter] public string? Accept { get; set; }
    [Parameter] public bool Disabled { get; set; }

    [Parameter]
    public IReadOnlyList<PartsUploadFormat> Formats { get; set; }
        = new[] { PartsUploadFormat.RebrickableCsv, PartsUploadFormat.RebrickableXml, PartsUploadFormat.BricklinkXml };

    [Parameter] public Func<PartsUploadFormat, string>? FormatLabelFunc { get; set; }

    private string FormatLabel(PartsUploadFormat f)
        => FormatLabelFunc?.Invoke(f) ?? f.ToString();

    [Parameter] public long MaxFileBytes { get; set; } = 10 * 1024 * 1024;
    [Parameter] public int MaxInvalidRowsShown { get; set; } = 500;
    [Parameter] public EventCallback<ParseResult<ParsedPart>> OnParsed { get; set; }
    [Parameter] public EventCallback<List<ParsedPart>> OnAdd { get; set; }
    [Parameter] public EventCallback OnCleared { get; set; }
    [Parameter] public bool ShowAddButton { get; set; } = true;
    [Parameter] public string AddButtonText { get; set; } = "Add upload items";
    [Parameter] public string ClearButtonText { get; set; } = "Clear upload";
    private bool _uploading;
    private string? _error;
    private ParseResult<ParsedPart>? _result;
    private PartsUploadFormat _selectedFormat = PartsUploadFormat.RebrickableCsv;

    protected override void OnParametersSet()
    {
        if (Formats.Count == 1)
            _selectedFormat = Formats[0];
    }

    private string EffectiveAccept
        => !string.IsNullOrWhiteSpace(Accept)
            ? Accept!
            : (_selectedFormat == PartsUploadFormat.RebrickableCsv ? ".csv" : ".xml");

    private async Task OnFileSelected(InputFileChangeEventArgs e)
    {
        if (Disabled) return;

        _error = null;
        _result = null;

        var file = e.File;

        if (file.Size > MaxFileBytes)
        {
            _error = $"File too large (max {(MaxFileBytes / (1024 * 1024))}MB).";
            return;
        }

        _uploading = true;
        StateHasChanged();

        try
        {
            using var stream = file.OpenReadStream(MaxFileBytes);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                _error = "File is empty.";
                return;
            }

            var parsed = await UploadService.ParseAsync(content, _selectedFormat);
            parsed.AppliedFormat = _selectedFormat;
            _result = parsed;

            if (!string.IsNullOrWhiteSpace(parsed.FatalError))
                _error = parsed.FatalError;
            else if (parsed.MappedItems.Count == 0 &&
                     (parsed.Unmapped.Count > 0 || parsed.InvalidColorIds.Count > 0 || parsed.InvalidRows.Count > 0))
                _error = "No usable items after validation (unmapped / invalid colors / invalid rows).";
            else if (parsed.MappedItems.Count == 0)
                _error = "No valid items found in file.";

            if (OnParsed.HasDelegate && _result is not null)
                await OnParsed.InvokeAsync(_result);
        }
        catch (Exception ex)
        {
            _error = $"Upload/Parse failed: {ex.Message}";
        }
        finally
        {
            _uploading = false;
            StateHasChanged();
        }
    }

    private async Task Add()
    {
        if (_result is null) return;
        if ((_result.MappedItems.Count) == 0) return;

        if (OnAdd.HasDelegate)
            await OnAdd.InvokeAsync(_result.MappedItems);
    }

    private async Task Clear()
    {
        _error = null;
        _result = null;

        if (OnCleared.HasDelegate)
            await OnCleared.InvokeAsync();

        StateHasChanged();
    }
}