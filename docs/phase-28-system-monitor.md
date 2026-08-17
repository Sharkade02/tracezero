# Phase 28 — Moniteur système honnête (Santé disque SMART + Impact au démarrage)

> **Statut** : `DONE` (livrée). Santé disque de base (WMI) + impact au démarrage (EventLog). SMART
> détaillé/NVMe restent `DETECTED_ONLY`/`PLANNED` — voir `KNOWN_LIMITATIONS.md`. Voir `PHASE_STATUS.md`.
> **Principe directeur** : tout est **mesuré, read-only et expliqué**. Aucun score inventé, aucun
> « booster », aucune cause de ralentissement affirmée sans preuve (§42 du cahier de mission). Là où
> Windows ne fournit pas de donnée fiable, on affiche honnêtement « Donnée non disponible » plutôt que
> d'inventer (convention `DETECTED_ONLY` de `KNOWN_LIMITATIONS.md`).

## Motivation

Le cahier de mission **interdit** explicitement RAM Booster, registry booster, faux compteur d'erreurs,
score santé inventé et « votre PC est en danger » sans preuve (§42). Cette phase apporte la valeur qu'un
utilisateur attend d'un « moniteur système » **sans** ces dark patterns : uniquement des faits mesurés
par Windows, présentés et expliqués, reliés quand c'est possible à une action réversible.

## Périmètre

- **A. Santé disque (SMART)** — état de santé réel de chaque disque physique, en lecture seule.
- **B. Impact au démarrage** — temps de démarrage réellement mesuré par Windows, par programme.
- **C. (option / stretch)** — panneau live CPU/RAM purement informatif, étiqueté « moniteur », jamais
  « optimisation » ni « libérer la RAM ».

## A — Santé disque SMART (par niveaux d'honnêteté)

| Niveau | Source Windows | Fiabilité | Sans admin |
|---|---|---|---|
| **Santé de base** : `Healthy` / `Warning` / `Unhealthy` + prédiction de panne | `MSFT_PhysicalDisk.HealthStatus` (`root\Microsoft\Windows\Storage`) + `Win32_DiskDrive.Status` (« OK » / « Pred Fail ») | Fiable | ✅ |
| **Attributs SMART détaillés** SATA/ATA : heures sous tension, secteurs réalloués, température, cycles de démarrage/arrêt | WMI `MSStorageDriver_ATAPISmartData` (`root\wmi`) | Best-effort, décodage vendeur | ⚠️ exige souvent l'élévation → opération **read-only** ajoutée à l'enum fermé de `TraceZero.Elevated.exe` |
| **Santé NVMe** : température, pourcentage d'usure, octets écrits (Data Units Written) | `IOCTL_STORAGE_QUERY_PROPERTY` (health log NVMe) | Avancé | ⚠️ → `DETECTED_ONLY` au départ, documenté dans `KNOWN_LIMITATIONS.md` |

- **Type de média** (HDD / SSD / NVMe) via `MSFT_PhysicalDisk.MediaType` — sert aussi à la **Phase 9**
  (effacement sécurisé, qui doit distinguer HDD/SSD).
- **Explication de chaque attribut** (comme le catalogue Privacy de la Phase 5) : ex. « Secteurs
  réalloués = 0 → bon ; > 0 → le disque a déjà remplacé des secteurs défectueux ».
- **Aucun score global chiffré inventé** : on n'expose que les états Windows réels et les valeurs brutes
  mesurées, avec leur seuil de référence quand il existe.

## B — Impact au démarrage (honnête, mesuré)

Le classement « High / Medium / Low » du Gestionnaire des tâches n'est pas exposé proprement à une API
tierce. On utilise donc la **source que Windows lui-même exploite** :

- **Journal `Microsoft-Windows-Diagnostics-Performance/Operational`** (Event ID 100-110) : Windows y
  consigne, à chaque démarrage, le **temps de démarrage réel** et la **pénalité par application**
  (nom + durée en ms) sur les derniers boots.
- Lecture **read-only** via `System.Diagnostics.Eventing.Reader`, puis **corrélation** avec les entrées
  de démarrage déjà gérées en **Phase 12** (`IStartupService`).
- Résultat par entrée : « +820 ms au démarrage, moyenne sur 5 boots », avec un bouton **Désactiver** qui
  réutilise le toggle **réversible** existant (`IStartupService.SetEnabled`, avec sauvegarde). Sans
  donnée récente → « Pas encore mesuré ».

C'est la fonctionnalité **différenciante et prouvable** : relier une mesure réelle à une action
réversible, sans jamais affirmer une cause de lenteur non prouvée.

## Architecture (respecte ADR-0001 / ADR-0006)

- **`TraceZero.Domain/Diagnostics/`** (`net10.0`) : modèles purs `DiskHealth`, `SmartAttribute`,
  `StartupImpact`.
- **`TraceZero.Application/Diagnostics/`** (`net10.0`) : interfaces `IDiskHealthService`,
  `IStartupImpactService` (+ option `ISystemResourceMonitor`).
- **`TraceZero.Windows/Diagnostics/`** (`net10.0-windows`) : implémentations WMI / Event Log / IOCTL.
- **Élévation** : l'accès SMART détaillé nécessitant admin passe par une **nouvelle opération read-only**
  dans l'ensemble fermé d'opérations de `TraceZero.Elevated.exe` (ADR-0006) — jamais de chemin
  arbitraire transmis par l'UI ; l'app principale **reste non-admin**.
- **UI** : nouvelle page **« Santé système »** dans le shell (ou onglet dans **Espace disque**). Même
  patterns que les pages existantes : états vide/résultat, `RiskChip` réutilisable, aucune valeur
  affichée qui ne soit pas réelle.

## Tests (`TraceZero.Windows.Tests`, `TraceZero.Domain.Tests`)

- Parsing d'un échantillon SMART WMI mocké → attributs décodés attendus.
- Corrélation impact ↔ entrée de démarrage (jointure par nom/chemin exécutable).
- Décodage de la pénalité / des durées depuis un enregistrement d'événement échantillon.
- Comportement « pas de donnée » : aucune valeur inventée, message honnête.
- Comportement disque en `Warning` : message factuel, pas d'alarmisme.

## Garde-fous produit (§42) — inscrits dans la phase

- ❌ Jamais de « score santé » global chiffré inventé — uniquement des états Windows réels
  (`Healthy`/`Warning`) et des valeurs mesurées.
- ❌ Jamais « votre PC est en danger » : un `Warning` SMART affiche « Windows signale un risque sur ce
  disque — sauvegardez vos données », factuel.
- ❌ Le panneau CPU/RAM (option C) est **informatif seulement**, sans bouton « booster » ni « libérer la
  RAM ».

## Definition of Done

- Santé de base réelle affichée pour chaque disque physique (sans admin).
- Attributs SMART détaillés lus quand disponibles ; sinon `DETECTED_ONLY` documenté honnêtement dans
  `KNOWN_LIMITATIONS.md`.
- Impact au démarrage mesuré depuis le journal Windows, corrélé aux entrées, avec désactivation
  réversible.
- Zéro mock, zéro valeur inventée ; tests verts ; build Release 0 warning ; limites consignées.

## Dépendances & insertion

Réutilise la **Phase 10** (lecteurs / `IDriveQueryService`), la **Phase 12** (démarrage /
`IStartupService`) et la **Phase 20** (élévation, pour une opération read-only). Insertion recommandée
**après la Phase 7** (backup/restore), avant les grosses phases NTFS (8) et effacement sécurisé (9).
