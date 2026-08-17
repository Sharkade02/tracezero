# Phase 4 — Nettoyage PRIVACY/REVIEW des navigateurs : plan & évaluation de risque

> **But** : compléter la Phase 4 (§14) au-delà des caches SAFE (déjà livrés) par le nettoyage
> **PRIVACY** (historique, téléchargements, formulaires…) et **REVIEW** (cookies, sessions, stockage local).
> **Contrainte absolue** : « Ne jamais modifier une base navigateur en cours d'utilisation sans stratégie
> sûre » ; « Conserver mes connexions » activé par défaut ; jamais toucher favoris ni mots de passe.

## 1. Ce qui existe déjà (SAFE — livré)

- `BrowserDetector` : détecte Chrome/Edge/Brave/Vivaldi/Chromium/Firefox, profils, profil par défaut,
  **état d'exécution** (`IsRunning`).
- `BrowserCacheScanProvider` : nettoie les **caches régénérables** (disk/GPU/code/shader caches, Service
  Worker) via le pipeline sûr. Connexions préservées par construction.
- Infra **Phase 7** (`IRegistryBackupService`/`IProtectionVault`) : sauvegarde/restauration — extensible
  à des **fichiers**.

## 2. Données concernées et emplacements

### Chromium (Chrome/Edge/Brave/Vivaldi/Chromium) — un fichier par usage
| Donnée | Fichier (dans le profil) | Catégorie | Consequence si erreur |
|---|---|---|---|
| Historique + téléchargements | `History` (SQLite **autonome**) | PRIVACY | Perte de l'historique (regénéré vide) |
| Formulaires / autofill | `Web Data` (SQLite) | PRIVACY | Perte autofill |
| Cookies / sessions auth | `Network\Cookies` (SQLite) | REVIEW | **Déconnexion des sites** |
| Sessions | dossier `Sessions`, `Current Session/Tabs` | REVIEW | Perte des onglets restaurés |
| Local/Session Storage | `Local Storage`, `Session Storage` (LevelDB) | REVIEW | Perte de données de sites |
| **Favoris** | `Bookmarks` (JSON) | **NE JAMAIS TOUCHER** | Perte des favoris |
| **Mots de passe** | `Login Data` (SQLite) | **NE JAMAIS TOUCHER** | Perte des identifiants |

### Firefox (Gecko) — ⚠️ historique ET favoris dans le MÊME fichier
| Donnée | Fichier | Catégorie | Consequence si erreur |
|---|---|---|---|
| Historique **+ favoris** | `places.sqlite` | PRIVACY (histo) | **Effacer l'historique = risque de perdre les favoris** |
| Cookies | `cookies.sqlite` | REVIEW | Déconnexion |
| Formulaires | `formhistory.sqlite` | PRIVACY | Perte autofill |
| Sessions | `sessionstore.jsonlz4`, `sessionstore-backups\` | REVIEW | Perte onglets |
| **Mots de passe** | `key4.db`, `logins.json` | **NE JAMAIS TOUCHER** | Perte identifiants |

## 3. Les risques (pourquoi c'est la fonctionnalité la plus dangereuse du produit)

1. **Corruption sur base ouverte** : Chromium/Firefox gardent leurs SQLite ouverts en mode **WAL**
   (fichiers `-wal`/`-shm`). Modifier pendant que le navigateur tourne peut **corrompre** la base ou être
   écrasé. → exige navigateur **fermé** + gestion des fichiers annexes.
2. **Dérive de schéma** : les schémas SQLite évoluent selon la version du navigateur. Du SQL au niveau
   ligne (`DELETE FROM urls`) peut casser (déclencheurs, clés étrangères, colonnes renommées) → base
   incohérente. Nécessite une **validation par version réelle** (Phase 26 VM).
3. **Firefox `places.sqlite` = historique + favoris** : la moindre erreur d'effacement d'historique peut
   **détruire les favoris**. Conséquence irréversible et très grave.
4. **Proximité des mots de passe/favoris** dans le même dossier de profil : un bug de chemin pourrait
   toucher `Login Data`/`Bookmarks`.
5. **TOCTOU** : le navigateur peut se (re)lancer entre la vérification « fermé » et l'opération.
6. **Gestionnaire de cookies « intelligent »** (§14) : lire la base Cookies, lister les domaines, allowlist,
   suppression **chirurgicale** `WHERE host_key NOT IN (...)` → dépend du schéma, corruption possible.

## 4. Options de conception

### Option A — Fichier entier, navigateur fermé, sauvegardé (risque MODÉRÉ)
- Cibler **uniquement des fichiers autonomes** dont la suppression est « propre » et que le navigateur
  **régénère** : Chromium `History`, `Web Data` (+ `-wal`/`-shm`). **Jamais** `Bookmarks`/`Login Data`.
- **Firefox `places.sqlite` : EXCLU** (contient les favoris) → `DETECTED_ONLY` documenté.
- Pré-conditions strictes : navigateur **fermé** (sinon on demande la fermeture, on ne force jamais),
  **backup fichier** via l'infra Phase 7 (copie horodatée dans le coffre, restaurable), **réversible**.
- « Conserver mes connexions » (défaut ON) = on ne touche **ni cookies ni sessions ni mots de passe**.
- Cookies/sessions/local storage : **REVIEW**, non cochés par défaut ; si l'utilisateur les sélectionne,
  suppression du fichier entier (Cookies) après fermeture + backup — **déconnexion assumée et annoncée**.
- **Testable** sur profils **synthétiques** Chromium (DoD §14) : créer un faux profil avec `History`,
  `Bookmarks`, `Login Data`, vérifier que History part et que Bookmarks/Login Data restent.

### Option B — SQL chirurgical (row-level) + gestionnaire de cookies (risque ÉLEVÉ)
- Ouvrir les bases avec `Microsoft.Data.Sqlite`, `DELETE` sélectif (garder favoris Firefox, allowlist
  cookies). Précision maximale **mais** fragilité schéma + corruption + dépendance version.
- **Non recommandé** sans batterie de tests sur profils réels multi-versions en VM (Phase 26).

## 5. Recommandation

**Ne PAS implémenter maintenant** l'Option B (SQL chirurgical, gestionnaire de cookies intelligent,
historique Firefox) : le rapport risque/bénéfice est défavorable pour un produit *safety-first* tant que
la **validation VM (Phase 26)** n'est pas faite — le risque de corruption ou de perte de favoris/mots de
passe est réel.

**Envisageable** avec prudence : l'Option A **limitée à l'historique Chromium** (fichiers autonomes,
navigateur fermé, backup réversible, favoris/mots de passe jamais touchés, Firefox en `DETECTED_ONLY`).
Même ainsi, elle **ne devrait être activée** qu'après tests sur profils synthétiques **et** validation VM.

**Décision par défaut (proposée)** : **on ne fait pas** le nettoyage PRIVACY/REVIEW pour l'instant. On
conserve la posture honnête actuelle (caches SAFE nettoyés, le reste `DETECTED_ONLY` avec explication),
et on garde ce plan prêt pour une future itération encadrée par la Phase 26.
