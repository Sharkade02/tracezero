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

- **`C:\Windows\Temp` et caches sous `C:\Windows`** — `PLANNED`. Volontairement exclus des règles par
  défaut : `ISafePathValidator` refuse tout chemin sous le répertoire Windows (sécurité). Leur
  nettoyage nécessitera une élévation (`TraceZero.Elevated.exe`, Phase 20) et une liste
  d'autorisation dédiée revalidée côté helper. En attendant, on ne cible que des emplacements sous le
  profil utilisateur (`AppData\Local`), tous autorisés par la validation.
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
