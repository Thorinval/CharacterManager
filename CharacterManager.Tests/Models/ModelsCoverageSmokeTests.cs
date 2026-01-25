using System;
using System.Collections.Generic;
using Xunit;
using CharacterManager.Server.Models;
using ActionModel = CharacterManager.Server.Models.Action;

namespace CharacterManager.Tests.Models
{
    public class ModelsCoverageSmokeTests
    {
        [Fact]
        public void Action_Model_Allows_Property_Assignment()
        {
            var a = new ActionModel { Id = 1, Nom = "Test", Icon = "icon.png" };
            Assert.Equal(1, a.Id);
            Assert.Equal("Test", a.Nom);
            Assert.Equal("icon.png", a.Icon);
        }

        [Fact]
        public void AppSettings_Defaults_Can_Be_Updated()
        {
            var s = new AppSettings { Id = 5 };
            s.LastImportedFileName = "import.xml";
            s.LastImportedDate = DateTime.Today;
            s.LastExportDate = DateTime.Today.AddDays(1);
            s.IsAdultModeEnabled = false;
            s.Language = "en";

            Assert.Equal(5, s.Id);
            Assert.Equal("import.xml", s.LastImportedFileName);
            Assert.False(s.IsAdultModeEnabled);
            Assert.Equal("en", s.Language);
        }

        [Fact]
        public void Capacite_Model_Assigns_Properties()
        {
            var c = new Capacite { Id = 10, Nom = "Cap", Description = "Desc", Icon = "icon" };
            Assert.Equal(10, c.Id);
            Assert.Equal("Cap", c.Nom);
            Assert.Equal("Desc", c.Description);
            Assert.Equal("icon", c.Icon);
        }

        [Fact]
        public void ClassementFormModel_Validation_Fails_For_Invalid_Ligue()
        {
            var f = new ClassementFormModel
            {
                DateEnregistrement = DateOnly.FromDateTime(DateTime.Today),
                Nutaku = 10,
                Top150 = 20,
                France = 30,
                Ligue = 60, // invalid per validation rule
                Score = 123
            };
            var results = f.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(f));
            Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("ligue"));
        }

        [Fact]
        public void HistoriqueClassement_Initializes_Collections()
        {
            var h = new HistoriqueClassement
            {
                Score = 999,
                Ligue = 7,
                PuissanceCommandant = 1000,
                PuissanceMercenaires = 2000,
                PuissanceLucie = 3000,
                PuissanceTotale = 6000
            };
            h.Classements.Add(new Classement { Id = 1, Nom = "Top", Type = TypeClassement.Top150, Valeur = 150 });
            h.Mercenaires.Add(new PersonnageClassement { Id = 2, Nom = "Merc", Puissance = 123 });
            h.Commandant = new PersonnageClassement { Id = 3, Nom = "Cmd", Puissance = 456 };
            h.Androides.Add(new PersonnageClassement { Id = 4, Nom = "And", Puissance = 789 });
            h.Pieces.Add(new PieceHistorique { Id = 5, Nom = "Piece", Niveau = 1 });

            Assert.Single(h.Classements);
            Assert.Equal(1, h.Classements[0].Id);
            Assert.Equal(2, h.Mercenaires[0].Id);
            Assert.Equal(3, h.Commandant!.Id);
            Assert.Equal(4, h.Androides[0].Id);
            Assert.Equal(5, h.Pieces[0].Id);
        }

        [Fact]
        public void DonneesEscouadeSerialisees_Assigns_Properties()
        {
            var e = new DonneesEscouadeSerialisees
            {
                Commandant = new PersonnelHistorique { Id = 1, Nom = "Cmd" },
                Mercenaires = new List<PersonnelHistorique> { new PersonnelHistorique { Id = 2, Nom = "M1" } },
                Androides = new List<PersonnelHistorique> { new PersonnelHistorique { Id = 3, Nom = "A1" } },
                LuciePuissance = 10,
                Ligue = 5,
                Nutaku = 100,
                Top150 = 150,
                Pays = 33,
                Score = 999
            };
            Assert.Equal("Cmd", e.Commandant!.Nom);
            Assert.Equal("M1", e.Mercenaires[0].Nom);
            Assert.Equal("A1", e.Androides[0].Nom);
            Assert.Equal(10, e.LuciePuissance);
            Assert.Equal(999, e.Score);
        }

        [Fact]
        public void HistoriqueLigue_Assigns_Properties()
        {
            var l = new HistoriqueLigue { Id = 2, Ligue = 8, DateMontee = DateOnly.FromDateTime(DateTime.Today) };
            Assert.Equal(2, l.Id);
            Assert.Equal(8, l.Ligue);
        }

        [Fact]
        public void HistoriqueModification_Assigns_Properties()
        {
            var m = new HistoriqueModification
            {
                Id = 3,
                TypeEntite = TypeEntite.Personnage,
                EntiteId = 42,
                NomEntite = "Alice",
                TypeModification = TypeModification.Modification,
                DateModification = DateTime.UtcNow,
                DateInsertion = DateTime.UtcNow.AddMinutes(-1),
                DateMiseAJour = DateTime.UtcNow,
                Description = "Change",
                ChampModifie = "Puissance",
                AncienneValeur = "5000",
                NouvelleValeur = "6000",
                EstImportation = true
            };
            Assert.Equal("Alice", m.NomEntite);
            Assert.Equal(TypeModification.Modification, m.TypeModification);
            Assert.Equal("Puissance", m.ChampModifie);
            Assert.Equal("6000", m.NouvelleValeur);
            Assert.True(m.EstImportation);
        }

        [Fact]
        public void ImportLogEntry_Assigns_Properties()
        {
            var log = new ImportLogEntry { Message = "OK", Level = ImportLogLevel.Ok, Category = ImportLogCategory.General, DataType = "xml" };
            Assert.Equal("OK", log.Message);
            Assert.Equal(ImportLogLevel.Ok, log.Level);
            Assert.Equal(ImportLogCategory.General, log.Category);
            Assert.Equal("xml", log.DataType);
        }

        [Fact]
        public void ImportPreviewResult_Assigns_Collections()
        {
            var preview = new ImportPreviewResult();
            preview.Logs.Add(new ImportLogEntry { Message = "Log" });
            preview.Conflicts.Add(new CharacterManager.Server.Services.ImportConflict { PersonnageName = "Alice", ChampModifie = "Nom" });
            preview.ValidCount = 2;
            preview.DuplicateCount = 1;
            preview.IsSuccess = true;
            preview.Error = null;

            Assert.Single(preview.Logs);
            Assert.Single(preview.Conflicts);
            Assert.Equal(2, preview.ValidCount);
            Assert.Equal(1, preview.DuplicateCount);
            Assert.True(preview.IsSuccess);
            Assert.True(preview.HasConflicts);
        }

        [Fact]
        public void ImportResult_Assigns_Collections_And_Properties()
        {
            var result = new ImportResult { IsSuccess = true, SuccessCount = 3, DuplicateCount = 1 };
            result.Errors.Add("E1");
            result.Warnings.Add("W1");
            result.Logs.Add(new ImportLogEntry { Message = "Log" });
            result.ConflictsApplied.Add(new ConflictResolutionApplied { PersonnageName = "Bob", ChampModifie = "Nom", Overwritten = true });

            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.SuccessCount);
            Assert.Equal(1, result.DuplicateCount);
            Assert.Single(result.Errors);
            Assert.Single(result.Warnings);
            Assert.Single(result.Logs);
            Assert.Single(result.ConflictsApplied);
        }

        [Fact]
        public void LucieHouse_Computes_PuissanceTotale_From_Selected_Pieces()
        {
            var h = LucieHouse.CreerDefaut();
            // set some puissance and select pieces
            h.Pieces[0].AspectsTactiques.Puissance = 2;
            h.Pieces[0].AspectsStrategiques.Puissance = 3;
            h.Pieces[0].Selectionnee = true;
            h.Pieces[1].AspectsTactiques.Puissance = 1;
            h.Pieces[1].AspectsStrategiques.Puissance = 4;
            h.Pieces[1].Selectionnee = true;

            Assert.Equal(10, h.PuissanceTotale);
            Assert.Equal(2, h.NombrePiecesSelectionnees);
            Assert.False(h.PeutSelectionner());
        }

        [Fact]
        public void Personnage_Methods_Work_As_Expected()
        {
            var p = new Personnage
            {
                Nom = "Alice",
                Puissance = 100,
                Rang = 2,
                Selectionne = false
            };

            var prc = p.PuissanceReelleCommandant();
            Assert.Equal(140, prc);

            Assert.False(string.IsNullOrEmpty(p.ImageUrlDetail));
            Assert.False(string.IsNullOrEmpty(p.ImageUrlPreview));
            Assert.False(string.IsNullOrEmpty(p.ImageUrlSelected));

            var url1 = p.GetImageUrl(useSelectionState: true);
            Assert.Equal(p.ImageUrlPreview, url1);

            p.Selectionne = true;
            var url2 = p.GetImageUrl(useSelectionState: true);
            Assert.Equal(p.ImageUrlSelected, url2);
        }

        [Fact]
        public void PersonnageClassement_Assigns_Properties()
        {
            var pc = new PersonnageClassement { Id = 2, Nom = "Alice", Puissance = 500 };
            Assert.Equal(2, pc.Id);
            Assert.Equal("Alice", pc.Nom);
            Assert.Equal(500, pc.Puissance);
        }

        [Fact]
        public void PersonnageHistorique_Assigns_Properties()
        {
            var ph = new PersonnageHistorique { Id = 3, Nom = "Alice", Puissance = 700 };
            Assert.Equal(3, ph.Id);
            Assert.Equal("Alice", ph.Nom);
            Assert.Equal(700, ph.Puissance);
        }

        [Fact]
        public void PersonnageImageConfig_Assigns_Properties()
        {
            var pic = new PersonnageImageConfig { CheminImage = "/images/a.png", NomFichier = "a.png", IsAdult = true };
            Assert.True(pic.IsAdult);
            Assert.Equal("/images/a.png", pic.CheminImage);
            Assert.Equal("a.png", pic.NomFichier);
        }

        [Fact]
        public void PersonnagesImagesConfiguration_Adds_Items()
        {
            var cfg = new PersonnagesImagesConfiguration();
            cfg.Images.Add(new PersonnageImageConfig { NomFichier = "x.png" });
            Assert.Single(cfg.Images);
        }

        [Fact]
        public void PieceHistorique_Assigns_Properties()
        {
            var ph = new PieceHistorique { Id = 1, Nom = "Piece", Niveau = 2 };
            ph.AspectsTactiques.Puissance = 10;
            ph.AspectsStrategiques.Puissance = 5;
            Assert.Equal("Piece", ph.Nom);
            Assert.Equal(15, ph.Puissance);
        }

        [Fact]
        public void PmlExportOptions_Selects_And_Queries_Export_Types()
        {
            var o = new PmlExportOptions();
            o.AddExportType(PmlExportOptions.EXPORT_TYPE_INVENTORY);
            o.AddExportType(PmlExportOptions.EXPORT_TYPE_TEMPLATES);
            Assert.True(o.IsExporting(PmlExportOptions.EXPORT_TYPE_INVENTORY));
            Assert.True(o.IsExporting(PmlExportOptions.EXPORT_TYPE_TEMPLATES));

            o.AddCustomExport("extra", new { Info = 1 });
            Assert.True(o.HasSelectedExports());
            Assert.NotNull(o.GetCustomExport("extra"));

            o.ClearAll();
            Assert.False(o.HasSelectedExports());
        }

        [Fact]
        public void Profile_Assigns_Properties()
        {
            var p = new Profile
            {
                Id = 1,
                Username = "user",
                AdultMode = true,
                Language = "en",
                Role = "admin",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                HashAlgorithm = "PBKDF2",
                FailedLoginCount = 2,
                LockoutUntil = DateTimeOffset.UtcNow.AddMinutes(5)
            };

            Assert.Equal("user", p.Username);
            Assert.True(p.AdultMode);
            Assert.Equal("en", p.Language);
            Assert.Equal("admin", p.Role);
            Assert.Equal(2, p.FailedLoginCount);
            Assert.True(p.LockoutUntil.HasValue);
        }

        [Fact]
        public void RoadmapNote_Assigns_Properties()
        {
            var r = new RoadmapNote { Id = 1, Content = "Note" };
            Assert.Equal(1, r.Id);
            Assert.Equal("Note", r.Content);
        }

        [Fact]
        public void Template_Serialization_Methods_Work()
        {
            var t = new Template { Id = 1, Nom = "Team", Description = "Desc" };
            t.SetPersonnageIds(new List<int> { 1, 2, 3 });
            var ids = t.GetPersonnageIds();
            Assert.Equal(new List<int> { 1, 2, 3 }, ids);

            t.PersonnagesJson = "not json";
            var ids2 = t.GetPersonnageIds();
            Assert.Empty(ids2);
        }
    }
}
