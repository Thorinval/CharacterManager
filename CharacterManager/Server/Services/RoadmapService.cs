using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CharacterManager.Server.Services;
public class RoadmapService(ApplicationDbContext db)
{
    private readonly ApplicationDbContext _db = db;

    public async Task SaveRoadmapAsync(string text)
    {
        var note = await _db.RoadmapNotes.FirstOrDefaultAsync();

        if (note == null)
        {
            note = new RoadmapNote { Content = text };
            _db.RoadmapNotes.Add(note);
        }
        else
        {
            note.Content = text;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<string> LoadRoadmapAsync()
    {
        var note = await _db.RoadmapNotes.FirstOrDefaultAsync();
        return note?.Content ?? "";
    }
}
