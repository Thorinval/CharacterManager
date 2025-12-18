# Release Notes - Character Manager

> **Version actuelle**: 0.2.0  
> Pour ajouter une nouvelle version, voir [Comment ajouter une nouvelle entrée](#comment-ajouter-une-nouvelle-entrée) en bas du fichier.

---

## Version 0.2.0 (Décembre 2025)

### ✨ Nouvelles Fonctionnalités

#### Localization Multilingue

- **Localization complète** de l'interface utilisateur en français et anglais
- Support pour tous les écrans majeurs (Login, Inventaire, Escouade, Templates, Historique, Importation CSV, etc.)
- **Service de localization centralisé** (ClientLocalizationService) pour maintenance facile
- **Chargement adaptatif** des ressources de traduction lors du démarrage
- Menu de navigation traduit avec **détection automatique de la langue** de l'utilisateur

#### Gestion des Personnages Améliorée

- **Ajout du champ Puissance** dans les formulaires d'ajout/modification
- **Téléchargement d'images doubles**:
  - Image portrait pour l'inventaire : `{nom}_small_portrait.png`
  - Image de sélection pour l'escouade : `{nom}_small_select.png`
- Aperçu en temps réel lors du téléchargement d'images
- Stockage automatique dans `wwwroot/images/personnages/` avec nommage conventionnel

#### Interface Inventaire Optimisée

- **Nouvelle mise en page CSS Grid** responsive pour afficher les cartes:
  - Base: 250px de largeur
  - Écrans larges (1600px+): 290px
  - Écrans moyens (1200px): 260px
  - Écrans petits (600px): 1 colonne
- **Améliorations des cartes**:
  - Images avec aspect ratio 4:5 (contain) pour respecter les proportions
  - Boutons d'action "Modifier" et "Supprimer" avec libellés complets
  - Retrait du bouton "Détail" (image clickable pour accéder aux détails)
  - Noms des personnages centrés
  - Valeurs (Niveau, Puissance) alignées à droite
  - Padding et espacement optimisés pour meilleure lisibilité

#### Versionning Centralisé

- **Version unique** stockée dans `appsettings.json` (`AppInfo:Version`)
- Synchronisation automatique avec `.csproj` (Version, InformationalVersion)
- Service `AppVersionService` fournit la version depuis la configuration
- Tous les composants utilisent le service (pas de hardcoding)

### 🔧 Améliorations Techniques

#### Architecture de Localization

- `LocalizationProvider.razor` : Composant passerelle qui gate le rendu jusqu'à l'initialisation
- `ClientLocalizationService` : Lecture efficace des fichiers JSON i18n depuis le disque
- `LocalizedText.razor` : Composant réutilisable pour traductions dans les templates
- Mécanisme lazy-load pour gérer les appels pré-initialisation

#### Optimisation CSS

- Utilisation de `repeat(auto-fit)` pour grilles responsive
- `justify-content: start` pour alignement gauche stable
- Propriétés d'aspect-ratio pour images responsive
- Breakpoints media queries granulaires pour tous les appareils

#### Gestion d'Uploads

- Support de fichiers PNG/JPEG jusqu'à 10 MB
- Validation de noms de fichiers (minuscules, underscores)
- Création automatique du dossier de destination
- Aperçu base64 avant sauvegarde

### 📋 Changements de l'Interface Utilisateur

| Élément | Avant | Après |
|---------|-------|-------|
| Langue de l'interface | Français uniquement | Français + Anglais |
| Largeur cartes inventaire | Variables (trop larges) | Fixe 250-290px (responsive) |
| Boutons action | Voir détail / Modifier / Supprimer (labels longs) | Modifier / Supprimer (labels complets) + aperçu image |
| Images personnage | Portrait seulement | Portrait + Image sélection escouade |
| Champ Puissance | Absent | Ajouté dans formulaires |
| Versionning | Hardcodé (1.0.0) | Centralisé dans appsettings.json (0.2.0) |

### 🐛 Corrections de Bugs

- Image d'inventaire qui se tronquait en haut/bas (changement en contain)
- Cartes s'étirant sur écrans larges (grille sans 1fr, utilisation de valeurs fixes)
- Appels T() pré-initialisation causant affichage de clés (ajout EnsureResourcesLoaded lazy-load)
- Menu non traduit au démarrage (ajout de LocalizationProvider gate)

### 📁 Structure de Fichiers Mise à Jour

```text
wwwroot/
├── images/
│   └── personnages/          # Dossier pour les images uploadées
│       ├── belle_small_portrait.png
│       ├── belle_small_select.png
│       └── ... (autres images)
└── i18n/
    ├── en.json              # 150+ clés anglaises
    └── fr.json              # 150+ clés françaises
```

### 📊 Couverture de Localization

- ✅ Pages: Login, ChangePassword, ManageUsers, Settings, Templates, History, ImportCSV, Squad, BestSquad, Inventory, Home, DetailCharacter
- ✅ Composants: Navigation, Toast, Modals
- ✅ Sections: 10+ (common, navigation, login, settings, inventory, history, etc.)
- ✅ Strings: 150+ clés traduites

### 🔮 Prévu pour les Prochaines Versions

- [ ] Édition en masse d'images
- [ ] Galerie d'images pour sélection
- [ ] Support de davantage de langues (Espagnol, Allemand, etc.)
- [ ] Optimisation de stockage des images (compression, thumbnails)
- [ ] Export/Import de profils de localization

### ⚠️ Notes de Compatibilité

- **Recommandé**: Supprimer le cache du navigateur après la mise à jour
- **Dossier personnages**: Doit être accessible en écriture par l'application
- **Fichiers i18n**: Vérifier que `wwwroot/i18n/` contient bien `en.json` et `fr.json`

---

**Date de Release**: Décembre 17, 2025  
**Version**: 0.2.0  
**Auteur**: Thorinval

---

## Comment ajouter une nouvelle entrée

### Option 1 : Automatiquement (Recommended)

Exécutez le script PowerShell fourni pour ajouter automatiquement une nouvelle version :

```powershell
.\scripts\Update-ReleaseNotes.ps1 -Version "0.3.0" -Date "Janvier 2026"
```

### Option 2 : Manuellement

1. Mettez à jour la version dans `appsettings.json` :

   ```json
   "AppInfo": {
     "Version": "0.3.0",
     ...
   }
   ```

2. Copiez le template ci-dessous et insérez-le **après la ligne `---`** et **avant** la version précédente :

```markdown
## Version X.Y.Z (Mois Année)

### ✨ Nouvelles Fonctionnalités

- Fonctionnalité 1
- Fonctionnalité 2

### 🔧 Améliorations Techniques

- Amélioration 1
- Amélioration 2

### 🐛 Corrections de Bugs

- Bug 1 corrigé
- Bug 2 corrigé

### 📋 Changements de l'Interface Utilisateur

| Élément | Avant | Après |
|---------|-------|-------|
| ... | ... | ... |

---

**Date de Release**: [DATE]  
**Version**: X.Y.Z  
**Auteur**: [AUTEUR]
```

1. Complétez le template avec les changements de cette version

### Synchronisation avec appsettings.json

Le fichier `.csproj` et `appsettings.json` doivent rester synchronisés :

- Mise à jour de version → Mettre à jour `appsettings.json` (`AppInfo:Version`)
- Le `.csproj` prendra la version de `appsettings.json` automatiquement
- Ajouter une entrée dans `RELEASE_NOTES.md`

### Checklist avant chaque release

- [ ] Mettre à jour la version dans `appsettings.json`
- [ ] Vérifier que `.csproj` a le bon numéro (doit être synchronisé)
- [ ] Ajouter une nouvelle entrée dans `RELEASE_NOTES.md`
- [ ] Tester la version dans `appsettings.json` qui s'affiche dans "À propos"
- [ ] Committer les changements avec tag git : `git tag -a v0.3.0 -m "Version 0.3.0 - Description"`
- [ ] Pusher : `git push origin v0.3.0`
