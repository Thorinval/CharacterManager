using System.Collections.Generic;
using CharacterManager.Server.Services;

namespace CharacterManager.Server.Models;

public class ImportPreviewResult
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public List<ImportLogEntry> Logs { get; set; } = new();
    public List<ImportConflict> Conflicts { get; set; } = new();
    public int ValidCount { get; set; }
    public bool HasConflicts => Conflicts.Count > 0;
}
