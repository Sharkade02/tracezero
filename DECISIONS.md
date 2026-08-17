# DECISIONS (ADR simplifié)

Chaque décision technique structurante est notée ici : contexte, choix, alternatives, conséquences.

---

## ADR-0001 — Découpage de la solution en couches

- **Contexte** : le cahier de mission (§6) impose une architecture testable, DI, sans logique de
  nettoyage dans le code-behind XAML.
- **Décision** : découpage en couches :
  - `TraceZero.Domain` (net10.0) — modèles purs, aucune dépendance.
  - `TraceZero.Application` (net10.0) — interfaces de services, abstractions DI/logging.
  - `TraceZero.Engine` (net10.0) — moteur scan/clean, `ISafePathValidator`, moteur de règles.
  - `TraceZero.Windows` / `TraceZero.Storage` (net10.0-windows) — providers Windows (registre, WMI, drives).
  - `TraceZero.Browsers` (net10.0) — providers navigateurs.
  - `TraceZero.Persistence` (net10.0) — historique local (SQLite prévu).
  - `TraceZero.Updater` (net10.0) — updater signé.
  - `TraceZero.App` (net10.0-windows, WPF) — composition root uniquement.
- **Alternatives** : projet unique (rejeté : non testable, couplage UI/logique).
- **Conséquences** : `Domain`/`Engine` restent multiplateformes donc testables sans Windows ;
  les spécificités Windows sont isolées dans `Windows`/`Storage`.

## ADR-0002 — Cibles de framework

- **Décision** : `net10.0` pour les couches portables ; `net10.0-windows` pour `Windows`, `Storage`,
  `App` et les tests qui les référencent (`Windows.Tests`, `SafetyTests`, `IntegrationTests`).
- **Raison** : registre, WMI, API de volumes et WPF nécessitent la cible Windows ; garder le reste
  portable maximise la testabilité.

## ADR-0003 — MVVM via CommunityToolkit.Mvvm + Generic Host

- **Décision** : `CommunityToolkit.Mvvm` (source generators `ObservableObject`/`RelayCommand`) +
  `Microsoft.Extensions.Hosting` pour DI, configuration et logging.
- **Alternatives** : Prism / MVVMLight (plus lourds), pas de framework (plus de boilerplate).
- **Conséquences** : ViewModels résolus par le conteneur ; navigation par DI.

## ADR-0004 — Safety-first : `ISafePathValidator` avant tout code de suppression

- **Contexte** : §9 « Sécurité de suppression — critique ».
- **Décision** : poser dès la Phase 0 l'interface et une implémentation de `ISafePathValidator`,
  accompagnées d'une vraie suite `SafetyTests`, **avant** d'écrire la moindre opération destructive.
- **Conséquences** : aucune suppression n'est possible sans passer par la validation centrale ;
  les cas interdits (racine, profil utilisateur, dossiers protégés, reparse points, traversal) sont
  prouvés refusés par les tests dès le début.

## ADR-0005 — `WarningsAsErrors=nullable`

- **Décision** : les warnings de nullabilité sont traités comme des erreurs de build ;
  les autres warnings restent des warnings (pour ne pas bloquer le bootstrap).
- **Raison** : la nullabilité mal gérée est une source directe de bugs de sécurité (NRE lors d'une
  énumération/suppression). On la verrouille tôt.

## ADR-0006 — Élévation via helper séparé et commandes structurées (Phase 20)

- **Contexte** : §30 impose que l'application ne démarre jamais admin et que l'élévation passe par un
  helper séparé à surface minimale, ne faisant jamais confiance au client UI.
- **Décision** :
  - Projet dédié `TraceZero.Elevated.exe` (`net10.0-windows`) avec manifeste `requireAdministrator` ;
    l'app principale n'a **aucun** manifeste d'élévation.
  - **IPC par fichiers JSON** (request/response) plutôt que named pipes : le helper est *single-shot*
    (« s'arrête après action »), l'app le lance à la demande via `Process.Start` + verbe `runas`
    (invite UAC). Pas de canal permanent, pas de service, surface d'attaque minimale.
  - **Vocabulaire fermé** : le helper n'accepte qu'une `ElevatedRequest` (enum d'opérations connu).
    Aucun chemin arbitraire n'est transmis par l'UI ; le helper résout lui-même la liste
    d'autorisation dédiée à chaque opération (ex. `%SystemRoot%\Temp`).
  - **Autorité de sécurité distincte** : `ElevatedSafePathValidator` (et non `SafePathValidator`, qui
    refuse tout `C:\Windows`) — « refus par défaut », n'autorise que les **descendants stricts** d'une
    racine élevée listée, refuse traversal/wildcard/UNC/racine/point d'analyse.
- **Alternatives** : app relançée en admin (rejeté : viole « jamais admin par défaut », élève toute la
  surface UI) ; named pipe permanent (rejeté : canal persistant inutile pour du single-shot) ;
  passage de chemins depuis l'UI (rejeté : « ne jamais faire confiance au client », §30).
- **Conséquences** : le cœur (`ElevatedTempCleaner`, validateur, exécuteur) est portable/testable sans
  privilège ; ajouter une opération élevée future = étendre l'enum + sa liste d'autorisation dédiée,
  jamais ouvrir un chemin générique.

## ADR-0007 — Protection/Restauration : sauvegarde registre native + coffre SQLite (Phase 7)

- **Contexte** : §17 impose de sauvegarder les données modifiées « lorsque possible » avant une action
  sensible, de classer chaque action (Reversible / PartiallyReversible / Irreversible) et d'offrir
  « Restaurer les éléments disponibles », sans jamais prétendre restaurer un fichier effacé de façon
  sécurisée.
- **Décision** :
  - **Sauvegarde registre native** plutôt que `reg.exe export`/`import` : `RegistryBackupService` lit
    récursivement une sous-clé HKCU en un `RegistryKeySnapshot` portable (types encodés en texte/base64),
    et restaure par réécriture. Motif : **testable sans admin ni shell externe** (round-trip sur une clé
    HKCU de test), format maîtrisé et sérialisable (`RegistrySnapshotCodec`, JSON).
  - **HKCU uniquement, aucune élévation** : la restauration réécrit sous HKEY_CURRENT_USER ; la sous-clé
    est **fournie par l'appelant** (catalogue de traces autorisées), le service ne choisit jamais seul
    quoi sauvegarder — cohérent avec le « refus par défaut » du nettoyeur registre (ADR équivalent §43).
  - **Coffre = table SQLite dédiée** (`restore_points`) dans la base locale existante, séparée de
    l'historique anonymisé : le coffre doit, lui, conserver la charge utile (contenu registre) pour
    pouvoir restaurer. Ces données restent **locales**, jamais transmises — c'est le principe d'une
    sauvegarde.
  - **Réversibilité honnête** portée par `RestoreRecord.Reversibility` ; l'UI n'offre « Restaurer » que
    pour les éléments réversibles.
- **Alternatives** : `reg.exe` (rejeté : moins testable, dépend d'un process externe) ; point de
  restauration Windows comme mécanisme principal (rejeté : exige l'élévation, trop lourd pour une trace
  HKCU — reste `PLANNED` pour les modules Expert) ; stocker la charge utile en fichiers `.reg` séparés
  (rejeté : gestion de fichiers superflue, une colonne `payload` JSON suffit).
- **Conséquences** : la Phase 4 (nettoyage PRIVACY navigateurs : cookies/DB) réutilisera ce coffre et le
  motif capture-avant-modification. Ajouter un type restaurable = étendre `RestoreItemKind` + son
  applicateur, sans ouvrir de chemin générique.
