# Candidature SignPath Foundation — signature de code gratuite (OSS)

Objectif : obtenir la **signature de code Authenticode gratuite** pour TraceZero via le programme
**SignPath Foundation** (destiné aux projets open source), afin de supprimer l'avertissement
SmartScreen « Éditeur inconnu ».

> **Où postuler :** https://signpath.org/apply (formulaire du programme Foundation).
> **⚠️ À vérifier :** les critères exacts d'éligibilité évoluent — confirmez-les sur le site avant
> d'envoyer. Ce document est un **brouillon prêt à copier** dans le formulaire, pas une garantie
> d'acceptation.

## Comment SignPath fonctionne (à comprendre avant de postuler)

SignPath **ne fournit pas un fichier certificat** à installer. C'est un **service de signature intégré au
CI/CD** : votre pipeline (GitHub Actions) produit le binaire **non signé**, l'envoie à SignPath via son
action/API, et récupère le binaire **signé**. Cela garantit que ce qui est signé correspond au **code
source public** — d'où l'exigence d'un build automatisé et reproductible.

➡️ **Prérequis technique en place :** le workflow [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)
construit et teste en Release, et produit le paquet portable sur tag. L'emplacement exact de l'étape de
signature SignPath y est déjà indiqué (bloc commenté « SignPath signing goes here »).

## Éligibilité — auto-évaluation

| Critère typique | TraceZero |
|---|---|
| Licence open source (OSI) | ✅ **MIT** ([LICENSE](../LICENSE)) |
| Dépôt source public | ✅ https://github.com/Sharkade02/tracezero |
| Application réelle et utile (pas une démo) | ✅ Nettoyage/confidentialité/espace disque Windows, release publique v0.1.1 |
| Build automatisé / CI vérifiable | ✅ GitHub Actions (`ci.yml`) |
| Pas de contenu malveillant / conforme | ✅ Local-first, zéro télémétrie, zéro pub, code auditable |
| Notoriété / adoption | ⚠️ **Projet récent** (peu d'étoiles/downloads pour l'instant) — voir caveat |

**Caveat honnête :** SignPath Foundation privilégie souvent des projets ayant une certaine **traction**
(étoiles, téléchargements, activité). Un dépôt tout neuf peut être **mis en attente** jusqu'à ce qu'il
gagne en adoption. Si c'est refusé pour ce motif, ce n'est pas un « non » définitif : reposter après
quelques semaines/mois d'usage. En attendant, la distribution non signée + SHA-256 + explication
SmartScreen (déjà en place) reste la solution.

## Brouillon de candidature (à coller dans le formulaire)

**Project name:** TraceZero

**Repository:** https://github.com/Sharkade02/tracezero

**License:** MIT

**Short description:**
Local-first, privacy-first Windows cleaner, privacy inspector and disk-space manager — no ads, no dark
patterns. A real CCleaner alternative.

**What does the project do / who uses it:**
TraceZero is a Windows desktop app (.NET 10, WPF) for safe cleaning, privacy-trace inspection, disk-space
management, duplicate finding, secure erase and system health. It is fully local (no telemetry, no data
leaves the machine), never runs as administrator, and never deletes anything without an explicit user
choice validated by a safety layer. It is distributed as a portable download and via winget.

**Why do you need code signing:**
The app is distributed directly (outside the Microsoft Store). Because it is unsigned, Windows SmartScreen
shows an "Unknown publisher" warning on first launch, which deters legitimate users. Authenticode signing
would establish publisher trust and remove that friction. As a free, donation-supported open-source
project, a commercial certificate is not financially sustainable — hence this application.

**Build & release process:**
Reproducible build via GitHub Actions (`.github/workflows/ci.yml`): `dotnet build -c Release` (0 warnings)
+ `dotnet test -c Release`, then a portable self-contained package (`build/scripts/publish-portable.ps1`)
produced on version tags, with a published SHA-256. We would integrate SignPath's signing step into this
pipeline (the placeholder is already in the workflow).

**Artifacts to sign:**
`TraceZero.App.exe`, `TraceZero.Elevated.exe` (and the portable `.zip` / future MSI-EXE installer).

**Maintainer:** Sharkade02 — https://github.com/Sharkade02 — contact via GitHub issues.

## Étapes concrètes

1. **Pousser le workflow CI** (ce commit) → vérifier qu'il passe vert dans l'onglet **Actions** du dépôt.
2. Vérifier les **critères actuels** sur https://signpath.org/apply.
3. **Envoyer la candidature** avec le brouillon ci-dessus.
4. **Si accepté :** SignPath crée une organisation + une *signing policy* ; on décommente et configure le
   bloc SignPath dans `ci.yml` (secrets `SIGNPATH_API_TOKEN` + variable `SIGNPATH_ORG_ID`), puis les
   releases suivantes sortent **signées** (plus d'avertissement SmartScreen).
5. **Si mis en attente (traction) :** continuer la distribution non signée actuelle, reposter plus tard.

## Alternative si SignPath n'aboutit pas

- **Certum Open Source Code Signing** (~70–100 €/an) — cert OSS peu cher, à financer par les dons.
- **Azure Trusted Signing** (~10 $/mois) si éligible.
- Voir [`docs/distribution-strategy.md`](distribution-strategy.md) pour l'arbitrage coût/traction.
