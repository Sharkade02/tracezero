# Runbook de release — Étape 0 (hors-Store, budget 0 €)

Procédure concrète pour publier une version de TraceZero **gratuitement**, avec intégrité vérifiable et
auto-update signé. Voir `docs/distribution-strategy.md` pour la stratégie et le « quand payer ».

## Pré-requis (une seule fois)

1. **Clés updater** : `bash build/updater/new-updater-key.sh .`
   - Sauvegardez `updater.private.pem` **hors dépôt** (gestionnaire de secrets / coffre).
   - Collez `updater.public.pem` dans `src/TraceZero.App/Services/UpdaterConfig.cs` → `PublicKeyPem`,
     et renseignez `ManifestUrl` (URL où sera hébergé `manifest.json`).
2. Compte GitHub (dépôt `Sharkade02/tracezero`) et, optionnellement, un fork de `microsoft/winget-pkgs`.

## À chaque version

1. **Bump de version** dans `Directory.Build.props` (`<Version>`).
2. **Build + tests + empreintes** :
   ```powershell
   pwsh build/scripts/release.ps1        # build Release 0 warning, tests, publish, SHA256SUMS.txt
   ```
3. **Paquet portable** :
   ```powershell
   pwsh build/scripts/publish-portable.ps1
   ```
   Produit `artifacts/portable/TraceZero-portable.zip`.
4. **Empreinte du zip** :
   ```powershell
   (Get-FileHash artifacts/portable/TraceZero-portable.zip -Algorithm SHA256).Hash
   ```
5. **GitHub Release** (tag `v<VERSION>`) : joindre `TraceZero-portable.zip` et `SHA256SUMS.txt`.
6. **Manifeste d'update signé** :
   ```bash
   bash build/updater/sign-manifest.sh \
     --key updater.private.pem \
     --version <VERSION> --channel stable \
     --url https://github.com/Sharkade02/tracezero/releases/download/v<VERSION>/TraceZero-portable.zip \
     --sha256 <SHA256_DU_ZIP> --min <MIN_VERSION> --out manifest.json
   ```
   Publiez `manifest.json` à l'URL de `UpdaterConfig.ManifestUrl` (asset de release ou GitHub Pages).
7. **Page de téléchargement** : mettre à jour `docs/download.md` (version + SHA-256) et la publier
   (GitHub Pages) ou l'intégrer au README.
8. **winget** (facultatif mais recommandé) : dans les gabarits `build/winget/*.yaml`, remplacer `<VERSION>`
   et `<SHA256_DU_ZIP>`, puis ouvrir une PR sur `microsoft/winget-pkgs` sous
   `manifests/t/TraceZero/TraceZero/<VERSION>/`. (Outil pratique : `wingetcreate`.)

## Vérifications avant publication

- [ ] `release.ps1` : **0 avertissement**, tous les tests verts.
- [ ] SHA-256 du zip identique dans : release GitHub, `docs/download.md`, `manifest.json`, winget.
- [ ] `sign-manifest.sh` affiche « signature valide » (auto-vérifiée).
- [ ] `UpdaterConfig` renseigné (clé publique + URL) **uniquement quand** le manifeste est en ligne.
- [ ] Page download : avertissement SmartScreen honnête + hash à jour.

## Quand passer à la signature payante

Voir `docs/distribution-strategy.md` §4. Résumé : **rester gratuit** (warning SmartScreen assumé) jusqu'à ce
que les dons financent ≥ 12 mois de certificat. Envisager l'open source pour une signature gratuite
(SignPath Foundation) ou peu chère (Certum OSS).
