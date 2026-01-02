# Release Notes - Character Manager

> **Version actuelle**: 0.12.0

---

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

## 0.9.11 (26 Décembre 2025)

✨ - Ajout init par fichier PML par défaut lorsque l'inventaire est vide

✨ - Ajout Export fichier PML pour config

## 0.9.10 (26 Décembre 2025)

✨ - Ajout portrait Scarlett

✨ - L'inventaire est triable par puissance également. Tri par défaut puissance décroissante

✨ - L'inventaire peut filtrer par catégorie (Commandants, Mercenaires, Androides et Lucie rooms)

🐛 - Fix image du personnage dans l'écran détail n'apparait pas pour les mercenaires non sélectionnés

🐛 - Fix mineurs

## 0.9.9 (24 Décembre 2025)

✨ - Renommage pages et menu

✨ - Création d'un classement via la page classement avec une fenetre modale

🔧 - Reprise de chaines en dur par des constantes

## 0.9.8 (23 Décembre 2025)

✨ - Ajout Roadmap

✨ - Ajout Releases notes

✨ - Ajout localisation notes de versions

✨ - Script d'automatisation

🐛 - Fix chargement des puissances dans les pieces de lucy

🐛 - Top commandant ne tenait pas compte du rang

🐛 - Meilleure escouade, le commandant affiché est Alexa au lieu de Dragana qui est la meilleure. le score est bien calculé
