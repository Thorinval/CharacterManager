namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;

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

    // Import/Export options
    private PmlExportOptions exportOptions = new();

    // Import checkboxes (mapped to PmlExportOptions)
    private bool importInventory 
    { 
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_INVENTORY);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_INVENTORY); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_INVENTORY); }
    }
    private bool importTemplates
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_TEMPLATES);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_TEMPLATES); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_TEMPLATES); }
    }
    private bool importBestSquad
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_BEST_SQUAD);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_BEST_SQUAD); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_BEST_SQUAD); }
    }
    private bool importHistories
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_HISTORIES);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_HISTORIES); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_HISTORIES); }
    }
    private bool importLeagueHistory
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_LEAGUE_HISTORY);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_LEAGUE_HISTORY); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_LEAGUE_HISTORY); }
    }
    private bool importCapacites
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_CAPACITES);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_CAPACITES); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_CAPACITES); }
    }

    // Export checkboxes (mapped to PmlExportOptions)
    private bool exportInventory 
    { 
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_INVENTORY);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_INVENTORY); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_INVENTORY); }
    }
    private bool exportTemplates
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_TEMPLATES);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_TEMPLATES); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_TEMPLATES); }
    }
    private bool exportBestSquad
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_BEST_SQUAD);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_BEST_SQUAD); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_BEST_SQUAD); }
    }
    private bool exportHistories
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_HISTORIES);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_HISTORIES); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_HISTORIES); }
    }
    private bool exportLeagueHistory
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_LEAGUE_HISTORY);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_LEAGUE_HISTORY); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_LEAGUE_HISTORY); }
    }
    private bool exportCapacites
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_CAPACITES);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_CAPACITES); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_CAPACITES); }
    }

    protected override async Task OnInitializedAsync()
    {
        // Initialiser les options d'export avec les valeurs par défaut
        exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_INVENTORY);
        lastImportedFileName = await PmlImportService.GetLastImportedFileName();
    }

    private void OnFileSelected(InputFileChangeEventArgs e)
    {
        selectedFile = e.File;
        selectedFileName = e.File.Name;
    }

    private bool HasSelectedImportTypes()
    {
        return exportOptions.HasSelectedExports();
    }

    private bool HasSelectedExportTypes()
    {
        return exportOptions.HasSelectedExports();
    }

    private async Task HandleImport()
    {
        if (selectedFile == null || !HasSelectedImportTypes())
            return;

        isImporting = true;

        try
        {
            using var stream = selectedFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB max
            importResult = await PmlImportService.ImportPmlAsync(
                stream,
                selectedFileName ?? "",
                exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_INVENTORY),
                exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_TEMPLATES),
                exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_BEST_SQUAD),
                exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_HISTORIES),
                exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_LEAGUE_HISTORY)
            );

            // Import capacités si sélectionné
            if (exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_CAPACITES) && importResult.IsSuccess)
            {
                using var capacitesStream = new MemoryStream();
                await stream.CopyToAsync(capacitesStream);
                capacitesStream.Position = 0;
                var capacitesResult = await PmlImportService.ImportCapacitesAsync(capacitesStream, selectedFileName ?? "");
                if (capacitesResult.IsSuccess)
                {
                    importResult.SuccessCount += capacitesResult.SuccessCount;
                    importResult.Errors.AddRange(capacitesResult.Errors);
                }
                else if (!capacitesResult.IsSuccess && !capacitesResult.Error?.Contains("Aucune section") == true)
                {
                    importResult.Errors.Add(capacitesResult.Error ?? "Erreur lors de l'import des capacités");
                }
            }

            importComplete = true;

            // Rafraîchir le nom du dernier fichier importé
            lastImportedFileName = await PmlImportService.GetLastImportedFileName();
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

        await HandleExportInternal(downloadToClient: true);
    }

    private async Task HandleExportInternal(bool downloadToClient)
    {
        try
        {
            var exportData = await PmlImportService.ExportPmlAsync(exportOptions);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"export_{timestamp}.pml";

            if (downloadToClient)
            {
                importResult = new ImportResult
                {
                    IsSuccess = true,
                    SuccessCount = 1,
                    Error = $"Export réussi: {fileName} ({exportData.Length} bytes)"
                };
                importComplete = true;
                await JS.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(exportData));
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
    }
  
    private void Reset()
    {
        selectedFile = null;
        selectedFileName = null;
        importComplete = false;
        importResult = null;
        activeTab = "import";
    }
    private async Task ExportAsConfigPml()
    {
        try
        {
            var exportData = await PmlImportService.ExportPmlAsync(exportOptions);

            var configPath = Path.Combine("wwwroot", "config.pml");
            await File.WriteAllBytesAsync(configPath, exportData);

            importResult = new ImportResult
            {
                IsSuccess = true,
                SuccessCount = 1,
                Error = $"Export serveur réussi : {configPath} ({exportData.Length} bytes)"
            };
            importComplete = true;
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            importResult = new ImportResult
            {
                IsSuccess = false,
                Error = $"Erreur export serveur : {ex.Message}"
            };
            importComplete = true;
            await InvokeAsync(StateHasChanged);
        }
    }

}

