# KNOWN_LIMITATIONS

Ne jamais masquer une limite. Toute fonctionnalité non encore réelle, ou impossible à réaliser de
façon fiable, est listée ici avec son état honnête.

> Convention d'état : `DETECTED_ONLY` (visible mais non nettoyable de façon fiable),
> `STUB_TEMP` (échafaudage temporaire à supprimer avant `DONE` de la phase),
> `PLANNED` (prévu, non commencé), `WONTDO` (écarté avec justification).

## Phase 0 — Bootstrap

- **UI non connectée au moteur** — `PLANNED`. Le shell WPF et les pages sont des placeholders
  clairement identifiés « module en cours de construction ». Aucune valeur affichée n'est présentée
  comme une donnée réelle. À retirer avant release (§11).
- **Pages secondaires** — `PLANNED`. Seule la coquille de navigation existe ; le contenu réel arrive
  aux phases correspondantes.

## Phase 3 — Nettoyage Windows

- **`C:\Windows\Temp`** — ✅ **Résolu en Phase 20**. Le nettoyage standard (`ISafePathValidator`) refuse
  toujours tout chemin sous le répertoire Windows ; le nettoyage de `C:\Windows\Temp` passe désormais
  par le helper élevé `TraceZero.Elevated.exe`, qui applique sa **propre** liste d'autorisation dédiée
  (`ElevatedSafePathValidator`, n'autorisant QUE les descendants stricts de `%SystemRoot%\Temp`) et
  revalide chaque fichier. Exposé dans **Paramètres → Nettoyage avancé**. L'app principale reste non
  élevée. Le nettoyage standard continue de ne cibler que le profil utilisateur (`AppData\Local`).
- **Autres caches sous `C:\Windows`** (hors `Temp`) — `PLANNED`. Non couverts : chaque emplacement
  supplémentaire devra être ajouté explicitement à la liste d'autorisation dédiée du helper, avec sa
  propre justification (jamais de nettoyage générique sous `C:\Windows`).
- **`windows.user-temp`** applique un âge minimum de 1 h pour éviter de supprimer des fichiers
  temporaires en cours d'utilisation ; les fichiers verrouillés sont de toute façon ignorés, jamais
  forcés.

## Phase 4 — Navigateurs

- **Nettoyage limité aux caches SAFE** — `PLANNED`. En Phase 4, seuls les dossiers de cache
  (régénérables) sont nettoyés ; les connexions/cookies/mots de passe/favoris ne sont jamais touchés
  (protection par construction). Le nettoyage PRIVACY/REVIEW (historique, cookies choisis, sessions)
  nécessite de fermer le navigateur et des transactions sur des bases SQLite : prévu ultérieurement
  avec sauvegarde/restauration (Phase 7) et gestionnaire de cookies (§14).
- **Opera** — `PLANNED`. Disposition cache/profil scindée entre `Local` et `Roaming` (cache sous
  `%LOCALAPPDATA%\Opera Software\Opera Stable\Cache`) : non incluse pour éviter des chemins erronés.
  Chrome, Edge, Brave, Vivaldi, Chromium et Firefox sont couverts.
- **Navigateur en cours d'exécution** — les caches d'un navigateur ouvert ne sont pas cochés par
  défaut, et ses fichiers verrouillés sont ignorés (jamais forcés).

## Phase 7 — Protection / Backup / Restore

- **Portée de la sauvegarde** — `PLANNED` (extension). La Phase 7 sauvegarde/restaure les **traces
  registre HKCU** avant un nettoyage réversible. Les traces **fichier** de la Confidentialité ne sont pas
  copiées dans le coffre : leur réversibilité repose sur la Corbeille lorsqu'elle est utilisée. Le
  gestionnaire de cookies / DB navigateur (transaction + backup temporaire, §14) reste `PLANNED`
  (Phase 4 PRIVACY) et réutilisera cette infrastructure.
- **Point de restauration Windows** — `PLANNED`. La création d'un point de restauration système
  (`SystemRestore.CreateRestorePoint`, réservée aux modules Expert, §17) nécessite l'élévation et n'est
  pas encore implémentée ; elle passera par une opération dédiée du helper `TraceZero.Elevated.exe`.
- **Nettoyage automatique headless** — `PLANNED`. Le mode `--autoclean` (§15) opère via les moteurs de
  scan/nettoyage et ne crée pas encore de points de restauration ; à câbler sur `IProtectionVault` pour
  les profils touchant des traces registre.

## Phase 21 — Localisation — ✅ DONE

- **Aucun texte UI codé en dur** (§31) : toute l'interface est traduite en **fr/en/de/es** avec bascule
  live (dictionnaires de chaînes swappés à chaud + culture du thread, mécanisme type thème). Couvre : les
  16 pages, les **descriptions de règles** (mécanisme clé+repli sur `ScanItem`/`FileSweepRule`, résolu par
  l'UI — l'Engine reste portable), le **catalogue Confidentialité** (8 traces), les navigateurs, **tous
  les libellés calculés des lignes**, et **tous les messages dynamiques des ViewModels** (status, toasts,
  confirmations modales, titres de dialogues) ainsi que les libellés de démarrage.
- **Non traduits (par conception)** : les **endonymes** du sélecteur de langue (Français/English/Deutsch/
  Español) et les **clés de catégorie stockées en base** (`Source` de l'historique = « Nettoyage »/
  « Confidentialité »/« Automatisation »), qui sont des identifiants stables **mappés vers une chaîne
  localisée à l'affichage** (jamais montrés bruts).
- **Restant hors périmètre §31** : audit humain des traductions DE/ES par un locuteur natif avant release
  (les traductions actuelles sont fonctionnelles et cohérentes).

## Phase 18 — Updater

- **Cœur** — ✅ livré : `UpdateChecker` valide un manifeste **signé RSA-SHA256** et décide s'il faut
  mettre à jour ; un manifeste dont la signature échoue n'est jamais accepté (§28).
- **Reste `PLANNED`** (dépend d'assets externes) : téléchargement **HTTPS** du binaire, vérification
  **SHA-256** du fichier téléchargé, **Authenticode** + vérification de l'**éditeur attendu**,
  exécution/rollback documenté, **endpoint de publication** réel et **certificat de signature de code**.
  Aucune de ces étapes ne s'exécute sans un manifeste validé.
- **Microsoft Store** — `WONTDO` pour cette build : la version Store ne doit pas contourner le mécanisme
  de mise à jour du Store (l'updater maison est désactivé dans ce packaging).

## Phase 8 — Analyse NTFS avancée

- **Lecture des artefacts NTFS bruts** (contenu du Journal USN, entrées MFT résiduelles, `$LogFile`) —
  `DETECTED_ONLY` par conception (§18). Ces lectures nécessitent un handle de volume privilégié et leur
  suppression sélective fiable n'existe pas sans risque de corruption : TraceZero les **explique** et les
  marque « Détectée »/« Gérée par Windows », **sans jamais** les modifier, les simuler comme nettoyées,
  ni supprimer le Journal USN.
- **Espace libre récupérable** — seul artefact réellement **atténuable en sécurité** : géré via
  l'effacement d'espace libre (Phase 9), vers lequel la page renvoie. La taille affichée est réelle
  (`DriveInfo`, volumes NTFS fixes).

## Phase 22 — Accessibilité

- **Livré** : focus clavier visible (templates boutons/nav), `AutomationProperties.Name` sur les
  contrôles sans libellé, modale au clavier (Entrée/Échap), statut jamais transmis par la seule couleur.
- **Déplacement automatique du focus vers la modale** — `PLANNED`. À l'ouverture d'une confirmation, le
  focus n'est pas encore déplacé programmatiquement dans la boîte (les lecteurs d'écran doivent y naviguer).
  Entrée/Échap fonctionnent déjà.
- **DPI per-monitor v2 & mise à l'échelle du texte** — `PLANNED`. L'app suit la mise à l'échelle système
  (WPF) ; le manifeste per-monitor v2 et la prise en compte du paramètre « taille du texte » de Windows
  ne sont pas encore configurés.
- **Audit lecteur d'écran complet (Narrator/NVDA)** — `PLANNED` : passe manuelle à mener avant release.

## Phase 9 — Effacement sécurisé

- **SSD / NVMe** — l'écrasement fichier par fichier et l'effacement d'espace libre **ne sont jamais
  présentés comme garantis** : wear leveling et TRIM peuvent laisser des copies ailleurs. L'UI affiche un
  avertissement honnête selon le média détecté (WMI) ; si le type est indéterminé, l'avertissement couvre
  les deux cas.
- **Détection du média** — best-effort (lettre → `MSFT_Partition.DiskNumber` → `MSFT_PhysicalDisk`).
  En cas d'échec/ambiguïté → `Unknown` + avertissement prudent, jamais un type deviné.
- **Effacement d'espace libre sur le lecteur système** — utilise le dossier temporaire (même volume) ;
  sur d'autres lecteurs, crée `\<lecteur>\TraceZeroWipe`. Si l'écriture est impossible (droits) →
  échec honnête, aucune donnée touchée.
- **Effacement bas niveau (ATA Secure Erase / NVMe Format)** — `PLANNED`/`WONTDO` à ce stade : non
  exposé ; l'écrasement logique ci-dessus est la seule méthode, avec ses limites annoncées.

## Phase 14 — Driver Health / Updater

- **Étape A (inventaire)** — ✅ livrée en lecture seule (WMI). Périphérique, version, fournisseur, date,
  signature, problèmes Gestionnaire de périphériques.
- **Étape B (installation de pilotes)** — `WONTDO` (par conception, §24). TraceZero n'installe ni ne
  télécharge aucun pilote : trop haut risque. La mise à jour est **déléguée à Windows Update** (pilotes
  signés, compatibles, réversibles) — jamais de base tierce, de mirror non officiel ni de matching fuzzy.
  Si un jour une méthode officielle et fiable existe, elle exigera point de restauration + backup +
  vérification de signature/compatibilité + confirmation explicite + rollback documenté.

## Phase 28 — Moniteur système honnête

- **Santé disque de base** — ✅ livrée via WMI (`MSFT_PhysicalDisk`) : état Healthy/Warning/Unhealthy,
  type HDD/SSD, taille, sans admin.
- **Attributs SMART détaillés (SATA/ATA)** — `DETECTED_ONLY`/`PLANNED`. Heures sous tension, secteurs
  réalloués, température : nécessitent `MSStorageDriver_ATAPISmartData` (décodage vendeur, souvent
  élévation) — non implémentés ; passeront par une opération read-only du helper élevé.
- **Santé NVMe** — `PLANNED`. Température / usure via `IOCTL_STORAGE_QUERY_PROPERTY` (health log) non
  couverte.
- **Impact au démarrage** — la lecture du journal `Diagnostics-Performance/Operational` peut exiger des
  droits administrateur : dans ce cas le rapport est marqué **indisponible** (message honnête) plutôt que
  faussé. Le classement « High/Medium/Low » du Gestionnaire des tâches n'est pas exposé par une API ;
  on affiche la **pénalité en millisecondes réellement mesurée** par Windows.
- **Désactivation depuis la page** — `PLANNED`. La désactivation d'un programme au démarrage se fait
  dans « Applications » (toggle réversible) ; la corrélation automatique impact↔entrée n'est pas encore
  câblée pour éviter tout faux appariement.

## Limitations techniques anticipées (à réévaluer aux phases concernées)

- **Traces NTFS / MFT / USN (Phase 8)** — certaines traces seront `DETECTED_ONLY` si aucune API
  Windows supportée ne permet une suppression fiable sans corrompre le système de fichiers.
- **Secure erase SSD/NVMe (Phase 9)** — le multi-pass ne sera jamais présenté comme garanti
  (wear leveling / TRIM). Avertissement honnête obligatoire.
- **Driver Updater (Phase 14)** — si aucune méthode d'installation suffisamment sûre n'est
  disponible, on livre `Driver Health` complet et on redirige vers Windows Update plutôt que de
  simuler une parité avec CCleaner.
- **Fichiers verrouillés** — pas de suppression forcée brutale ; options ignorer/réessayer/fermer
  l'app, planification au redémarrage seulement si méthode Windows documentée.
