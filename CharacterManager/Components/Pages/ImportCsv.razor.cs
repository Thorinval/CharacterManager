namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using CharacterManager.Server.Services;

public partial class ImportCsv
{
    [Inject]
    public CsvImportService CsvImportService { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    private IBrowserFile? selectedFile;
    private string? selectedFileName;
    private bool isImporting = false;
    private bool importComplete = false;
    private ImportResult? importResult;
    private string? lastImportedFileName;

    protected override async Task OnInitializedAsync()
    {
        lastImportedFileName = await CsvImportService.GetLastImportedFileName();
    }

    private void OnFileSelected(InputFileChangeEventArgs e)
    {
        selectedFile = e.File;
        selectedFileName = e.File.Name;
    }

    private async Task HandleImport()
    {
        if (selectedFile == null)
            return;

        isImporting = true;

        try
        {
            using (var stream = selectedFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024)) // 10MB max
            {
                importResult = await CsvImportService.ImportCsvAsync(stream, selectedFileName ?? "");
                importComplete = true;
                
                // Rafraîchir le nom du dernier fichier importé
                lastImportedFileName = await CsvImportService.GetLastImportedFileName();
            }
        }
        catch (Exception ex)
        {
            importResult = new ImportResult 
            { 
                IsSuccess = false, 
                Error = $"Erreur: {ex.Message}" 
            };
            importComplete = true;
        }
        finally
        {
            isImporting = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void Reset()
    {
        selectedFile = null;
        selectedFileName = null;
        importComplete = false;
        importResult = null;
    }
}
