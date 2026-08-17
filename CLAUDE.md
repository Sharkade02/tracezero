# CLAUDE.md — TraceZero

> Guide de contexte pour reprendre le développement à tout moment.
> Sources de vérité détaillées : `PHASE_STATUS.md` (avancement), `DECISIONS.md` (ADR),
> `KNOWN_LIMITATIONS.md` (limites honnêtes), `TRACEZERO_MASTER_PROMPT_CLAUDE_CODE.md` (cahier de mission).

## Le produit

TraceZero = logiciel Windows de **nettoyage / confidentialité / gestion d'espace disque / maintenance**,
qui doit rivaliser réellement avec CCleaner et PrivaZer. Positionnement : nettoyeur sûr + inspecteur de
confidentialité + nettoyeur navigateurs + gestionnaire d'espace + effacement sécurisé + détecteur de
doublons + gestionnaire apps/démarrage. **Local-first, privacy-first, zéro pub, zéro dark pattern.**

Tagline : *« Voyez ce qui reste. Nettoyez ce que vous choisissez. »*

**Règle absolue (§0) : ce n'est pas une démo.** Aucun bouton décoratif, aucun résultat mocké, aucune
valeur inventée. Dans une phase `DONE`, aucun `TODO` / `NotImplementedException` / stub ne subsiste.
Si une fonction est trop dangereuse pour être fiable, livrer une version read-only honnête et documenter
la limite dans `KNOWN_LIMITATIONS.md` — jamais simuler.

## Stack & langue

- **.NET 10** + **WPF** + **MVVM** (CommunityToolkit.Mvvm) + **Generic Host** (Microsoft.Extensions.Hosting/DI/Logging).
- Windows 10/11 x64. Persistance : SQLite (`TraceZero.Persistence`).
- **Code en anglais, UI en français** par défaut ; architecture prête pour EN/DE/ES.

## Architecture (solution `TraceZero.slnx`)

Couches portables (`net10.0`) vs Windows (`net10.0-windows`) — voir ADR-0001/0002 :

- `TraceZero.Domain` — modèles purs, aucune dépendance.
- `TraceZero.Application` — interfaces de services, abstractions.
- `TraceZero.Engine` — moteur scan/clean, **`ISafePathValidator`**, moteur de règles (portable, testable).
- `TraceZero.Windows` / `TraceZero.Storage` — providers Windows (registre, WMI, drives).
- `TraceZero.Browsers` — providers navigateurs.
- `TraceZero.Persistence` — historique local SQLite, licences.
- `TraceZero.Updater` — updater signé (à faire).
- `TraceZero.Elevated` — **helper admin séparé** `TraceZero.Elevated.exe` (voir ADR-0006).
- `TraceZero.App` — WPF, composition root uniquement (jamais de logique de nettoyage en code-behind).

## Règles de sécurité — NON NÉGOCIABLES

- **Toute suppression passe par `ISafePathValidator`** (posé dès Phase 0, prouvé par `SafetyTests`).
  Refus prouvés : `C:\`, racine de volume, profil utilisateur, dossiers système/personnels, wildcard,
  traversal, UNC, reparse points/jonctions.
- L'énumération (`SafeFileEnumerator`) **ne suit jamais** jonctions/liens et ignore l'inaccessible.
- Fichiers verrouillés : **signalés, jamais forcés**.
- Suppressions destructives → Corbeille (**réversible**) quand possible ; jamais auto-sélectionnées.
- **L'app principale n'est jamais admin.** L'élévation passe par `TraceZero.Elevated.exe` (manifeste
  `requireAdministrator`, single-shot, IPC fichiers JSON, vocabulaire fermé, revalidation via
  `ElevatedSafePathValidator` « refus par défaut »). Le helper ne fait **jamais** confiance à l'UI.
- **`WarningsAsErrors=nullable`** (ADR-0005). Release doit être 0 warning.
- Historique : **jamais de chemin personnel** enregistré (§39).

## Build & run

⚠️ **Pré-requis : SDK .NET 10.** La machine n'avait que le SDK .NET 8 — vérifier avec `dotnet --list-sdks`.
Installer via `winget install --id Microsoft.DotNet.SDK.10 -e`.

```powershell
dotnet build -c Release              # doit être 0 warning
dotnet test                          # 124 tests au dernier point (2026-08)
dotnet run --project src\TraceZero.App\TraceZero.App.csproj   # lance l'appli WPF
build\scripts\release.ps1            # pipeline release (Phase 27) : build/test Release + publish + SHA-256
```

## Où en est-on ? (voir `PHASE_STATUS.md` pour le détail)

**93 tests au total. Tous les onglets sont fonctionnels** : Accueil · Nettoyage · Confidentialité ·
Navigateurs · Espace disque · Doublons · Applications · Automatisation · Historique · Paramètres · Soutenir.
Plus aucune page placeholder.

**DONE (21)** : 0 Bootstrap · 1 UI Shell + Design System · 2 Scan Engine · 3 Nettoyage Windows ·
5 Privacy Inspector · 6 Cleaning Plan + Exclusions + Historique · 7 Protection/Backup/Restore ·
8 Analyse NTFS · 9 Effacement sécurisé · 10 Disk Space · 11 Duplicate Finder · 12 Applications & Démarrage ·
14 Driver Health · 15 Automatisation · 16 Historique/Stats · 17 Supporter/PWYW · 20 Élévation ·
21 Localisation · 22 Accessibilité · 27 Qualité release · 28 Moniteur système.

**IN_PROGRESS (2)** :
- **Phase 4 — Navigateurs** : caches SAFE OK ; reste History/cookies/sessions + Opera (différés).
  Peut réutiliser l'infra backup/restore de la Phase 7.
- **Phase 24 — Tests de sécurité** : suite `SafetyTests` amorcée, à étoffer.

**Localisation (Phase 21, DONE)** : toute l'UI en fr/en/de/es, bascule live. Ajouter une chaîne = clé dans
les 4 `Localization/Strings.*.xaml` ; en XAML `{DynamicResource Ma.Cle}`, en code `Localizer.Get("Ma.Cle")`
ou `Localizer.Format("Ma.Cle", args)`. Descriptions de règles = `NameKey`/`DescriptionKey` sur le modèle.

**Design system (Phase 1)** : composants réutilisables — `Skeleton` (Shared.xaml), `IToastService`
(toasts superposés, coin bas-droit), `IDialogService` (`ConfirmAsync` awaitable, modale thématisée).
Hôtés dans `MainWindow`, exposés par `ShellViewModel` (`Toasts`, `Dialog`). Câblés à de vraies actions.

**Protection/Restauration (Phase 7)** : `IRegistryBackupService` (Windows) capture/restaure des sous-clés
HKCU ; `IProtectionVault` (Persistence, table `restore_points`) persiste les points de restauration ;
le nettoyage Confidentialité sauvegarde les traces registre avant de les effacer ; page **Restauration**.
Réversibilité honnête via l'enum `Reversibility`.

## Ce qu'il reste à développer (NOT_STARTED)

| Phase | Nom | Notes |
|------:|-----|-------|
| 13 | Software Updater | |
| 18 | Updater signé | `TraceZero.Updater` |
| 19 | Installateur / Portable / MSIX | Distribution signée + Microsoft Store |
| 23 | Performance | |
| 25 | Golden dataset | |
| 26 | Tests VM | |

**Prochaine étape recommandée** (backend/qualité) : Phase 14 (Driver Health read-only), Phase 21
(localisation), Phase 22 (accessibilité), puis 13/18/19. La Phase 4 (privacy navigateurs) peut réutiliser
l'infra backup/restore.
