# Images de Personnages

Ce dossier contient les images des personnages organisées par sous-dossier.

## Structure

Chaque personnage a son propre dossier avec jusqu'à 4 types d'images :

```
Images/
├── NomPersonnage/
│   ├── nompersonnage.png                 (image détaillée)
│   ├── nompersonnage_header.png          (optionnel - image d'en-tête)
│   ├── nompersonnage_small_portrait.png  (petit portrait)
│   └── nompersonnage_small_select.png    (portrait sélectionné)
```

## Migration depuis wwwroot/images/personnages

Pour migrer les images existantes :

1. Créer un dossier pour chaque personnage (première lettre en majuscule)
2. Copier les 4 fichiers du personnage dans son dossier
3. Les images seront embarquées automatiquement dans la DLL lors de la compilation

## Exemples

- `Images/Alexa/alexa.png`
- `Images/Alexa/alexa_header.png`
- `Images/Alexa/alexa_small_portrait.png`
- `Images/Alexa/alexa_small_select.png`

## Contenu Adulte

Pour les images adultes, créer un sous-dossier `Adult` :

```
Images/
├── Adult/
│   ├── NomPersonnage/
│   │   ├── nompersonnage.png
│   │   └── ...
```
