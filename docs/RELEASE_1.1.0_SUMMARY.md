# 🎉 Character Manager v1.1.0 - Notes de Release Courtes

> **Publié le 25 janvier 2026**  
> A comprehensive update focused on stability, user experience, and intelligent data import.

---

## ⚡ Quoi de neuf ?

### 🔄 Import PML Assisté [NEW]
Un nouvel assistant **3 étapes** pour importer vos données en toute confiance :

1. **Prévisualisation** - Vérifiez avant d'importer
2. **Résolution de conflits** - Choisissez quelle donnée conserver
3. **Rapport détaillé** - Vérifiez ce qui a été importé

**Avantages** : Plus d'erreurs silencieuses, plus de contrôle ! ✅

---

### 🧹 Nettoyage des Doublons [NEW]
Un nouvel outil admin pour corriger les doublons automatiquement.

👉 Accessible via : **Admin → Cleanup Duplicates** (après import si nécessaire)

---

### ✏️ Édition de la Maison de Lucie [NEW]
Vous pouvez maintenant éditer les pièces directement dans l'app !

- Cliquez sur une pièce pour la modifier
- Changez le niveau, puissance tactique, puissance stratégique
- Sauvegardez automatiquement avec historisation

---

### 💪 Puissance Réelle [IMPROVED]
Les commandants affichent maintenant leur **puissance réelle** = Puissance + (Rang × 20)

- Visible partout : Inventaire, Escouade, Classements
- Format : **1050 (1250)** = Base / Réelle

---

### 🎨 Interface Uniformisée [IMPROVED]
Toutes les pages ont maintenant un design cohérent et moderne.

- Headers transparents avec bordure subtile
- Meilleur espacement
- Icônes cohérentes

---

## 🔧 Sous le Capot

- ✅ **78/78 tests** - Tous réussis
- 📊 **Logging amélioré** - Meilleur diagnostic
- 🐛 **Corrections** - JSON localisation, HTML structure
- 📚 **Documentation** - À jour et complète

---

## 🚀 Comment Mettre à Jour ?

### Windows
1. Télécharger `CharacterManager-1.1.0-Setup.exe`
2. Exécuter l'installeur (vos données sont préservées)
3. Relancer l'app

### Docker
```bash
docker-compose pull
docker-compose up -d
```

---

## ⚠️ Points Importants

- 📁 **Backup** votre `charactermanager.db` avant le nettoyage des doublons
- 🆔 **Admin** doit changer son mot de passe immédiatement
- 📝 Format import : `.pml` uniquement (XML valide)
- ✅ **Zéro perte de données** dans la migration v1.0.0 → v1.1.0

---

## 🆕 Prochaines Versions

- v1.2 : Refonte du classement + UX améliorations
- v1.3+ : Nouveaux graphiques, multilangue étendu, perf optim

Voir [ROADMAP.md](ROADMAP.md) pour le plan complet.

---

## 📞 Besoin d'Aide ?

- 🐛 Bug ? [Ouvrir une issue](https://github.com/Thorinval/CharacterManager/issues)
- 💡 Suggestion ? [Discussion](https://github.com/Thorinval/CharacterManager/discussions)
- 📖 Docs ? [Documentation complète](DOCUMENTATION.md)

---

**Merci de tester v1.1.0 et de nous faire part de vos retours ! 🙌**
