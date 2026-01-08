namespace CharacterManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;

public partial class ImportExportPml
{
    [Inject]
    public PmlImportService PmlImportService { get; set; } = null!;

    [Inject]
    public PmlExportService PmlExportService { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    public IJSRuntime JS { get; set; } = null!;

    private IBrowserFile? selectedFile;
    private string? selectedFileName;
    internal bool isImporting = false;
    internal bool importComplete = false;
    private ImportResult? importResult;
    internal string? lastImportedFileName;
    internal string activeTab = "import";

    // Import/Export options
    internal PmlExportOptions exportOptions = new();

    // Import checkboxes (mapped to PmlExportOptions)
    internal bool importInventory 
    { 
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_INVENTORY);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_INVENTORY); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_INVENTORY); }
    }
    internal bool importTemplates
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_TEMPLATES);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_TEMPLATES); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_TEMPLATES); }
    }
    internal bool importBestSquad
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_BEST_SQUAD);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_BEST_SQUAD); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_BEST_SQUAD); }
    }
    internal bool importHistories
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_HISTORIES);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_HISTORIES); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_HISTORIES); }
    }
    internal bool importLeagueHistory
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_LEAGUE_HISTORY);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_LEAGUE_HISTORY); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_LEAGUE_HISTORY); }
    }
    internal bool importCapacites
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_CAPACITES);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_CAPACITES); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_CAPACITES); }
    }

    // Export checkboxes (mapped to PmlExportOptions)
    internal bool exportInventory 
    { 
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_INVENTORY);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_INVENTORY); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_INVENTORY); }
    }
    internal bool exportTemplates
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_TEMPLATES);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_TEMPLATES); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_TEMPLATES); }
    }
    internal bool exportBestSquad
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_BEST_SQUAD);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_BEST_SQUAD); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_BEST_SQUAD); }
    }
    internal bool exportHistories
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_HISTORIES);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_HISTORIES); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_HISTORIES); }
    }
    internal bool exportLeagueHistory
    {
        get => exportOptions.IsExporting(PmlExportOptions.EXPORT_TYPE_LEAGUE_HISTORY);
        set { if (value) exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_LEAGUE_HISTORY); else exportOptions.RemoveExportType(PmlExportOptions.EXPORT_TYPE_LEAGUE_HISTORY); }
    }
    internal bool exportCapacites
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

    internal void OnFileSelected(InputFileChangeEventArgs e)
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

    internal async Task HandleImport()
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

    internal async Task HandleExport()
    {
        if (!HasSelectedExportTypes())
            return;

        await HandleExportInternal(downloadToClient: true);
    }

    internal async Task HandleExportInternal(bool downloadToClient)
    {
        try
        {
            var exportData = await PmlExportService.ExportPmlAsync(exportOptions);

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
  
    internal void Reset()
    {
        selectedFile = null;
        selectedFileName = null;
        importComplete = false;
        importResult = null;
        activeTab = "import";
    }
    internal async Task ExportAsConfigPml()
    {
        try
        {
            var exportData = await PmlExportService.ExportPmlAsync(exportOptions);

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

