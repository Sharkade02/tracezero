# Updater — clés & manifeste signé (gratuit, Étape 0)

Outils pour publier une mise à jour **signée** sans aucun coût : la signature du **manifeste** utilise une
paire de clés RSA que vous générez (PKI maison), indépendante de tout certificat Authenticode payant.

> Sécurité vérifiée : les manifestes produits par `sign-manifest.sh` sont acceptés par le vrai
> `UpdateChecker` (résultat `UpdateAvailable`) et un manifeste altéré est rejeté (`ManifestInvalid`).

## 1. Générer la paire de clés (une seule fois)

```bash
bash build/updater/new-updater-key.sh .
```

- `updater.private.pem` — **clé privée, SECRÈTE**. Sauvegardez-la hors du dépôt. Ne jamais committer
  (déjà couverte par `.gitignore` : `*.private.pem`).
- `updater.public.pem` — **clé publique**. Copiez son contenu dans
  `src/TraceZero.App/Services/UpdaterConfig.cs` → `PublicKeyPem`, et renseignez `ManifestUrl`
  (URL HTTPS où le `manifest.json` sera hébergé). Tant que ces deux champs sont vides, l'updater est
  **désactivé** (aucune vérification réseau).

## 2. Signer un manifeste à chaque release

Après avoir publié le `.zip` portable et calculé son SHA-256 (voir `artifacts/SHA256SUMS.txt` produit par
`build/scripts/release.ps1`) :

```bash
bash build/updater/sign-manifest.sh \
  --key updater.private.pem \
  --version 0.2.0 \
  --channel stable \
  --url https://github.com/Sharkade02/tracezero/releases/download/v0.2.0/TraceZero-portable.zip \
  --sha256 <SHA256_DU_ZIP> \
  --min 0.1.0 \
  --out manifest.json
```

Le script écrit `manifest.json` **et vérifie lui-même la signature** avant de sortir. Publiez ensuite
`manifest.json` à l'URL configurée dans `UpdaterConfig.ManifestUrl` (par ex. un asset de release GitHub ou
une page GitHub Pages — hébergement **gratuit**).

## 3. Détail du contenu signé (pour audit)

La signature couvre exactement (séparateurs `\n`, UTF-8, sans saut de ligne final) :

```
{Version}
{Channel}
{Url}
{Sha256}
{MinimumSupportedVersion ou chaîne vide}
```

C'est la définition de `UpdateChecker.SignedPayload`. Toute altération d'un de ces champs invalide la
signature et le manifeste est refusé — jamais exécuté.

## Rappel : deux signatures distinctes

- **Ici (manifeste RSA)** = authenticité des mises à jour. **Gratuit**, fait.
- **Authenticode (certificat)** = confiance de Windows/SmartScreen à l'installation. **Payant**, à n'acheter
  qu'après traction. Voir `docs/distribution-strategy.md`.
