namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using CharacterManager.Server.Constants;
using CharacterManager.Components;

public partial class ImportExportPML
{
    [Inject]
    public PmlImportService PmlImportService { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    public IJSRuntime JS { get; set; } = null!;

    private IBrowserFile? selectedFile;
    private string? selectedFileName;
    private bool isImporting = false;
    private bool importComplete = false;
    private ImportResult? importResult;
    private string? lastImportedFileName;
    private string activeTab = "import";

    // Import checkboxes
    private bool importInventory = true;
    private bool importTemplates = false;
    private bool importBestSquad = false;
    private bool importHistories = false;

    // Export checkboxes
    private bool exportInventory = true;
    private bool exportTemplates = false;
    private bool exportBestSquad = false;
    private bool exportHistories = false;

    protected override async Task OnInitializedAsync()
    {
        lastImportedFileName = await PmlImportService.GetLastImportedFileName();
    }

    private void OnFileSelected(InputFileChangeEventArgs e)
    {
        selectedFile = e.File;
        selectedFileName = e.File.Name;
    }

    private bool HasSelectedImportTypes()
    {
        return importInventory || importTemplates || importBestSquad || importHistories;
    }

    private bool HasSelectedExportTypes()
    {
        return exportInventory || exportTemplates || exportBestSquad || exportHistories;
    }

    private async Task HandleImport()
    {
        if (selectedFile == null || !HasSelectedImportTypes())
            return;

        isImporting = true;

        try
        {
            using (var stream = selectedFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024)) // 10MB max
            {
                importResult = await PmlImportService.ImportPmlAsync(
                    stream, 
                    selectedFileName ?? "",
                    importInventory,
                    importTemplates,
                    importBestSquad,
                    importHistories);
                importComplete = true;
                
                // Rafraîchir le nom du dernier fichier importé
                lastImportedFileName = await PmlImportService.GetLastImportedFileName();
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

    private async Task HandleExport()
    {
        if (!HasSelectedExportTypes())
            return;

        try
        {
            var exportData = await PmlImportService.ExportPmlAsync(
                exportInventory,
                exportTemplates,
                exportBestSquad,
                exportHistories);

            // Generate filename with timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"export_{timestamp}.pml";

            // Download the file using JavaScript interop would be needed here
            // For now, we'll show a success message
            importResult = new ImportResult
            {
                IsSuccess = true,
                SuccessCount = 1,
                Error = $"Export réussi: {fileName} ({exportData.Length} bytes)"
            };
            importComplete = true;

            // Trigger download via JavaScript
            await JS.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(exportData));
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
    }

    private void Reset()
    {
        selectedFile = null;
        selectedFileName = null;
        importComplete = false;
        importResult = null;
        activeTab = "import";
    }
}

