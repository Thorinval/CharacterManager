# Version 0.12.0 - Plan de mise à jour

## Objectif
Migrer les images d'interface du répertoire `wwwroot/images/interface` vers une DLL dédiée `CharacterManager.Resources.Interface`.

## Structure

### Créée
- ✅ Projet Class Library: `CharacterManager.Resources.Interface`
- ✅ Service: `InterfaceResourceManager` pour accéder aux ressources
- ✅ Configuration: Fichiers images comme ressources embedded dans le projet

### À faire

#### Phase 1: Migration des fichiers images
1. Copier tous les fichiers images de `wwwroot/images/interface` vers `CharacterManager.Resources.Interface/Images`
   - best.png
   - btn_retour.png
   - capacités.png
   - default_portrait.png
   - droite.png
   - e58ed60c-29be-4155-948f-03dc4771c785.png
   - factions.png
   - faction_hommelibre.png
   - faction_pacificateur.png
   - faction_syndicat.png
   - favicon.png
   - fist.png
   - fondheader.png
   - fond_puissance.png
   - gauche.png
   - parametres.png
   - parametres_small.png
   - piece_bar.png
   - piece_cafe.png
   - puissance.png
   - rarete.png
   - rarete_commun.png
   - rarete_r.png
   - rarete_sr.png
   - rarete_ssr.png

#### Phase 2: Créer un endpoint pour servir les images
- Ajouter un contrôleur ou middleware pour servir les images depuis la DLL
- Exemple: `/api/resources/interface/{fileName}`

#### Phase 3: Mettre à jour les références dans le code
- Remplacer les chemins statiques (`/images/interface/`) par des appels au service
- Tester chaque page affichant des images

#### Phase 4: Tests
- Vérifier que toutes les images s'affichent correctement
- Tests unitaires pour le `InterfaceResourceManager`
- Tests d'intégration pour les endpoints

#### Phase 5: Nettoyage
- Supprimer le répertoire `wwwroot/images/interface` une fois migré
- Vérifier qu'aucune référence à l'ancien chemin ne reste

## Avantages
- 📦 Images packagées avec l'application
- 🔒 Contrôle centralisé des ressources
- 📈 Extensibilité : permet d'ajouter d'autres types de ressources (polices, sons)
- 🚀 Facilite le déploiement en conteneur

## Ressources créées
- `CharacterManager.Resources.Interface.csproj` - Configuration du projet
- `InterfaceResourceManager.cs` - Service d'accès aux ressources

## Commandes Git
```bash
git add CharacterManager.Resources.Interface/
git commit -m "v0.12.0: Créer projet de ressources interface"
```
