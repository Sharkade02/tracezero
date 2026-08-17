# PHASE_STATUS

Source de vérité de l'avancement. Statuts : `NOT_STARTED`, `IN_PROGRESS`, `BLOCKED`, `DONE`.

> Règle : une phase n'est `DONE` que si sa Definition of Done est vérifiée, les tests passent,
> et aucun `TODO` / `NotImplementedException` / mock ne subsiste dans le code de production concerné.

| Phase | Nom | Status | Tests | Notes |
|------:|-----|--------|-------|-------|
| 0 | Bootstrap du projet | DONE | 29 ✅ | Solution 18 projets, DI host + shell WPF (nav + thèmes) + safety layer. Build Debug/Release OK, app lance sans crash |
| 1 | UI Shell + Design System | DONE | — | Design system complet : Card/boutons/NavButton/RiskChip/Progress/empty+result states + **skeleton loader**, **toast** (superposition, auto-dismiss, couleur par nature) et **modale de confirmation** (thématisée, action destructive en rouge jamais présélectionnée). Composants **réellement câblés** : désinstallation Apps, vider coffre/historique → modale ; restauration/actions → toast ; chargement Apps → skeleton |
| 2 | Scan Engine | DONE | 14 ✅ | Moteur async parallèle borné, progress, cancellation, erreurs isolées, tailles réelles |
| 3 | Nettoyage Windows standard | DONE | ✅ | Règles user-scoped réelles (TEMP, CrashDumps, WER, INetCache, thumbnails) + Corbeille. Windows\Temp différé (voir KNOWN_LIMITATIONS) |
| 4 | Navigateurs | IN_PROGRESS | 5 ✅ | Détection Chrome/Edge/Brave/Vivaldi/Chromium/Firefox + profils + état exécution ; **caches SAFE** nettoyés (connexions préservées). History/cookies/sessions + Opera différés |
| 5 | Privacy Inspector Windows | DONE | 2 ✅ | Page « Ce que Windows sait encore » : RecentDocs, RunMRU, TypedPaths, WordWheelQuery, UserAssist, ComDlg32 MRU + Documents récents/Jump Lists. Chaque trace expliquée, nettoyage registre allowlisté + fichiers validés |
| 6 | Cleaning Plan + Safety layer | DONE | 3 ✅ | `CleaningPlan`/preview + **exclusions** (dossier/catégorie, JSON, appliquées au scan) + **journal d'historique** SQLite |
| 7 | Protection / Backup / Restore | DONE | 8 ✅ | Classification Reversible/PartiallyReversible/Irreversible ; **sauvegarde des traces registre HKCU avant nettoyage réversible** (`IRegistryBackupService`, capture/restore récursif natif, tous types de valeurs) ; **coffre de restauration** SQLite (`IProtectionVault`) ; page **Restauration** (« Protection du nettoyage » + « Restaurer les éléments disponibles »). Jamais de promesse de restaurer un fichier effacé de façon sécurisée |
| 8 | Analyse NTFS avancée | DONE | 2 ✅ | Page **Analyse NTFS (Mode Expert)**, lecture seule : catalogue expliqué des artefacts (Journal USN, MFT, `$LogFile`, résidus de noms, contenu récupérable de l'espace libre avec taille réelle). Statut **honnête** — « Détectée » / « Gérée par Windows » (jamais « Nettoyable », jamais simulé) ; seul l'espace libre est **atténuable** → renvoi vers l'effacement sécurisé (Phase 9). Jamais d'écriture MFT/USN ni de structure NTFS brute (§18) |
| 9 | Effacement sécurisé | DONE | 10 ✅ | Page **Effacement sécurisé** : effacement fichier (écrasement 1 passe / renforcé 3 passes puis suppression, garde-fou dédié refusant système/racine/dossier/jonction) + **effacement espace libre** (remplissage temporaire annulable, ne touche aucun fichier existant, estimation/progression). Détection média HDD/SSD (WMI) → **avertissement honnête SSD/NVMe** (wear leveling/TRIM, jamais garanti). Irréversible jamais présenté autrement |
| 10 | Disk Space Manager | DONE | 2 ✅ | Vue lecteurs (capacité/utilisé/libre + barre) ; recherche de gros fichiers (seuil 100 Mo–2 Go) ; ouvrir dans l'Explorateur ; envoi à la Corbeille (réversible), jamais auto-sélectionné |
| 11 | Duplicate Finder | DONE | 3 ✅ | Pipeline taille → hash partiel → SHA-256 complet ; groupes, « garder le plus récent », garde-fou keep-one, envoi Corbeille réversible + validation |
| 12 | Applications & Démarrage | DONE | 4 ✅ | Apps installées (registre Uninstall, recherche, ouvrir, désinstaller via éditeur) ; démarrage (Run HKCU activable/désactivable réversible + backup, HKLM/dossier en lecture seule) |
| 13 | Software Updater | NOT_STARTED | — | |
| 14 | Driver Health / Updater | DONE | 7 ✅ | **Étape A (Driver Health)** : inventaire pilotes lecture seule via WMI (`Win32_PnPSignedDriver` : périphérique, version, fournisseur, date, signature) + problèmes Gestionnaire de périphériques (`Win32_PnPEntity.ConfigManagerErrorCode`). Recherche + skeleton. **Étape B (updater)** : redirigée vers **Windows Update** (§24), jamais de base tierce ni d'installation par TraceZero |
| 15 | Automatisation | DONE | 1 ✅ | Profils Sûr/Confidentialité, déclencheurs hebdo/mensuel/ouverture session via Planificateur (schtasks), mode headless `--autoclean` ; jamais de REVIEW en auto |
| 16 | Historique & Statistiques | DONE | ✅ | Page Historique réelle : total libéré, nb nettoyages, dernier, journal. Local, sans chemins personnels |
| 17 | Supporter / PWYW | DONE | 4 ✅ | Page Soutenir (PWYW 10/19/29/49 + autre), `ILicenseService` validation RSA-SHA256 locale hors ligne, activation/désactivation, clé privée jamais embarquée |
| 18 | Updater | NOT_STARTED | — | |
| 19 | Installateur / Portable | NOT_STARTED | — | |
| 20 | Élévation de privilèges | DONE | 8 ✅ | `TraceZero.Elevated.exe` (manifeste requireAdministrator, surface minimale, single-shot). IPC par fichiers request/response JSON, commande **structurée** uniquement, revalidation via `ElevatedSafePathValidator` (liste d'autorisation dédiée), journal `%ProgramData%\TraceZero\logs`. Débloque le nettoyage de `C:\Windows\Temp` (Paramètres → Nettoyage avancé). App jamais admin par défaut |
| 21 | Localisation | NOT_STARTED | — | fr/en/de/es |
| 22 | Accessibilité | DONE | — | **Focus clavier visible** (bordure) ajouté aux templates boutons/nav qui le masquaient ; `AutomationProperties.Name` sur les contrôles sans libellé (recherches, sélecteur de lecteur, barre de progression, fermeture toast) ; modale : Entrée=confirmer / Échap=annuler (`IsDefault`/`IsCancel`) ; aucun statut par la couleur seule (toasts glyphe+texte, états disque/pilote libellés). Résiduels (gestion du focus lecteur d'écran sur modale, DPI per-monitor, mise à l'échelle du texte) dans `KNOWN_LIMITATIONS.md` |
| 23 | Performance | NOT_STARTED | — | |
| 24 | Tests de sécurité | IN_PROGRESS | — | `TraceZero.SafetyTests` amorcé en Phase 0 |
| 25 | Golden dataset | NOT_STARTED | — | |
| 26 | Tests VM | NOT_STARTED | — | |
| 27 | Qualité release | DONE | — | Script **`build/scripts/release.ps1`** (validé de bout en bout) : restore, **build -c Release (échec si le moindre avertissement)**, **test -c Release**, publish App+Elevated (win-x64), empreintes **SHA-256** (`artifacts/SHA256SUMS.txt`). Portes externes (signature Authenticode, scan AV, smoke install/uninstall, test updater, nettoyage réel en VM) **listées honnêtement comme manuelles, jamais simulées** (§37). Gate Release actuel : 0 warning, 124 tests ✅ |
| 28 | Moniteur système honnête | DONE | 4 ✅ | Page **Santé système** : santé disque via WMI (`MSFT_PhysicalDisk` : état Healthy/Warning/Unhealthy + type HDD/SSD + taille, sans admin) + **impact au démarrage mesuré** (journal `Diagnostics-Performance` évt 101, agrégé par programme). Read-only, mesuré, expliqué ; aucun score inventé ni « booster » (§42). SMART détaillé/NVMe = `DETECTED_ONLY`. Détail : `docs/phase-28-system-monitor.md` |

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

- **Phase 1 — UI Shell + Design System** (§11) — finalisée : les trois derniers composants du design
  system sont livrés et **câblés à de vraies actions** (pas décoratifs, §0).
  - **Skeleton loader** : style `Skeleton` réutilisable (fond neutre thématisé, pulsation douce) ; utilisé
    pendant le chargement de la page Applications (lecture du registre = vraie latence).
  - **Toast** : `IToastService`/`ToastService` (liste observable, disparition auto ~4,5 s + fermeture
    manuelle), superposition coin bas-droit, couleur d'accent par nature (Info/Succès/Avertissement/Erreur).
    Utilisé sur restauration, désinstallation, effacements.
  - **Modale de confirmation** : `IDialogService`/`DialogService` (`ModalViewModel` avec
    `TaskCompletionSource`, `ConfirmAsync` awaitable), superposition centrée sur voile ; action destructive
    en rouge, **jamais présélectionnée**. Remplace les `MessageBox` système (désinstallation) et protège
    les actions destructives (vider le coffre de restauration, effacer l'historique).
  - Convertisseur réutilisable `InverseBooleanToVisibilityConverter` ; couleurs `Brush.Overlay.Scrim` /
    `Brush.Skeleton` ajoutées aux deux thèmes. Shell (`ShellViewModel`) expose `Toasts` et `Dialog`, hôtés
    dans `MainWindow`.

- **Phase 7 — Protection, Backup et Restauration** (§17) :
  - **Réversibilité honnête** : l'enum `Reversibility` (Reversible / PartiallyReversible / Irreversible)
    est portée par chaque `RestoreRecord` ; la page n'offre « Restaurer » que pour les éléments réellement
    réversibles. Jamais de promesse de restaurer un fichier effacé de façon sécurisée.
  - **Sauvegarde registre** (`TraceZero.Windows`, `RegistryBackupService`) : capture/restauration
    **récursive native** d'une sous-clé HKCU (String, ExpandString, DWord, QWord, Binary, MultiString +
    sous-clés), sans élévation. La sous-clé est fournie par l'appelant (issue du catalogue de traces
    autorisées) — le service ne décide jamais seul quoi sauvegarder. Sérialisation portable
    (`RegistrySnapshotCodec`, JSON) testable sans Windows.
  - **Coffre de restauration** (`TraceZero.Persistence`, `SqliteProtectionVault`, `IProtectionVault`) :
    nouvelle table `restore_points` (même base SQLite locale) — horodatage, description, réversibilité,
    cible, charge utile. Local uniquement, jamais transmis (§39).
  - **Câblage** : le nettoyage Confidentialité **sauvegarde chaque trace registre sélectionnée AVANT de
    l'effacer**, puis persiste un point de restauration ; ni la sauvegarde ni l'historique ne peuvent
    faire échouer un nettoyage réussi.
  - **UI** : page **Restauration** (bannière « Protection du nettoyage » + liste « Restaurer les éléments
    disponibles » avec puce de réversibilité et bouton Restaurer réversible ; vider le coffre).
  - **Tests** : 4 round-trips sauvegarde/restauration registre (tous types de valeurs + sous-clé, clé
    absente, sérialisation, garde-fou allow-list) + 4 coffre SQLite (ajout/liste plus récent d'abord,
    marquage restauré, effacement, persistance/réversibilité). **101 tests au total.**

- **Phase 27 — Qualité release** (§37) — DONE (portes automatisables).
  - **`build/scripts/release.ps1`** (compatible PS 5.1 et 7, validé) : `restore` → `build -c Release`
    (échec si le moindre avertissement — objectif « Release 0 warning ») → `test -c Release` (unitaires +
    `SafetyTests` + `IntegrationTests`) → `publish` de `TraceZero.App` et `TraceZero.Elevated` (win-x64,
    framework-dependent) → **empreintes SHA-256** (`artifacts/SHA256SUMS.txt`).
  - **Honnêteté (§0, §37)** : signature Authenticode, scan antivirus, smoke test install/désinstall, test
    updater et **nettoyage réel en VM** ne sont **jamais simulés** — le script les affiche comme portes
    manuelles à cocher (elles dépendent d'un certificat/AV/VM externes).
  - **État vérifié** : build Release **0 avertissement**, **124 tests** verts en Release.

- **Phase 8 — Analyse NTFS avancée** (§18) — DONE (Mode Expert, lecture seule).
  - `NtfsAnalyzer` (`TraceZero.Engine`, `INtfsAnalyzer`) : catalogue expliqué des artefacts NTFS de
    confidentialité — Journal USN, MFT, `$LogFile`, résidus de noms, contenu récupérable de l'espace libre
    (avec taille réelle par volume NTFS via `DriveInfo`).
  - **Honnêteté (§18)** : chaque artefact porte un statut — `DetectedOnly` / `ManagedByWindows`
    (« Détectée », jamais « Nettoyable », jamais de suppression simulée) ou `MitigableByFreeSpaceWipe`
    (seul l'espace libre est atténuable **en sécurité**). Aucune écriture MFT/USN, aucune structure NTFS
    brute modifiée, aucun contournement du FS.
  - **UI** : page « Analyse NTFS » (badge Mode Expert). L'artefact espace libre propose « Effacer l'espace
    libre… » qui **navigue** vers le module d'effacement sécurisé (Phase 9) — action réelle et sûre.
  - **Tests** : 2 (artefacts expliqués ; MFT/USN en détecté-seul, seul l'espace libre atténuable).
    **124 tests au total.**

- **Phase 22 — Accessibilité** (§32) — DONE (passe transverse).
  - **Focus clavier visible** : les templates `PrimaryButton`/`SecondaryButton`/`NavButton` masquaient
    l'anneau de focus (ControlTemplate custom) → ajout d'une bordure visible sur `IsKeyboardFocused`.
  - **Libellés accessibles** : `AutomationProperties.Name` sur les contrôles sans texte propre (champs de
    recherche Applications/Pilotes, sélecteur de lecteur, barre de progression, bouton de fermeture des
    toasts).
  - **Clavier** : navigation par Tab déjà en place ; modale de confirmation pilotable au clavier
    (Entrée = confirmer via `IsDefault`, Échap = annuler via `IsCancel`).
  - **Statut jamais par la couleur seule** : toasts (glyphe + message), états disque/pilote (texte +
    couleur), réversibilité (texte). Vérifié transversalement.
  - Résiduels honnêtes (déplacement du focus vers la modale pour lecteurs d'écran, DPI per-monitor v2,
    mise à l'échelle du texte système) : consignés dans `KNOWN_LIMITATIONS.md`.

- **Phase 9 — Effacement sécurisé** (§19) — DONE. Module distinct, page **Effacement sécurisé**.
  - **Effacement de fichier** (`TraceZero.Engine`, `SecureEraser`) : garde-fou **dédié** (la cible est
    choisie par l'utilisateur, mais on refuse dossiers système/Program Files, racines de volume,
    répertoires, jonctions/liens, fichiers absents). Écrasement `WriteThrough` — 1 passe (aléatoire) ou
    renforcé 3 passes (aléatoire/0xFF/aléatoire) — puis suppression. Pas de multiplication artificielle
    des passes. Irréversible, jamais présenté comme réversible.
  - **Effacement de l'espace libre** (`FreeSpaceWiper`) : écrit un **unique** fichier de remplissage
    jusqu'à saturation (ou plafond), avec estimation, progression et **annulation**, puis le supprime
    (finally). Ne supprime **jamais** de fichier existant.
  - **Détection média** (`TraceZero.Storage`, `StorageMediaProbe`, WMI `MSFT_Partition`→`MSFT_PhysicalDisk`)
    → l'UI adapte l'**avertissement honnête** : SSD/NVMe = « non garanti » (wear leveling/TRIM),
    HDD = efficace, inconnu = avertissement couvrant les deux cas.
  - **UI** : `SecureEraseViewModel` (+ `IDisposable`) : sélection de fichiers (picker + validation),
    mode renforcé, confirmation **modale destructive**, toasts ; wipe par lecteur avec barre de
    progression + annulation.
  - **Tests** (sur fichiers temporaires uniquement, jamais sur un vrai lecteur — DoD §19) : 7 eraser
    (1/3 passes, refus manquant/dossier/racine/système, autorisation fichier utilisateur) + 3 wiper
    (plafond + fichier témoin intact + fichier de remplissage retiré, annulation, dossier introuvable).
    **122 tests au total.**

- **Phase 14 — Driver Health** — DONE (étape A). Page **Pilotes**, lecture seule, §24-safe.
  - **Inventaire** (`TraceZero.Windows`, `DriverHealthService`, WMI `Win32_PnPSignedDriver` via
    `System.Management`) : périphérique, classe, version, fournisseur/fabricant, date (parsing
    `CIM_DATETIME` → `DateOnly`, testable), signature. Croisé avec `Win32_PnPEntity.ConfigManagerErrorCode`
    pour marquer les périphériques signalés en problème par le Gestionnaire de périphériques.
  - **Étape B (mises à jour)** : **pas d'updater maison**. Bouton « Ouvrir Windows Update »
    (`ms-settings:windowsupdate`) — pilotes signés/compatibles/réversibles gérés par Windows (§24).
    Aucune base de pilotes tierce, aucun matching fuzzy.
  - **UI** : `DriverHealthViewModel` + page (recherche, tri problèmes d'abord, skeleton au chargement,
    puce d'état colorée). Enregistrée après « Santé système ».
  - **Tests** : 6 parsing date CIM (valide + 5 cas invalides) + 1 smoke (jamais d'exception). **112 tests.**

- **Phase 28 — Moniteur système honnête** — DONE. Page **Santé système** livrée, read-only et §42-safe.
  - **Santé disque** (`TraceZero.Storage`, `DiskHealthService`, WMI `MSFT_PhysicalDisk` via
    `System.Management`) : état Healthy/Warning/Unhealthy, type HDD/SSD, taille — tels que rapportés par
    Windows, sans admin ni score inventé. Un `Warning` est présenté factuellement (« Windows signale un
    risque — sauvegardez vos données »).
  - **Impact au démarrage** (`TraceZero.Windows`, `StartupImpactService`, journal
    `Diagnostics-Performance/Operational` évt 101 via `System.Diagnostics.EventLog`) : pénalité moyenne
    par programme sur les derniers démarrages **réellement mesurés** par Windows. Parsing pur
    (`TryParseEventXml`) testable. Si le journal exige des droits élevés, rapport marqué indisponible
    (honnête) plutôt que faussé.
  - **UI** : `SystemHealthViewModel` + page (santé disque en cartes colorées par état, impacts triés) ;
    couleurs par `DataTrigger` (thème à chaud). Enregistrée après « Espace disque ».
  - **Tests** : 3 parsing d'événement (nom+temps, sans temps rejeté, XML invalide) + 1 smoke santé disque
    (jamais d'exception, liste cohérente). **105 tests au total.**
  - **Limites honnêtes** (SMART détaillé SATA/ATA, santé NVMe, lecture du journal sans élévation) : voir
    `KNOWN_LIMITATIONS.md` et `docs/phase-28-system-monitor.md`.

- **Phase 28 (spécification initiale)** : nouvelle phase (le plan initial
  s'arrêtait à 27). Deux capacités **read-only, mesurées et expliquées**, sans jamais tomber dans le
  « PC booster » interdit par §42 :
  - **Santé disque (SMART)** par niveaux d'honnêteté : santé de base (`MSFT_PhysicalDisk.HealthStatus`
    + `Win32_DiskDrive.Status`, fiable et sans admin) → attributs SMART détaillés SATA/ATA
    (`MSStorageDriver_ATAPISmartData`, best-effort, élévation read-only via le helper existant) → santé
    NVMe (`IOCTL_STORAGE_QUERY_PROPERTY`, `DETECTED_ONLY` au départ). Type de média HDD/SSD/NVMe
    réutilisé par la Phase 9.
  - **Impact au démarrage mesuré** : lecture du journal Windows
    `Microsoft-Windows-Diagnostics-Performance/Operational` (Event ID 100-110, pénalité par app en ms),
    corrélée aux entrées de la **Phase 12** ; désactivation **réversible** via `IStartupService.SetEnabled`.
  - Architecture : `Domain/Diagnostics`, `Application/Diagnostics` (`IDiskHealthService`,
    `IStartupImpactService`), `Windows/Diagnostics` (`net10.0-windows`) ; accès SMART admin = nouvelle
    opération read-only dans l'enum fermé de `TraceZero.Elevated.exe` (ADR-0006), app jamais admin.
  - Détail complet : `docs/phase-28-system-monitor.md`. Insertion recommandée après la Phase 7.

### Prochaine étape
Backend/qualité : Phase 28 (moniteur système honnête), Phase 13 (Software Updater), Phase 18 (Updater
signé), Phase 19 (installateur/portable/MSIX), Phase 21 (localisation). La Phase 4 (nettoyage PRIVACY des
navigateurs) peut désormais s'appuyer sur l'infrastructure de sauvegarde/restauration de la Phase 7.
