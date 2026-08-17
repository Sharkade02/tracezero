# Télécharger TraceZero

TraceZero est un logiciel Windows de **nettoyage / confidentialité / espace disque**, **local-first** et
**privacy-first** : tout se passe sur votre PC, rien n'est envoyé sur Internet, aucune valeur n'est inventée,
et rien de sensible n'est supprimé sans votre choix explicite.

> Cette page est destinée à être publiée telle quelle (GitHub Pages ou section README). Remplacez
> `<VERSION>` et `<SHA256_DU_ZIP>` à chaque release.

## Téléchargement

- **Version portable (recommandée, aucune installation)** :
  [`TraceZero-portable.zip`](https://github.com/Sharkade02/tracezero/releases/latest) — décompressez,
  lancez `TraceZero.App.exe`. Les données restent dans le dossier (`Data\`), rien n'est écrit ailleurs.
- Ou via **winget** : `winget install TraceZero.TraceZero`

## Vérifier l'intégrité (recommandé)

Chaque release publie l'empreinte **SHA-256** du fichier. Comparez-la après téléchargement :

```powershell
Get-FileHash .\TraceZero-portable.zip -Algorithm SHA256
```

Le résultat doit être **exactement** :

```
<SHA256_DU_ZIP>
```

Si l'empreinte diffère, **ne lancez pas** le fichier (téléchargement corrompu ou altéré).

## ⚠️ « Windows a protégé votre PC » — pourquoi, et que faire

Au premier lancement, Windows SmartScreen peut afficher **« Windows a protégé votre PC »** avec la mention
**« Éditeur inconnu »**. C'est **normal** et **honnête de notre part de l'expliquer** :

- TraceZero est distribué **directement** (hors Microsoft Store) et n'est **pas encore signé** par un
  certificat de code payant. SmartScreen affiche cet avertissement pour **tout** logiciel non signé, sans
  rapport avec sa dangerosité.
- Nous **ne pouvons pas** « faire disparaître » cet écran sans acheter un certificat de signature (coût
  annuel récurrent). Nous préférons être transparents plutôt que de vous demander de désactiver votre
  antivirus.
- **Votre protection reste entière** : vérifiez l'empreinte SHA-256 ci-dessus (elle prouve que le fichier
  est bien celui que nous avons publié), puis, si vous le souhaitez :

  1. Cliquez sur **« Informations complémentaires »**.
  2. Cliquez sur **« Exécuter quand même »**.

Vous pouvez aussi analyser le fichier sur [VirusTotal](https://www.virustotal.com/) avant de l'exécuter.

> Transparence : nous n'ajoutons jamais de publicité, de dark pattern, ni de télémétrie. Le code de
> vérification des mises à jour n'exécute **jamais** une mise à jour dont la signature n'est pas valide.

## Soutenir le projet

TraceZero est gratuit et sans publicité. Si vous le trouvez utile, vous pouvez soutenir son développement
via l'onglet **Soutenir** dans l'application (paiement au prix que vous voulez). Aucune fonctionnalité n'est
bloquée derrière un paiement.
