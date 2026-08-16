# PHASE_STATUS

Source de vérité de l'avancement. Statuts : `NOT_STARTED`, `IN_PROGRESS`, `BLOCKED`, `DONE`.

> Règle : une phase n'est `DONE` que si sa Definition of Done est vérifiée, les tests passent,
> et aucun `TODO` / `NotImplementedException` / mock ne subsiste dans le code de production concerné.

| Phase | Nom | Status | Tests | Notes |
|------:|-----|--------|-------|-------|
| 0 | Bootstrap du projet | DONE | 29 ✅ | Solution 18 projets, DI host + shell WPF (nav + thèmes) + safety layer. Build Debug/Release OK, app lance sans crash |
| 1 | UI Shell + Design System | IN_PROGRESS | — | Card/boutons/NavButton/RiskChip/Progress/empty+result states posés ; reste composants (toast, modal, skeleton) |
| 2 | Scan Engine | DONE | 14 ✅ | Moteur async parallèle borné, progress, cancellation, erreurs isolées, tailles réelles |
| 3 | Nettoyage Windows standard | DONE | ✅ | Règles user-scoped réelles (TEMP, CrashDumps, WER, INetCache, thumbnails) + Corbeille. Windows\Temp différé (voir KNOWN_LIMITATIONS) |
| 4 | Navigateurs | IN_PROGRESS | 5 ✅ | Détection Chrome/Edge/Brave/Vivaldi/Chromium/Firefox + profils + état exécution ; **caches SAFE** nettoyés (connexions préservées). History/cookies/sessions + Opera différés |
| 5 | Privacy Inspector Windows | DONE | 2 ✅ | Page « Ce que Windows sait encore » : RecentDocs, RunMRU, TypedPaths, WordWheelQuery, UserAssist, ComDlg32 MRU + Documents récents/Jump Lists. Chaque trace expliquée, nettoyage registre allowlisté + fichiers validés |
| 6 | Cleaning Plan + Safety layer | DONE | 3 ✅ | `CleaningPlan`/preview + **exclusions** (dossier/catégorie, JSON, appliquées au scan) + **journal d'historique** SQLite |
| 7 | Protection / Backup / Restore | NOT_STARTED | — | |
| 8 | Analyse NTFS avancée | NOT_STARTED | — | Mode Expert |
| 9 | Effacement sécurisé | NOT_STARTED | — | HDD/SSD |
| 10 | Disk Space Manager | DONE | 2 ✅ | Vue lecteurs (capacité/utilisé/libre + barre) ; recherche de gros fichiers (seuil 100 Mo–2 Go) ; ouvrir dans l'Explorateur ; envoi à la Corbeille (réversible), jamais auto-sélectionné |
| 11 | Duplicate Finder | DONE | 3 ✅ | Pipeline taille → hash partiel → SHA-256 complet ; groupes, « garder le plus récent », garde-fou keep-one, envoi Corbeille réversible + validation |
| 12 | Applications & Démarrage | DONE | 4 ✅ | Apps installées (registre Uninstall, recherche, ouvrir, désinstaller via éditeur) ; démarrage (Run HKCU activable/désactivable réversible + backup, HKLM/dossier en lecture seule) |
| 13 | Software Updater | NOT_STARTED | — | |
| 14 | Driver Health / Updater | NOT_STARTED | — | |
| 15 | Automatisation | DONE | 1 ✅ | Profils Sûr/Confidentialité, déclencheurs hebdo/mensuel/ouverture session via Planificateur (schtasks), mode headless `--autoclean` ; jamais de REVIEW en auto |
| 16 | Historique & Statistiques | DONE | ✅ | Page Historique réelle : total libéré, nb nettoyages, dernier, journal. Local, sans chemins personnels |
| 17 | Supporter / PWYW | DONE | 4 ✅ | Page Soutenir (PWYW 10/19/29/49 + autre), `ILicenseService` validation RSA-SHA256 locale hors ligne, activation/désactivation, clé privée jamais embarquée |
| 18 | Updater | NOT_STARTED | — | |
| 19 | Installateur / Portable | NOT_STARTED | — | |
| 20 | Élévation de privilèges | DONE | 8 ✅ | `TraceZero.Elevated.exe` (manifeste requireAdministrator, surface minimale, single-shot). IPC par fichiers request/response JSON, commande **structurée** uniquement, revalidation via `ElevatedSafePathValidator` (liste d'autorisation dédiée), journal `%ProgramData%\TraceZero\logs`. Débloque le nettoyage de `C:\Windows\Temp` (Paramètres → Nettoyage avancé). App jamais admin par défaut |
| 21 | Localisation | NOT_STARTED | — | fr/en/de/es |
| 22 | Accessibilité | NOT_STARTED | — | |
| 23 | Performance | NOT_STARTED | — | |
| 24 | Tests de sécurité | IN_PROGRESS | — | `TraceZero.SafetyTests` amorcé en Phase 0 |
| 25 | Golden dataset | NOT_STARTED | — | |
| 26 | Tests VM | NOT_STARTED | — | |
| 27 | Qualité release | NOT_STARTED | — | |

## Journal

- **Phase 0 — DONE** — Bootstrap : git initialisé ; solution `TraceZero.slnx` (format .slnx .NET 10)
  avec 9 projets `src`, 7 projets `tests`, 2 outils ; `.editorconfig`, `Directory.Build.props`
  (nullable, implicit usings, `WarningsAsErrors=nullable`), `.gitignore`. Maquettes copiées dans
  `docs/design/`. Packages : CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting/DI/Logging.
  - **Safety-first** : `ISafePathValidator` + `WindowsKnownFolders` + 29 `SafetyTests` (refus prouvé
    de C:\, profil utilisateur, dossiers système/personnels, wildcard, traversal, UNC, drive root,
    hors racine autorisée, jonctions/reparse points).
  - **Shell WPF** : Generic Host + DI, barre latérale (11 pages §4), navigation MVVM
    (CommunityToolkit), thèmes clair/sombre à chaud, localisation de vue par type de VM.
  - **DoD vérifiée** : `dotnet build -c Debug` OK, `-c Release` OK (0 warning), `dotnet test` 29/29,
    application lance sans crash (smoke test 6 s). Aucune valeur de scan simulée (état « — »).

- **Phases 2 & 3 — DONE** — Cœur fonctionnel réel :
  - **Moteur de scan** (`ScanEngine`) : `Parallel.ForEachAsync` borné (cœurs/2), `IProgress<ScanProgress>`,
    annulation, erreurs de fournisseur isolées (`ProviderError`), agrégat par risque, tailles réelles.
  - **Énumération sûre** (`SafeFileEnumerator`) : ne suit jamais jonction/lien (reparse), ignore
    l'inaccessible.
  - **Moteur de nettoyage** (`CleaningEngine`) : `BuildPlan` → `CleaningPlan`, chaque fichier revalidé
    par `ISafePathValidator` avant suppression ; fichiers verrouillés signalés, jamais forcés ;
    octets libérés réels ; Corbeille via API Shell (`SHEmptyRecycleBin`).
  - **Règles Windows** (`WindowsCleaningRules`) : TEMP session, CrashDumps, WER, INetCache, miniatures ;
    Corbeille en REVIEW non cochée par défaut.
  - **UI câblée** : page **Nettoyage** réelle (scan → liste avec cases/puces de risque/tailles →
    prévisualisation sélection par risque → nettoyage → résultat) ; bouton **Analyser mon PC** du
    Dashboard ouvre Nettoyage et lance un vrai scan.
  - **Tests** : 14 tests moteur (âge min, skip jonction, taille réelle, isolation d'erreur, annulation,
    suppression + refus hors-racine + refus dossier protégé + fichier verrouillé + Corbeille) ;
    43 tests au total, Release 0 warning.

- **Phase 4 — Navigateurs** — Détection Chrome/Edge/Brave/Vivaldi/Chromium/Firefox (profils, état
  d'exécution) ; nettoyage des **caches SAFE** via le pipeline (multi-dossiers `SweepRoots`) ;
  page Navigateurs ; connexions préservées par construction. 5 tests.
- **Phase 5 — Privacy Inspector** — Nouvelle primitive de **nettoyage registre allowlistée**
  (`IRegistryTraceCleaner`, HKCU uniquement, refus par défaut) + `FileActionKind.ClearRegistryKey`.
  Catalogue de traces expliquées (`WindowsPrivacyCatalog`) + `WindowsPrivacyInspector`. Page
  « Confidentialité » : chaque trace expliquée (ce que c'est / pourquoi / nombre), sélection et
  nettoyage réel. 2 tests registre (clear allowlisté + refus hors-liste). **51 tests au total.**

- **Phase 6 — Cleaning Plan + Exclusions + Historique** :
  - **Historique SQLite** (`TraceZero.Persistence`, `SqliteCleanupHistoryStore`) : chaque nettoyage
    (Nettoyage + Confidentialité) enregistre un résumé (date, version, source, octets libérés, nb,
    échecs, durée) — **jamais de chemin personnel** (§39). Page Historique réelle (total libéré, nb,
    dernier, journal, effacer) rafraîchie à l'activation.
  - **Exclusions** (`JsonExclusionStore`) : par dossier ou catégorie, persistées en JSON, appliquées
    au scan (éléments exclus retirés + décompte affiché). Gérées dans **Paramètres** (ajout via
    sélecteur de dossier, retrait) + bascule de thème.
  - 3 tests d'intégration (persistance/agrégation/effacement historique). **54 tests au total.**

- **Phase 10 — Disk Space Manager** :
  - **Lecteurs** (`TraceZero.Storage`, `DriveQueryService` via `DriveInfo`) : capacité/utilisé/libre
    + barre d'occupation par lecteur fixe.
  - **Gros fichiers** (`LargeFileScanner`, Engine, réutilise l'énumérateur sûr) : seuil configurable
    (100 Mo → 2 Go), scan du profil utilisateur, progression fluide, tri par taille, cap 500.
  - **Actions** : « Ouvrir » (sélection dans l'Explorateur) ; « Envoyer à la Corbeille »
    (`RecycleFileService`, **réversible**, confirmation, jamais auto-sélectionné).
  - 2 tests (seuil + non-suivi des jonctions). **56 tests au total.**

- **Phase 11 — Duplicate Finder** (`DuplicateFinder`, Engine) : pipeline 3 passes
  (taille → hash partiel 4 Ko → SHA-256 complet) ; jamais de doublon conclu sur nom/date/taille.
  Page Doublons : groupes triés par espace récupérable, stratégie « garder le plus récent »,
  garde-fou **keep-one** (jamais supprimer tout un groupe), suppression **réversible** (Corbeille)
  avec confirmation. 3 tests (doublons réels, même-taille-contenu-différent non groupés,
  octets récupérables). **59 tests au total.**

- **Phase 12 — Applications & Démarrage** :
  - **Apps installées** (`InstalledAppService`) : lues depuis les clés Uninstall (HKLM/WOW64/HKCU),
    filtrées (composants système/mises à jour exclus) ; recherche, ouvrir l'emplacement,
    **désinstaller via le mécanisme de l'éditeur** (jamais de suppression manuelle).
  - **Démarrage** (`StartupService`) : entrées Run HKCU **activables/désactivables de façon
    réversible** (désactiver = déplacer vers une sauvegarde, jamais supprimer) ; Run HKLM et dossier
    de démarrage affichés en lecture seule (élévation requise, différée). 2 tests (toggle+backup
    réversible, refus des entrées machine). **61 tests au total.**

- **Phase 17 — Supporter / PWYW** : page « Soutenir », montants 10/19/29/49 € + autre, avantages
  listés. `ILicenseService` (`LicenseService`, Persistence) : jetons de soutien **signés RSA-SHA256**,
  vérifiés **localement et hors ligne** avec une clé publique embarquée ; la clé privée n'est jamais
  livrée (gitignore). Activation/désactivation persistée. 4 tests (jeton valide, forgé, altéré,
  persistant).
- **Phase 15 — Automatisation** : profils **Sûr** / **Confidentialité** (jamais de REVIEW),
  déclencheurs hebdo/mensuel/à l'ouverture de session via **Planificateur de tâches** (`schtasks`,
  sans service permanent). Mode **headless `--autoclean safe|privacy`** (`AutoCleanRunner`) exécuté
  par la tâche planifiée, respectant les exclusions et journalisant l'historique. 1 test
  (sélection par profil). **71 tests au total.**

### État — TOUS les onglets sont fonctionnels
Accueil · Nettoyage · Confidentialité · Navigateurs · Espace disque · Doublons · Applications ·
Automatisation · Historique · Paramètres · Soutenir. Plus aucune page placeholder.

- **Phase 20 — Élévation de privilèges** (§30) : helper séparé `TraceZero.Elevated.exe`
  (`net10.0-windows`, manifeste `requireAdministrator`, surface minimale, s'arrête après action).
  L'application principale ne demande jamais l'élévation. IPC contrôlé : le client
  (`ElevatedOperationClient`, Windows) lance le helper via le verbe `runas` (invite UAC), transmet une
  **commande structurée** (`ElevatedRequest`, ensemble fermé d'opérations) par fichier JSON et lit la
  réponse (`ElevatedResult`) ; un refus UAC est signalé sans planter. Le helper **ne fait jamais
  confiance au client** : il revalide chaque chemin via `ElevatedSafePathValidator` (nouvelle autorité
  « refus par défaut » n'autorisant QUE les descendants stricts d'une racine élevée dédiée), résout
  lui-même la liste d'autorisation (`%SystemRoot%\Temp`), refuse protocole/opération inconnus, journalise
  sous `%ProgramData%\TraceZero\logs`. Débloque le nettoyage de `C:\Windows\Temp` (déféré Phase 3),
  exposé dans **Paramètres → Nettoyage avancé**. Cœur `ElevatedTempCleaner` testable (jamais de suivi de
  jonction, âge minimum, fichier verrouillé jamais forcé, racine préservée). Tests : validateur élevé
  (autorise un descendant strict, refuse la racine elle-même / chemins Windows arbitraires / traversal /
  wildcard / UNC / racine de volume / liste vide), nettoyeur, exécuteur (refus protocole/opération
  inconnus + résolution de la liste dédiée). **93 tests au total.**

### Prochaine étape
Backend/qualité : Phase 7 (backup/restauration), Phase 13 (Software Updater), Phase 18 (Updater
signé), Phase 19 (installateur/portable/MSIX), Phase 21 (localisation).
