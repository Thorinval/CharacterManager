# Character Manager

> **Version actuelle**: 0.14.0

---

## 0.14.0 (04 Janvier 2026)

✨ - Modèle: Suppression de l'attribut `Description` du modèle Personnage (non utilisé)

✨ - BDD: Migration EF Core pour la suppression de la colonne Description de la table Personnages

✨ - UI/UX: Ajout d'une modale de sélection des capacités pour les personnages avec liste scrollable et recherche

✨ - Modale: `PersonnageCapacitesModal` - Sélection et gestion des capacités assignées à un personnage

✨ - Service: Méthode `UpdateCapacitesAsync` pour la mise à jour des capacités d'un personnage en base

✨ - Architecture: Conversion de `ManageUsers` en modale `ManageUsersModal` accessible depuis la top bar (admin uniquement)

✨ - Architecture: Conversion de `ImportExportPML` en modale `ImportExportPmlModal` accessible depuis la top bar

✨ - Architecture: Conversion de `Settings` en modale `SettingsModal` avec injection du ModalService

✨ - Navigation: Ajout d'icônes Material Symbols dans la top bar (settings, admin_panel_settings, cloud_upload)

✨ - UI/UX: Toutes les modales affichent désormais l'icône Material Symbols correspondante dans leur en-tête

✨ - UI/UX: Ajout de titres localisés "Notes de version" et "Feuille de route" dans les modales Changelog/Roadmap

🔧 - Navigation: Suppression des entrées de menu pour "Import/Export PML" et "Gestion des utilisateurs" (déplacées en top bar)

🔧 - Refactorisation: Suppression des boutons close redondants dans les modales (ModalHost gère déjà la fermeture)

🔧 - Modal: Simplification de l'en-tête d'ImportExportPmlModal (titre affiché une seule fois)

🔧 - Code: Nettoyage des méthodes `Close()` inutilisées dans les composants modaux

## 0.13.0 (03 Janvier 2026)

✨ - Modèle: Ajout des attributs `HasRelation` (bool) et `NivRelation` (int) au modèle Personnage pour les mercenaires

✨ - BDD: Migration EF Core pour l'ajout des colonnes HasRelation et NivRelation à la table Personnages

✨ - UI/UX: Système complet de relations mercenaires (affichage lecture, édition avec bounds 1-30)

✨ - Import/Export: Support des relations HasRelation/NivRelation dans le format d'import/export PML

✨ - Modale: Consolidation - Une seule modale `DetailPersonnageModal` pour tous les accès (image + édition)

✨ - UI/UX: Capacités affichées en tuiles style Home (dark theme #273449, icônes bleues, grille 9/ligne)

✨ - UI/UX: Pièces Maison Lucie affichées en tuiles style Home (alignement gauche, 320px fixes)

🔧 - Refactorisation: Suppression de la page dupliquée `DetailPersonnage.razor` (conservée uniquement la modale)

🔧 - Inventaire: Clic "Modifier" ouvre maintenant la modale directement en mode édition (paramètre StartInEdit)

🔧 - Inventaire: Clic image affiche la modale de détail en mode lecture

🔧 - Modal: Contrainte max-height 350px sur l'image du personnage dans la modale

🔧 - CSS: Page Capacités calée à gauche, conteneur full-width (sans max-width)

🔧 - CSS: Page Maison Lucie utilise classes hub pour cohérence visuelle avec Home

## 0.12.2 (03 Janvier 2026)

🐛 - Export/Import: Correction du format d'export historique des classements (XML → PML)

🐛 - UI/UX: Fix style du bouton "Importer" Inventaire (btn-outline-info → btn-info)

🐛 - UI/UX: Suppression de l'alerte bloquante après édition de personnage

🐛 - UI/UX: Correction largeur chips d'affection Lucie (affichage 3 chiffres)

✨ - Architecture: Migration complète éditeur de templates vers page Templates (380+ lignes)

📦 - Documentation: Tous fichiers .md déplacés dans dossier docs/

🔧 - Standardisation: Format PML devient standard pour tous les exports

## 0.12.1 (03 Janvier 2026)

✨ - Architecture: DLL `CharacterManager.Resources.Personnages` avec 126 images embarquées

✨ - API: Endpoint `/api/resources/personnages/{personnage}/{fichier}` pour servir les images

✨ - Services: `PersonnageResourceManager` pour accès aux ressources

✨ - Utilities: `PersonnageImageUrlHelper` pour génération d'URLs

📊 - 86 personnages uniques organisés avec ~130 MB d'images

🏗️ - Architecture: Images organisées par dossier de personnage

## 0.12.0 (02 Janvier 2026)

🏗️ - Architecture: Création du projet `CharacterManager.Resources.Interface` pour intégrer les images d'interface

🏗️ - Architecture: Service `InterfaceResourceManager` pour accéder aux ressources embedded

🔧 - API: Ajout du contrôleur `ResourcesController` pour servir les images depuis la DLL

📦 - Configuration: Images d'interface packagées comme ressources embedded dans l'assembly

🗺️ - Documentation: Plan de migration des ressources pour les versions futures

**En cours**: Migration progressive des fichiers images depuis `wwwroot/images/interface` vers `CharacterManager.Resources.Interface/Images`

## 0.11.1 (02 Janvier 2026)

✨ - Création de la page "Capacités" avec gestion CRUD complète des capacités

✨ - Ajout d'une tuile "Capacités" sur le tableau de bord avec compteur

✨ - Import/Export des capacités via fichier PML

✨ - 28 capacités pré-importées avec icônes Bootstrap Icons

🔧 - Refactorisation de la méthode ExportPmlAsync avec classe PmlExportOptions pour meilleure extensibilité

🔧 - Ajout de constantes pour les types d'export (INVENTORY, TEMPLATES, BEST_SQUAD, HISTORIES, LEAGUE_HISTORY, CAPACITES)

🔧 - Remplacement de toutes les icônes Material Symbols par Bootstrap Icons dans le tableau de bord

🔧 - Réduction de la largeur minimale des hub-cards de 600px à 500px pour layout plus responsive

🐛 - Migration appliquée : Correction colonne PuissanceTotale dans table HistoriquesClassement

🐛 - Correction du rendu des icônes Bootstrap Icons avec le format correct bi bi-{iconname}

## 0.11.0 (01 Janvier 2026)

✨ - Création page "Maison de Lucie" avec affichage complet des pièces et de l'affection

✨ - Ajout entrée menu "Maison de Lucie" avec navigation

✨ - Tuile d'accueil "Maison de Lucie" avec aperçu rapide des pièces

🔧 - Harmonisation des largeurs de tuiles sur le tableau de bord

🔧 - Synchronisation des icônes de menu avec les pages correspondantes

🔧 - Normalisation du système de grille CSS pour le layout responsive

🐛 - Correction tests unitaires pour les méthodes Lucie House

## 0.10.3 (01 Janvier 2026)

🔧 - Alimentation roadmap à partir d'un fichier

## 0.10.2 (31 Décembre 2025)

🐛 - fix import des pieces

✨ - Suppression d'un classement de l'historique

## 0.10.1 (28 Décembre 2025)

✨ - Refonte fenetres modales

## 0.10.0 (28 Décembre 2025)

✨ - Refonte layout

🔨 - En cours - historique de classement
🔨 - En cours - page d'accueil

## 0.9.2 (26 Décembre 2025)

✨ - Ajout init par fichier PML par défaut lorsque l'inventaire est vide

✨ - Ajout Export fichier PML pour config

✨ - L'inventaire est triable par puissance également. Tri par défaut puissance décroissante

✨ - L'inventaire peut filtrer par catégorie (Commandants, Mercenaires, Androides et Lucie rooms)

🐛 - Fix image du personnage dans l'écran détail n'apparait pas pour les mercenaires non sélectionnés

🐛 - Fix mineurs

## 0.9.1 (22-24 Décembre 2025)

✨ - Renommage pages et menu

✨ - Création d'un classement via la page classement avec une fenetre modale

✨ - Ajout Roadmap

✨ - Ajout Releases notes

✨ - Ajout localisation notes de versions

✨ - Script d'automatisation

🔧 - Reprise de chaines en dur par des constantes

🐛 - Fix chargement des puissances dans les pieces de lucy

🐛 - Top commandant ne tenait pas compte du rang

🐛 - Meilleure escouade, le commandant affiché est Alexa au lieu de Dragana qui est la meilleure. le score est bien calculé

## 0.9.0 (21 Décembre 2025)

✨ - Implémentation complète de la Maison de Lucie

✨ - Export Lucie inclus dans l'inventaire

✨ - Calcul de puissance incluant les bonus de Lucie

✨ - Nouvelle page dédiée à la gestion de la maison

🔧 - Mise à jour des tests unitaires

🔧 - Ajout d'un script de gestion de version automatique

🐛 - Fix affichage détail des personnages

🐛 - Fix localisation

🐛 - Correction affichage androïdes et top commandant

## 0.8.0 (21 Décembre 2025)

🐛 - Corrections mineures et optimisations diverses

🐛 - Corrections majeures des localisations (français/anglais)

🐛 - Correction des warnings de localisation

✨ - Amélioration de la gestion multilingue

🐛 - Correction import/export avec localisation

## 0.7.0 (20-21 Décembre 2025)

✨ - Correction de l'affichage du seuil par rapport au max escouade

✨ - Amélioration des icônes dans les templates

🔧 - Corrections des tests unitaires

🐛 - Fix emplacement des cards

🐛 - Fix détail incorrect

## 0.6.0 (20 Décembre 2025)

🎯 - Refonte complète des imports/exports vers le nouveau format PML

🎯 - Format XML standardisé pour l'application

✨ - Meilleure compatibilité et extensibilité

🔧 - Nouvelle fonction limite de puissance (en travaux)

🔧 - Refonte des pages de gestion

## 0.5.0 (19-20 Décembre 2025)

✨ - Refonte des pages

✨ - Adaptation des traductions

✨ - Déplacement du bouton paramètres en haut à gauche

✨ - Correction de la casse du titre historique

🔧 - Mise à jour des références vers le nouveau dossier interface

🔧 - Corrections Docker

🔧 - Fix workflow environment et notifications Slack

🔧 - Ajout d'un job de vérification des secrets

🔧 - Guide de setup CI/CD

🔧 - Création automatique du repo distant

🔧 - Corrections YAML de build

## 0.4.0 (19 Décembre 2025)

🎯 - Nouvelle navigation complète

🎯 - Mise en page revue de toute l'application

✨ - Layout modernisé

✨ - Meilleure organisation des pages

✨ - Refonte de l'ergonomie générale

## 0.3.0 (17-18 Décembre 2025)

✨ - Déploiement Google Cloud (GCP)

✨ - Configuration pour cloud

✨ - Scripts de déploiement automatisés

✨ - Ajout du champ puissance pour les personnages

✨ - Upload d'image select

✨ - Calcul de puissance intégré

✨ - Système d'upload d'images pour personnages

✨ - Gestion des ressources visuelles

✨ - Release notes automatiques

✨ - Génération automatisée de la documentation de version

🔧 - Localisation améliorée

## 0.2.0 (15-16 Décembre 2025)

✨ - Système de profils utilisateur

✨ - Authentification complète

✨ - Gestion des sessions

✨ - Localisation multilingue complète (Français et Anglais)

✨ - Page de classement localisée

✨ - Amélioration majeure de l'historique des classements

✨ - Interface template revue

🔧 - Réorganisation : CSS séparé, CS séparé (meilleure architecture)

🐛 - Correction du login

🐛 - Correction de l'affichage des classements

🐛 - Correction des warnings

## 0.1.0 (13-16 Décembre 2025)

✨ - Page inventaire complète

✨ - Page détail des personnages

✨ - Intégration SQLite

✨ - Système d'import de personnages

✨ - Système de templates d'escouade

✨ - Drag-and-drop pour organisation

✨ - Nouvelle page Meilleur Escouade

✨ - Ajout de la puissance dans l'interface

✨ - Implémentation des méthodes de calcul de puissance

✨ - Dockerisation de l'application

✨ - Refonte de la page "À propos"

✨ - Ajout de l'export de données

✨ - Mise en place des tests unitaires

✨ - Mode adulte (filtrage de contenu)

🔧 - Clean up : Suppression des binaires

🔧 - Ajout d'un `.gitignore` approprié

🔧 - Gestion propre du versioning Git
