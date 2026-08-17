# Stratégie de distribution hors-Store — TraceZero

> But : distribuer TraceZero de façon **crédible et sûre** avec un **budget initial de 0 €**, puis ne
> dépenser (signature de code) **qu'une fois une adoption réelle constatée**. Adapté à un modèle de
> rémunération **par dons (PWYW)** où rien ne garantit de récupérer une avance de trésorerie.

Statut : recommandation. Décisions financières à valider par l'utilisateur (Sébastien).

---

## 0. La règle économique (le cœur de la décision)

**Ne jamais engager un coût récurrent avant d'avoir la preuve de la demande.** Avec un revenu par dons,
un certificat OV à ~200–450 €/an est un pari à fonds perdu tant que personne n'utilise le logiciel.

La séquence saine inverse l'ordre habituel :

```
Adoption  →  Dons  →  Certificat        (BON : le coût est financé par ce qu'il débloque)
Certificat →  Adoption →  Dons          (RISQUÉ : avance à fonds perdu, revenu incertain)
```

**Conclusion : on expédie d'abord gratuitement, on mesure, et la signature payante n'arrive que si les
dons la financent.** Voir §4 (stratégie étagée).

---

## 1. Deux « signatures » à ne pas confondre

| | Ce que c'est | Coût | État TraceZero |
|---|---|---|---|
| **Signature de mise à jour** (RSA-SHA256, clé publique embarquée) | Prouve que le **manifeste d'update** vient bien de nous ; empêche une fausse MAJ | **0 €** (PKI maison) | ✅ **déjà implémentée** (`UpdateChecker`, Phase 18) |
| **Signature de code Authenticode** (certificat AC) | Fait que **Windows fait confiance à l'installeur/l'exe** (SmartScreen, « Éditeur : … » au lieu de « Éditeur inconnu ») | **payant** (voir §5) | ❌ nécessite un certificat externe |

⇒ La sécurité *technique* de l'auto-update ne dépend **pas** d'un certificat payant. Le certificat ne sert
qu'à la **confiance de l'OS à l'installation**. C'est une dépense **marketing/adoption**, pas de sécurité.

---

## 2. Pourquoi hors-Store (rappel)

Le Microsoft Store restreint fortement les « system utility / cleaner / optimizer » (**policy 10.2.1**) ;
Microsoft a déjà refusé/retiré ce type d'app. L'admission n'est **pas garantie**, donc le Store ne peut pas
être le canal principal. La distribution directe + winget n'a pas ces restrictions et reste sous notre
contrôle. Le Store, si un jour admissible, sera un **bonus** — jamais un pré-requis.

---

## 3. Le vrai obstacle hors-Store : SmartScreen

À l'exécution d'un `.exe`/`.msi` téléchargé, Windows Defender SmartScreen affiche un avertissement selon la
**réputation** du binaire :

- **Non signé** → « Windows a protégé votre PC / Éditeur inconnu ». L'utilisateur doit cliquer
  *Informations complémentaires → Exécuter quand même*. Friction forte, beaucoup abandonnent.
- **Signé OV** → l'avertissement **s'atténue avec le temps** (à mesure que le binaire signé accumule des
  téléchargements « propres »). Réputation qui se **construit**.
- **Signé EV** → réputation **immédiate**, pas d'avertissement dès le premier téléchargement.

On ne peut pas « tricher » : la réputation est liée au certificat (ou, très lentement, au hash du binaire).

**Atténuations gratuites** qui n'éliminent pas le warning mais rassurent l'utilisateur avisé :
- Publier les **empreintes SHA-256** (déjà produites par `build/scripts/release.ps1` →
  `artifacts/SHA256SUMS.txt`) pour vérifier l'intégrité du téléchargement.
- Page « Pourquoi cet avertissement ? » honnête sur le site/README, avec la procédure et le hash attendu.
- Build **reproductible** documentée (permet à un tiers de recréer le binaire et comparer le hash).

---

## 4. Stratégie étagée (le coût suit la traction)

### Étape 0 — Lancement, budget 0 € (maintenant)
- **Canal** : **GitHub Releases** (portable `.zip` self-contained via `publish-portable.ps1`, déjà prêt) +
  `SHA256SUMS.txt`.
- **Signature** : aucune (ou chemin OSS gratuit, voir §5) → assumer le warning SmartScreen, l'expliquer.
- **Découverte** : soumettre à **winget** (`microsoft/winget-pkgs`, gratuit) — l'app devient installable via
  `winget install TraceZero`. (winget valide la soumission ; l'exécution reste soumise à SmartScreen, mais
  l'installation par winget est perçue comme plus fiable et automatisable.)
- **Auto-update** : déjà signé (RSA), endpoint = un fichier `manifest.json` hébergé **gratuitement**
  (GitHub Pages / release asset). Aucun serveur payant requis.
- **Objectif** : mesurer les téléchargements, les retours, les **premiers dons**. **Coût : 0 €.**

### Étape 1 — Traction confirmée (premiers utilisateurs / premiers dons)
- Acheter une signature **abordable** (Certum Open Source ~70–100 €/an, ou **Azure Trusted Signing**
  ~10 $/mois si éligible — voir §5), **financée par les dons** de l'étape 0.
- Signer App + Elevated + installeur + updater ; la réputation OV commence à se construire.
- Ajouter un **installeur MSI/EXE** (WiX) signé en plus du portable.

### Étape 2 — Projet établi (flux de dons régulier)
- Passer éventuellement à un certificat **EV** (réputation immédiate) si le volume le justifie.
- (Optionnel) Tenter le Microsoft Store en connaissant le risque 10.2.1, en gardant la distribution directe
  comme canal principal.

> **Décision par défaut recommandée : rester en Étape 0 jusqu'à ce que les dons couvrent au moins 12 mois
> de certificat d'avance.** On ne prend pas de risque financier ; on laisse l'usage décider.

---

## 5. Options de signature comparées

| Option | Coût indicatif* | Réputation SmartScreen | Éligibilité | Verdict |
|---|---|---|---|---|
| **Non signé** | 0 € | ❌ warning permanent | — | OK pour démarrer, friction assumée |
| **SignPath Foundation** | **0 €** | ✅ (OV) | Projets **open source** éligibles | Idéal si TraceZero passe en OSS |
| **Certum Open Source Code Signing** | ~70–100 €/an* | ✅ (OV, se construit) | Développeurs **open source**, ID vérifiée | Le moins cher des payants crédibles |
| **Azure Trusted Signing** | ~10 $/mois* (~120 $/an) | ✅ (bonne) | Individus/orgs **avec vérification d'identité** (orgs souvent ≥ 3 ans) | Bon rapport qualité/prix **si éligible** |
| **Certificat OV standard** (Sectigo/DigiCert…) | ~200–450 €/an* | ✅ se construit avec le temps | Tout le monde (ID vérifiée) | Solide mais cher pour un projet à dons |
| **Certificat EV** | ~300–700 €/an* | ✅ **immédiate** | ID renforcée, souvent token matériel | Seulement si volume élevé |

\* *Tarifs indicatifs à vérifier au moment de l'achat (peuvent changer).*

**Chemin gratuit le plus crédible : passer TraceZero en open source et demander SignPath Foundation** (cert
OV gratuit pour l'OSS) **ou Certum OSS** (très bon marché). C'est cohérent avec le positionnement
« local-first, privacy-first, zéro dark pattern » : l'open source **renforce** la confiance ET débloque la
signature gratuite/quasi-gratuite. À arbitrer avec l'utilisateur (choix de licence, exposition du code).

---

## 6. Canaux de distribution (tous gratuits)

1. **GitHub Releases** — source de vérité des binaires + `SHA256SUMS.txt`. Le portable `.zip` est déjà
   produit par `build/scripts/publish-portable.ps1`.
2. **winget** (`microsoft/winget-pkgs`) — soumission d'un manifeste pointant vers la release GitHub.
   Gratuit, large portée, `winget upgrade` intégré (cohérent avec la Phase 13 qui utilise déjà winget).
3. **Site / page projet** — GitHub Pages (gratuit) : présentation, bouton de téléchargement, page
   « avertissement SmartScreen » honnête, hash attendu, lien dons (PWYW, Phase 17).
4. **(Plus tard) MSI/EXE signé** — pour les utilisateurs qui préfèrent un installeur classique.

---

## 7. Auto-update hors-Store (déjà en place, à finaliser sans coût)

- `UpdateChecker` vérifie au démarrage un **manifeste JSON signé RSA-SHA256** (clé publique embarquée) et
  n'exécute **jamais** un manifeste invalide. **La partie sécurité est faite et gratuite.**
- **Hébergement de l'endpoint = gratuit** : publier `manifest.json` + le binaire en **asset de release
  GitHub** ou sur **GitHub Pages**. Aucun serveur à louer.
- **Reste (dépend de la signature payante, donc Étape 1+)** : vérification **Authenticode** du binaire
  téléchargé + éditeur attendu avant exécution. Tant qu'on n'est pas signé, la MAJ peut se limiter à
  « télécharger + vérifier SHA-256 + ouvrir le dossier / lancer l'installeur » (l'utilisateur valide),
  ce qui reste honnête et sûr.

---

## 8. Checklist de mise en ligne (Étape 0, 0 €)

- [ ] `release.ps1` → build Release 0 warning + tests + `SHA256SUMS.txt`.
- [ ] `publish-portable.ps1` → `.zip` portable self-contained.
- [ ] Créer une **GitHub Release** (tag versionné) : joindre `.zip` + `SHA256SUMS.txt`.
- [ ] Publier `manifest.json` (updater) en asset de release ou sur GitHub Pages ; renseigner `UpdaterConfig`.
- [ ] Page/README : présentation, **procédure SmartScreen** honnête, **hash attendu**, lien **dons** (PWYW).
- [ ] Soumettre le **manifeste winget** pointant vers la release.
- [ ] (Optionnel, recommandé) Décider de l'**open source** → demander **SignPath Foundation** / **Certum OSS**
      pour une signature gratuite/peu chère.
- [ ] Mesurer : téléchargements, retours, **dons**. Passer à l'Étape 1 **seulement** si les dons financent
      le certificat.

---

## 9. Décision recommandée (résumé)

1. **Expédier maintenant, gratuitement**, via GitHub Releases + winget, warning SmartScreen assumé et
   expliqué, intégrité garantie par SHA-256, auto-update sécurisé déjà en place.
2. **Envisager l'open source** pour débloquer une signature **gratuite (SignPath)** ou **quasi-gratuite
   (Certum OSS)** — double bénéfice : confiance accrue + coût quasi nul.
3. **N'acheter un certificat payant qu'après** un signal d'adoption/dons clair, en le finançant par ces
   dons. **Aucune avance de trésorerie à risque.**
4. Le **Microsoft Store** reste optionnel et incertain (policy 10.2.1) : ne pas investir dans l'empaquetage
   Store tant que la distribution directe n'a pas prouvé la demande.
