# Changelog

Toutes les versions notables de TraceZero. Format inspiré de [Keep a Changelog](https://keepachangelog.com/fr/).

## [0.1.2] — 2026-08-17

### Ajouté
- **Installeur `.exe`** (Inno Setup) : `TraceZeroSetup-<version>.exe` — installation par utilisateur sans
  admin (choix « pour tous » possible), raccourci menu Démarrer, désinstallation propre. En plus du
  portable et de winget.
- **Icône de l'application** : le logo TraceZero est désormais l'icône de l'exe, du raccourci, de la barre
  des tâches et de l'installeur (fini l'icône .NET générique).

## [0.1.1] — 2026-08-17

### Corrigé
- **Langue au premier lancement** : l'application suit désormais la **langue de Windows**
  (fr/de/es/en, repli anglais pour les autres langues) au lieu de démarrer systématiquement en français.
  Tant qu'aucune langue n'est choisie explicitement dans les Paramètres, l'app continue de suivre l'OS ;
  un choix manuel reste persistant.

## [0.1.0] — 2026-08-17

Première version publique. Distribution **portable** (aucune installation), **hors-Store**, gratuite et
open source (MIT).

### Nettoyage & maintenance
- **Nettoyage Windows** : fichiers temporaires, rapports de plantage, WER, caches, Corbeille — règles
  user-scoped, tailles réelles, prévisualisation par risque, suppression validée par une couche de sécurité.
- **Confidentialité** : « ce que Windows sait encore » (documents récents, RunMRU, chemins tapés,
  recherches, UserAssist…), chaque trace expliquée, nettoyage registre allowlisté.
- **Navigateurs** : Chrome/Edge/Brave/Vivaldi/Chromium/Opera/Opera GX/Firefox. Caches SAFE + historique,
  cookies et sessions **en option** (jamais cochés par défaut). Historique Firefox par suppression
  **ciblée** (favoris préservés). Mots de passe et favoris jamais touchés.
- **Espace disque**, **Doublons** (SHA-256), **Applications & démarrage**, **Effacement sécurisé**
  (fichiers + espace libre, avertissement SSD honnête), **Analyse NTFS** (Expert, lecture seule).

### Système
- **Santé système** : santé disque (WMI), **RAM** (modules DDR, XMP/EXPO), **activité en direct**
  (CPU + RAM utilisée), **top consommateurs mémoire**, **indice de performance Windows** (WinSAT),
  impact au démarrage mesuré. Tout en lecture seule, jamais de score inventé.
- **Pilotes** (inventaire lecture seule), **Mises à jour logicielles** via winget.

### Sûreté & confidentialité
- Toute suppression passe par `ISafePathValidator` (refus prouvé par tests).
- L'application n'est **jamais** admin ; l'élévation passe par un helper séparé.
- **Local-first** : aucune télémétrie, aucune publicité, aucune donnée envoyée.
- Protection/Restauration (sauvegarde des traces registre avant nettoyage).

### Divers
- Interface **multilingue** (fr/en/de/es), accessibilité (focus clavier, AutomationProperties).
- Écran **À propos / Mentions** (version, licence, avertissement, confidentialité, notices tierces).
- Soutien **PWYW** (au prix que vous voulez), sans blocage de fonctionnalité.

### Limites connues (voir `KNOWN_LIMITATIONS.md`)
- Binaire **non signé Authenticode** → avertissement SmartScreen « Éditeur inconnu » (voir
  `docs/download.md`). Vérifiez l'empreinte SHA-256.
- **Auto-update désactivé** dans cette version (mises à jour via winget ou téléchargement manuel).
- Installeur MSI/EXE et validation en VM propre : à venir.

[0.1.2]: https://github.com/Sharkade02/tracezero/releases/tag/v0.1.2
[0.1.1]: https://github.com/Sharkade02/tracezero/releases/tag/v0.1.1
[0.1.0]: https://github.com/Sharkade02/tracezero/releases/tag/v0.1.0
