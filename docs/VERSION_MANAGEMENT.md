## Gestion automatique des versions

Source de vérité : `CharacterManager/appsettings.json` → `AppInfo.Version`

### Fonctionnement

- **Hook Git (pre-commit)** : à chaque commit, le numéro **patch** (dernier segment) est incrémenté automatiquement à partir de `appsettings.json`, puis synchronisé dans `CharacterManager.csproj` (`<Version>` et `<InformationalVersion>`).
	- Exemple : `0.8.1` → `0.8.2`.
- **Script manuel** `Increment-Version.ps1` : permet d'incrémenter `major | minor | patch` (défaut : patch) tout en gardant `appsettings.json` comme source.

### Scripts / fichiers

- `.git/hooks/pre-commit` : wrapper multi-OS (appelle PowerShell sur Windows, Python sur Linux/Mac).
- `.git/hooks/pre-commit.ps1` : incrément patch depuis `appsettings.json`, met à jour aussi `CharacterManager.csproj` et ajoute les fichiers au commit.
- `Increment-Version.ps1` : incrément manuel (major/minor/patch) avec proposition de commit.

### Schéma de versionnement (SemVer)

```
MAJOR.MINOR.PATCH
```

- **MAJOR** : rupture
- **MINOR** : ajout rétro-compatible
- **PATCH** : correctifs (auto-incrémenté à chaque commit)

### Désactivation temporaire

```
git commit --no-verify -m "message"
```

### Notes

- La version affichée par l'application provient de `appsettings.json` (AppInfo.Version).
- Le `.csproj` est toujours synchronisé automatiquement sur cette valeur.
