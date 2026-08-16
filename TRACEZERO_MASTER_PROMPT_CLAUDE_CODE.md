# TRACEZERO — MASTER BUILD PROMPT FOR CLAUDE CODE

> **But** : construire un logiciel Windows de nettoyage, confidentialité, gestion d'espace disque et maintenance qui soit réellement publiable, fiable et crédible face à CCleaner et PrivaZer.
>
> **Nom de travail** : TraceZero  
> **Plateforme cible** : Windows 10 / Windows 11, x64 en priorité  
> **Stack imposée** : .NET 10 + WPF + MVVM  
> **Langue de développement** : anglais dans le code, français dans l'UI par défaut, architecture prête pour EN/DE/ES  
> **Mode de distribution** : téléchargement direct signé + Microsoft Store (MSIX) à terme  
> **Philosophie** : local-first, privacy-first, zéro publicité, zéro dark pattern, aucune promesse mensongère d'optimisation.
>
> Ce fichier est le cahier de mission principal. Il doit être traité comme la source de vérité du projet.

---

# 0. RÈGLE ABSOLUE : CE N'EST PAS UNE DÉMO

Tu dois construire un **vrai logiciel complet**, pas une maquette WPF, pas un prototype dont les boutons ne font rien, pas une moitié de produit.

Le projet doit être développé en plusieurs phases, mais **les phases sont des jalons de construction, pas une réduction de périmètre**.

À la fin :

- l'application doit être installable ;
- l'application doit scanner réellement ;
- l'application doit nettoyer réellement ;
- chaque action destructive doit être sécurisée ;
- les navigateurs doivent être réellement détectés ;
- les éléments affichés doivent correspondre à ce qui est réellement trouvé ;
- les tailles doivent être calculées sur les fichiers réels ;
- les historiques doivent être persistés ;
- les exclusions doivent être respectées ;
- l'annulation / protection doit être implémentée lorsque possible ;
- les fonctions avancées prévues dans ce document doivent exister ;
- aucun bouton ne doit être décoratif ;
- aucun résultat ne doit être mocké ;
- aucun écran ne doit afficher de valeur inventée ;
- aucun `TODO`, `NotImplementedException`, faux service ou stub ne doit subsister dans une phase marquée `DONE`.

Si une fonctionnalité est trop dangereuse pour être implémentée de façon fiable, ne simule jamais son fonctionnement. Implémente d'abord une version read-only honnête, documente précisément la limitation dans `PHASE_STATUS.md`, puis poursuis la recherche vers une méthode Windows supportée.

---

# 1. OBJECTIF PRODUIT

TraceZero doit réunir les forces des meilleurs outils du marché tout en améliorant fortement leur UX.

## Positionnement

TraceZero n'est pas un « PC booster miracle ».

TraceZero est :

1. un nettoyeur Windows sûr ;
2. un inspecteur de confidentialité ;
3. un nettoyeur de navigateurs ;
4. un gestionnaire d'espace disque ;
5. un outil d'effacement sécurisé adapté au type de stockage ;
6. un détecteur de doublons ;
7. un gestionnaire d'applications / démarrage ;
8. un outil de maintenance explicable ;
9. un logiciel local-first et respectueux de la vie privée.

Tagline de travail :

> **See what's stored. Clean what you choose.**

En français :

> **Voyez ce qui reste. Nettoyez ce que vous choisissez.**

---

# 2. BENCHMARK À ÉGALER OU DÉPASSER

Le benchmark fonctionnel 2026 comprend notamment :

## CCleaner

Fonctions pertinentes à égaler ou dépasser :

- Health Check / scan simplifié ;
- Custom Clean ;
- nettoyage Windows ;
- nettoyage navigateurs ;
- nettoyage applications ;
- gestion du démarrage ;
- désinstallation ;
- recherche de doublons ;
- analyse disque / fichiers volumineux ;
- Software Updater ;
- automatisation / Smart Cleaning ;
- historique / maintenance ;
- outils système ;
- optimisation compréhensible.

## PrivaZer

Fonctions pertinentes à égaler ou dépasser :

- nettoyage profond des traces d'activité ;
- scans de confidentialité Windows ;
- traces navigateurs ;
- gestion intelligente des cookies ;
- traces liées aux fichiers récemment ouverts ;
- Jump Lists ;
- UserAssist ;
- Shellbags ;
- RecentDocs ;
- Thumbnail/Icon caches ;
- traces DNS ;
- traces de périphériques ;
- analyse de traces persistantes ;
- prise en compte de NTFS / MFT / USN dans le mode Expert ;
- analyse de l'espace libre ;
- effacement sécurisé ;
- distinction HDD / SSD ;
- nettoyage automatique ;
- fonctionnement portable ;
- zéro tracking / zéro télémétrie / zéro publicité.

## Différence TraceZero

TraceZero doit être meilleur sur :

- la lisibilité ;
- la sécurité ;
- la pédagogie ;
- le design ;
- la prévisualisation ;
- l'explication de chaque trace ;
- la classification du risque ;
- la protection des sessions utilisateur ;
- la transparence sur ce qui sera supprimé ;
- la capacité d'annuler ou restaurer ce qui peut l'être ;
- le modèle Supporter sans abonnement forcé.

---

# 3. PRINCIPES UX NON NÉGOCIABLES

Toute l'application doit suivre ces règles.

## 3.1 Trois niveaux de risque

Chaque élément scanné possède une classification :

### SAFE
Peut être supprimé sans impact fonctionnel normalement attendu.

Exemples :

- caches ;
- fichiers temporaires ;
- crash dumps anciens ;
- miniatures régénérables ;
- fichiers temporaires d'installation explicitement identifiés.

### PRIVACY
La suppression est généralement sûre mais supprime une trace ou un historique.

Exemples :

- RecentDocs ;
- RunMRU ;
- Jump Lists ;
- historiques navigateurs ;
- recherches récentes.

### REVIEW
Peut supprimer une information souhaitée par l'utilisateur ou modifier son expérience.

Exemples :

- Corbeille ;
- téléchargements anciens ;
- cookies ;
- sessions ;
- fichiers volumineux ;
- données applicatives secondaires.

Cette classification doit être exposée par le moteur, pas calculée dans l'UI.

---

## 3.2 Nettoyage recommandé

L'utilisateur standard doit pouvoir lancer :

> **Analyser mon PC**

puis voir :

- espace récupérable ;
- Sans risque ;
- Confidentialité ;
- À vérifier.

Le bouton principal :

> **Nettoyer maintenant**

ne doit sélectionner automatiquement que les catégories sûres et les catégories privacy explicitement autorisées par le profil.

Aucun élément `REVIEW` ne doit être sélectionné silencieusement.

---

## 3.3 Simulation avant suppression

Avant une suppression importante, afficher un résumé :

> Voici exactement ce qui va se passer.

Exemples :

- 2,84 Go seront supprimés ;
- les mots de passe seront conservés ;
- les favoris seront conservés ;
- les sessions de connexion seront conservées ;
- 0 fichier personnel sera supprimé ;
- la Corbeille n'est pas sélectionnée.

Le moteur doit produire ce résumé à partir du `CleaningPlan`.

---

## 3.4 Mode Simple / Expert

### Simple

- vocabulaire humain ;
- actions recommandées ;
- aucune manipulation dangereuse ;
- détails techniques repliés ;
- aucune suppression agressive.

### Expert

Expose :

- règles individuelles ;
- clés de registre ciblées ;
- chemins ;
- artefacts NTFS ;
- algorithmes d'effacement ;
- exclusions ;
- logs détaillés ;
- sécurité du stockage ;
- options avancées.

Le passage en Expert doit être volontaire.

---

# 4. DESIGN / UI

Utiliser WPF moderne avec une esthétique inspirée de Windows 11 Fluent, mais sans dépendre d'un framework UI lourd si cela complique le produit.

## Layout principal

Sidebar gauche :

- Accueil
- Nettoyage
- Confidentialité
- Navigateurs
- Espace disque
- Doublons
- Applications
- Automatisation
- Historique
- Paramètres

En bas :

- Soutenir

## Dashboard

Titre :

> Votre PC peut être nettoyé en toute sécurité

Carte principale :

- espace récupérable ;
- SAFE ;
- PRIVACY ;
- REVIEW ;
- Analyser mon PC ;
- Nettoyer maintenant.

Cartes secondaires :

- Windows temporaires ;
- Navigateurs ;
- Corbeille ;
- Traces Windows ;
- Espace disque ;
- dernier nettoyage ;
- total libéré.

## Contraintes design

- coins légèrement arrondis ;
- ombres très légères ;
- beaucoup d'espace ;
- hiérarchie visuelle claire ;
- aucune interface « utilitaire 2010 » ;
- dark mode ;
- light mode ;
- respect du scaling Windows ;
- DPI per-monitor ;
- clavier complet ;
- tooltips ;
- focus visible ;
- minimum 1280x720 ;
- UI agréable en 1920x1080 / 2560x1440 ;
- taille minimum de fenêtre raisonnable ;
- pas d'écran saturé d'options.

Si une maquette existe dans `docs/design/`, l'utiliser comme référence visuelle.

---

# 5. STACK TECHNIQUE

## Obligatoire

- .NET 10
- WPF
- C#
- MVVM
- `async/await`
- nullable reference types activés
- implicit usings
- warnings importants traités
- architecture testable
- dépendances injectées
- aucune logique de nettoyage dans le code-behind XAML

## Recommandé

- `CommunityToolkit.Mvvm`
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.Logging`
- `System.Text.Json`
- `Microsoft.Data.Sqlite` si utile pour historique/cache local

Utiliser des packages stables et activement maintenus.

Éviter les dépendances obscures pour les fonctions sensibles.

---

# 6. ARCHITECTURE DE SOLUTION

Créer au minimum :

```text
TraceZero.sln

src/
  TraceZero.App/
  TraceZero.Application/
  TraceZero.Domain/
  TraceZero.Engine/
  TraceZero.Windows/
  TraceZero.Browsers/
  TraceZero.Storage/
  TraceZero.Persistence/
  TraceZero.Updater/

tests/
  TraceZero.Domain.Tests/
  TraceZero.Engine.Tests/
  TraceZero.Windows.Tests/
  TraceZero.Browsers.Tests/
  TraceZero.IntegrationTests/
  TraceZero.SafetyTests/
  TraceZero.PerformanceTests/

tools/
  TraceZero.TestDataGenerator/
  TraceZero.RuleValidator/

build/
  packaging/
  scripts/

docs/
  architecture/
  design/
  safety/
  release/
```

---

# 7. MODÈLE DE DOMAINE

Créer des modèles clairs.

Exemples :

```csharp
ScanTarget
ScanRule
ScanResult
ScanItem
CleaningPlan
CleaningAction
CleaningResult
CleaningFailure
RiskLevel
Category
BrowserProfile
DriveInfoModel
StorageType
ExclusionRule
PrivacyTrace
CleanupHistoryEntry
AppInstallation
StartupEntry
DuplicateGroup
LargeFileEntry
SupporterLicense
UpdateManifest
```

## ScanItem

Doit au minimum pouvoir porter :

- identifiant stable ;
- règle source ;
- catégorie ;
- sous-catégorie ;
- nom affiché ;
- description ;
- chemin ou identifiant système ;
- taille ;
- nombre d'éléments ;
- risk level ;
- selected by default ;
- needs elevation ;
- browser/app concerné ;
- locked state ;
- last modified ;
- reversible / irreversible ;
- reason ;
- help/explanation key.

---

# 8. MOTEUR DE RÈGLES

Les règles ne doivent pas être dispersées dans l'UI.

Créer un moteur central.

Une règle doit pouvoir décrire :

- ID ;
- nom ;
- catégorie ;
- chemins ;
- variables d'environnement ;
- glob patterns ;
- fichiers ;
- dossiers ;
- âge minimum ;
- extensions ;
- exclusions ;
- risque ;
- droits admin ;
- méthode de suppression ;
- compatibilité Windows ;
- compatibilité application ;
- description utilisateur ;
- description expert ;
- validation.

Exemple conceptuel :

```yaml
id: chrome.gpu-cache
category: BrowserCache
browser: Chrome
risk: Safe
paths:
  - "%LOCALAPPDATA%\\Google\\Chrome\\User Data\\*\\GPUCache"
action: DeleteContents
preserveRoot: true
```

Le format final peut être JSON/YAML/code strongly typed, mais il doit rester :

- validable ;
- testable ;
- versionné ;
- extensible.

Créer un validateur de règles empêchant :

- chemin racine ;
- chemin vide ;
- suppression de `C:\` ;
- suppression de `%USERPROFILE%` entier ;
- wildcard non bornée ;
- traversal ;
- patterns risqués.

---

# 9. SÉCURITÉ DE SUPPRESSION — CRITIQUE

Cette section est prioritaire.

## Interdictions absolues

Le moteur ne doit jamais, par défaut :

- supprimer Documents ;
- supprimer Desktop ;
- supprimer Downloads ;
- supprimer Pictures ;
- supprimer Videos ;
- supprimer un profil utilisateur ;
- supprimer un profil navigateur complet ;
- supprimer des mots de passe ;
- supprimer des favoris ;
- supprimer une session connectée ;
- supprimer un dossier parent par erreur ;
- suivre aveuglément un junction / symlink / reparse point ;
- effacer des fichiers système non explicitement ciblés ;
- supprimer un fichier seulement parce que son extension paraît temporaire.

## Reparse points

Lors d'une énumération récursive :

- détecter les reparse points ;
- ne pas les suivre par défaut ;
- éviter toute sortie du root autorisé ;
- journaliser leur présence.

## Validation de chemin avant suppression

Chaque action destructive doit passer par une validation centrale :

`ISafePathValidator`.

Valider :

- canonical path ;
- root autorisé ;
- volume ;
- symlinks ;
- fichiers spéciaux ;
- règles interdites ;
- système ;
- profil utilisateur ;
- exclusions.

## Locked files

Ne pas forcer une suppression brutale.

Proposer :

- ignorer ;
- réessayer ;
- fermer l'application concernée ;
- planifier au redémarrage uniquement si méthode Windows documentée et justifiée.

---

# 10. PHASE 0 — BOOTSTRAP DU PROJET

## Objectifs

- créer la solution ;
- initialiser Git si nécessaire ;
- ajouter `.editorconfig` ;
- ajouter `Directory.Build.props` ;
- activer nullable ;
- installer les packages ;
- configurer DI ;
- créer le shell WPF ;
- thème clair/sombre ;
- navigation ;
- logs ;
- configuration locale ;
- infrastructure tests.

## Fichiers de pilotage

Créer :

### `PHASE_STATUS.md`

Tableau :

```text
Phase | Status | Tests | Notes
```

Status :

- NOT_STARTED
- IN_PROGRESS
- BLOCKED
- DONE

### `DECISIONS.md`

ADR simplifié :

- choix techniques ;
- changements importants ;
- compromis ;
- restrictions.

### `KNOWN_LIMITATIONS.md`

Ne jamais masquer une limite.

## Definition of Done

- build Debug OK ;
- build Release OK ;
- tests de base OK ;
- application lance ;
- navigation fonctionne ;
- pas de crash au démarrage ;
- aucun mock affiché comme vraie donnée.

---

# 11. PHASE 1 — UI SHELL + DESIGN SYSTEM

Construire le design complet.

## Composants

- sidebar ;
- topbar ;
- cards ;
- primary button ;
- secondary button ;
- status chip ;
- progress ;
- skeleton loader ;
- toast ;
- modal ;
- confirmation ;
- list rows ;
- expandable technical details ;
- empty state ;
- error state ;
- cancel state.

## Pages

Créer les pages définitives, même si les fonctions sont connectées progressivement :

- Dashboard ;
- Cleanup ;
- Privacy ;
- Browsers ;
- Disk Space ;
- Duplicates ;
- Applications ;
- Automation ;
- History ;
- Settings ;
- Supporter.

Les pages non encore fonctionnelles pendant la phase doivent être clairement indiquées comme « module en cours de construction » uniquement pendant le développement.

Avant release, aucune page vide ne doit rester.

## DoD

- UI cohérente ;
- responsive fenêtre ;
- dark/light ;
- navigation clavier ;
- aucune exception binding ;
- aucun texte coupé ;
- aucune valeur mockée en Release.

---

# 12. PHASE 2 — SCAN ENGINE

Créer le moteur asynchrone.

Interfaces suggérées :

```csharp
IScanEngine
IScanProvider
ICleaningEngine
ICleaningProvider
IProgressReporter
ICancellationService
ISafePathValidator
IRuleRepository
```

## Caractéristiques

- scan parallèle limité ;
- progress stream ;
- cancellation ;
- erreurs isolées ;
- un provider défaillant ne fait pas crasher tout le scan ;
- calcul réel des tailles ;
- déduplication des résultats ;
- temps écoulé ;
- nombre de fichiers ;
- résultat par catégorie.

## DoD

Tests avec arborescences temporaires synthétiques.

Tester :

- permissions refusées ;
- fichier supprimé pendant scan ;
- junction ;
- symlink ;
- fichier verrouillé ;
- très gros dossier ;
- nom Unicode ;
- long paths ;
- annulation.

---

# 13. PHASE 3 — NETTOYAGE WINDOWS STANDARD

Implémenter réellement les catégories sûres.

Inclure notamment, après validation :

- `%TEMP%` utilisateur ;
- fichiers temporaires Windows accessibles ;
- caches Windows clairement identifiés ;
- crash dumps ;
- mini dump ;
- thumbnail cache ;
- caches d'icônes lorsque sûr ;
- caches de shaders ;
- fichiers temporaires de certaines apps Microsoft ;
- Corbeille en option REVIEW ;
- anciens logs clairement jetables ;
- caches de rapports ;
- caches de téléchargement système lorsque leur suppression est documentée et sûre.

## Important

Ne pas promettre qu'un cache accélère Windows.

Le vocabulaire :

> espace récupérable

et non :

> erreurs critiques.

## DoD

- vrais scans ;
- vraies tailles ;
- suppression ;
- rescanner après nettoyage => taille proche de zéro pour cibles nettoyées ;
- aucune suppression hors root ;
- logs par action ;
- erreurs lisibles.

---

# 14. PHASE 4 — NAVIGATEURS

Support minimum complet :

- Chrome ;
- Edge ;
- Firefox ;
- Brave.

Puis ajouter :

- Opera ;
- Vivaldi ;
- Chromium ;
- éventuellement autres navigateurs populaires si détectables proprement.

## Détection

Détecter :

- installation ;
- profils ;
- profil par défaut ;
- profils multiples ;
- navigateur en cours d'exécution.

## Catégories

### SAFE

- disk cache ;
- GPU cache ;
- code cache ;
- media cache ;
- crash reports ;
- temporary browser data.

### PRIVACY

- history ;
- download history ;
- form history si approprié ;
- typed URLs ;
- thumbnails ;
- site data selon type.

### REVIEW

- cookies ;
- sessions ;
- local storage ;
- offline storage.

## Protection des connexions

Par défaut :

- NE PAS supprimer cookies/auth ;
- NE PAS supprimer password store ;
- NE PAS supprimer bookmarks ;
- NE PAS supprimer sessions.

Ajouter une option visible :

> **Conserver mes connexions**

activée par défaut.

## Cookies intelligents

Créer un gestionnaire :

- afficher domaine ;
- last access ;
- taille approximative ;
- permettre allowlist ;
- conserver cookies de domaines choisis ;
- preset « conserver mes connexions ».

Ne jamais modifier une base navigateur en cours d'utilisation sans stratégie sûre.

Si nécessaire :

- demander fermeture du navigateur ;
- effectuer backup temporaire ;
- transaction ;
- restaurer si échec.

## DoD

Tests sur profils synthétiques Chromium et Firefox.

---

# 15. PHASE 5 — PRIVACY INSPECTOR WINDOWS

C'est une fonctionnalité différenciante majeure.

Page :

> **Ce que Windows sait encore de votre activité**

Afficher des cartes explicatives.

## Traces à analyser

Lorsque possible de façon fiable :

- RecentDocs ;
- RunMRU ;
- TypedPaths ;
- OpenSavePidlMRU ;
- UserAssist ;
- Jump Lists ;
- Recent Items ;
- thumbnail history ;
- Windows Search related traces ;
- clipboard history si accessible/supporté ;
- DNS cache ;
- explorer histories ;
- Shellbags ;
- USB / device usage traces ;
- application execution traces ;
- selected event traces uniquement en mode Expert ;
- autres MRU documentés.

Pour chaque trace :

- expliquer ce qu'elle signifie ;
- pourquoi elle existe ;
- niveau de risque ;
- date ;
- source ;
- possibilité de nettoyage ;
- impact potentiel.

Exemple :

> Windows se souvient que ce dossier a été ouvert récemment.

Ne jamais afficher uniquement :

`HKCU\Software\Microsoft\Windows\Shell\BagMRU`

sans explication.

## DoD

- scan réel ;
- détails Expert ;
- action de nettoyage par catégorie ;
- re-scan ;
- tests de registre sous hive de test lorsque possible.

---

# 16. PHASE 6 — CLEANING PLAN + SAFETY LAYER

Avant toute suppression, le moteur doit produire un `CleaningPlan`.

Le plan contient :

- items ;
- action ;
- taille ;
- risque ;
- réversibilité ;
- privilèges ;
- application à fermer ;
- avertissement ;
- groupe.

## Preview

L'UI doit pouvoir dire :

- espace libéré ;
- catégories ;
- ce qui est protégé ;
- ce qui est irréversible.

## Exclusions

Supporter :

- fichier ;
- dossier ;
- wildcard borné ;
- domaine cookie ;
- application ;
- catégorie.

## Journal

Chaque nettoyage crée un historique contenant :

- date ;
- version TraceZero ;
- règle ;
- chemin anonymisable ;
- taille ;
- résultat ;
- erreur ;
- durée.

Ne jamais stocker des contenus de fichiers.

---

# 17. PHASE 7 — PROTECTION, BACKUP ET RESTAURATION

Avant actions sensibles :

- créer backup des données modifiées lorsque possible ;
- créer point de restauration Windows pour certains modules Expert ;
- backup ciblé de clés registre ;
- backup temporaire de DB navigateur si modification structurée ;
- journal d'opération.

Ajouter :

> **Protection du nettoyage**

et :

> **Restaurer les éléments disponibles**

Ne jamais prétendre pouvoir restaurer un fichier effacé de façon sécurisée.

Classer chaque action :

- Reversible
- PartiallyReversible
- Irreversible

---

# 18. PHASE 8 — ANALYSE AVANCÉE DE CONFIDENTIALITÉ NTFS

Objectif : approcher la profondeur de PrivaZer sans prendre de risque absurde.

## Mode Expert uniquement

Étudier et implémenter de manière documentée et testée :

- USN Journal : lecture / analyse des traces pertinentes ;
- NTFS metadata visibility ;
- MFT : détection de références résiduelles si techniquement possible sans corrompre le FS ;
- `$LogFile` / artefacts associés en lecture lorsque supportable ;
- résidus de noms de fichiers ;
- espace libre ;
- traces supprimées récupérables.

## Règle de sécurité

Ne jamais :

- supprimer arbitrairement le journal USN complet juste pour « nettoyer » ;
- modifier des structures NTFS brutes ;
- écrire directement dans MFT ;
- contourner le système de fichiers.

Utiliser des API Windows supportées ou des techniques de wipe de l'espace libre sûres.

Si une trace est visible mais ne peut pas être supprimée de façon fiable :

- afficher `Détectée` ;
- expliquer ;
- ne pas afficher `Nettoyable` ;
- ne pas simuler une suppression.

---

# 19. PHASE 9 — EFFACEMENT SÉCURISÉ

Créer un module distinct.

## Fichiers / dossiers

Drag & drop + picker.

Détecter le type de stockage.

### HDD

Proposer :

- suppression normale ;
- écrasement sécurisé simple ;
- mode renforcé Expert.

Ne pas multiplier artificiellement les passes pour faire croire à plus de sécurité.

### SSD / NVMe

Ne jamais présenter le multi-pass comme garanti.

Expliquer :

- wear leveling ;
- TRIM ;
- limitations du secure delete fichier par fichier.

Utiliser les mécanismes Windows / stockage pertinents.

## Effacement espace libre

Pour HDD :

- méthode contrôlée ;
- possibilité priorité basse ;
- annulation ;
- estimation ;
- ne jamais toucher aux fichiers actifs.

Pour SSD :

- stratégie adaptée ;
- pas de write amplification inutile ;
- TRIM lorsque pertinent ;
- avertissement honnête.

## DoD

Tests sur images disque / volumes de test lorsque possible.

Ne jamais expérimenter le wipe destructif sur le disque système du développeur pendant les tests automatisés.

---

# 20. PHASE 10 — DISK SPACE MANAGER

Créer une page réellement utile.

Afficher :

- capacité ;
- utilisé ;
- libre ;
- graphique simple ;
- catégories ;
- gros fichiers ;
- gros dossiers.

## Large files

Recherche configurable :

- > 100 Mo ;
- > 500 Mo ;
- > 1 Go ;
- custom.

Par défaut exclure / protéger :

- Windows ;
- Program Files ;
- ProgramData critiques ;
- fichiers système.

L'utilisateur peut explorer en Expert.

## Old Downloads

Possibilité d'afficher :

- téléchargements > 30/90/180 jours.

Mais jamais sélectionnés automatiquement.

## DoD

Scan rapide, cancellation, ouverture Explorer, sélection manuelle.

---

# 21. PHASE 11 — DUPLICATE FINDER

Fonction complète.

## Pipeline performant

1. group by size ;
2. fast partial hash ;
3. full cryptographic hash pour confirmation ;
4. comparaison finale.

Ne jamais conclure doublon sur :

- nom ;
- date ;
- taille seule.

## Types

- photos ;
- vidéos ;
- documents ;
- musique ;
- tous fichiers.

## UX

Afficher :

- groupes ;
- preview ;
- chemin ;
- date ;
- taille ;
- hash confirmé.

## Sécurité

Protéger :

- Windows ;
- Program Files ;
- ProgramData ;
- AppData système ;
- dossiers de composants.

Aucune suppression globale automatique.

Créer des stratégies d'aide :

- garder le plus récent ;
- garder le plus ancien ;
- garder dans dossier préféré.

Mais exiger validation utilisateur.

---

# 22. PHASE 12 — APPLICATIONS ET DÉMARRAGE

## Installed Apps

Lister :

- nom ;
- éditeur ;
- version ;
- date install ;
- taille si disponible ;
- origine.

Actions :

- ouvrir emplacement ;
- désinstaller via mécanisme déclaré de l'application ;
- ne pas supprimer un logiciel manuellement.

## Startup Manager

Analyser :

- Run ;
- RunOnce ;
- Startup folders ;
- tâches de démarrage pertinentes ;
- services uniquement en Expert.

Actions :

- activer ;
- désactiver ;
- restaurer.

Créer backup avant modification.

## Uninstall leftovers

Après désinstallation, proposer un scan prudent :

- dossiers résiduels clairement liés ;
- entrées spécifiques ;
- jamais de suppression par simple ressemblance de nom.

---

# 23. PHASE 13 — SOFTWARE UPDATER

Objectif : identifier des logiciels installés obsolètes via des sources fiables.

Priorité :

1. API / sources officielles Windows ;
2. Windows Package Manager / mécanismes Microsoft supportés ;
3. metadata éditeur fiable.

Ne pas scraper des sites de téléchargement douteux.

Afficher :

- version installée ;
- version disponible ;
- éditeur ;
- source ;
- changelog si disponible ;
- signature.

Les installations doivent être visibles et annulables lorsque possible.

---

# 24. PHASE 14 — DRIVER HEALTH / DRIVER UPDATER

CCleaner possède un Driver Updater, mais c'est une zone à très haut risque.

TraceZero doit privilégier la sécurité.

## Étape A — Driver Health

Toujours implémenter :

- inventaire ;
- version ;
- fournisseur ;
- date ;
- device ;
- statut ;
- problèmes Device Manager.

## Étape B — Updates

N'implémenter une mise à jour automatique que via une méthode officielle, documentée et fiable.

Avant installation :

- point de restauration ;
- export / backup du driver actuel lorsque possible ;
- vérifier signature ;
- vérifier compatibilité ;
- confirmation explicite ;
- rollback documenté.

Interdit :

- bases de drivers douteuses ;
- drivers téléchargés depuis des mirrors non officiels ;
- matching fuzzy agressif.

Si aucune méthode suffisamment sûre n'est disponible, garder Driver Health complet et rediriger vers Windows Update pour l'installation au lieu de faire semblant d'avoir la parité.

---

# 25. PHASE 15 — AUTOMATISATION

Créer des profils.

## Presets

### Sûr
Caches et temporaires uniquement.

### Confidentialité
Ajoute histories et traces autorisées.

### Personnalisé
Sélection utilisateur.

## Déclencheurs

- manuel ;
- chaque semaine ;
- chaque mois ;
- au démarrage ;
- avant arrêt si faisable proprement ;
- disque au-dessus de X %.

## Règles

- ne pas interrompre jeu plein écran ;
- ne pas nettoyer navigateur actif sans permission ;
- notification avant action sensible ;
- journal ;
- possibilité pause.

Pas de service lourd permanent si non nécessaire.

Utiliser Task Scheduler si plus approprié qu'un service.

---

# 26. PHASE 16 — HISTORIQUE ET STATISTIQUES

Dashboard local :

- total libéré ;
- nombre de nettoyages ;
- dernier scan ;
- historique ;
- évolution espace disque.

Aucune télémétrie cloud requise.

Historique local nettoyable.

Mode confidentialité :

> ne pas conserver le détail des chemins.

---

# 27. PHASE 17 — SUPPORTER / PAY WHAT YOU WANT

Le logiciel doit rester réellement utilisable gratuitement.

## Positionnement

Page :

> **Soutenir TraceZero**

Texte :

> TraceZero est développé sans publicité, sans vente de données et sans abonnement obligatoire.

Proposer :

- 10 €
- 19 € recommandé
- 29 €
- 49 €
- autre montant via le site

## Supporter

Peut débloquer par exemple :

- automatisations avancées ;
- profils multiples ;
- nettoyage silencieux ;
- fonctions de confort ;
- thèmes / personnalisation ;
- fonctionnalités avancées non essentielles.

Le nettoyage principal et la sécurité ne doivent pas être volontairement sabotés dans la version gratuite.

## Licence

Préférer :

- token signé ;
- validation cryptographique locale ;
- aucune obligation de compte ;
- mode offline.

Ne jamais stocker un secret serveur dans le client.

Créer `ILicenseService`.

Prévoir une source externe de paiement mais ne pas coupler le cœur du logiciel à un fournisseur particulier.

---

# 28. PHASE 18 — UPDATER

Créer un updater fiable.

## Manifest

Le serveur publie un manifest signé :

```json
{
  "version": "1.2.3",
  "channel": "stable",
  "url": "...",
  "sha256": "...",
  "signature": "...",
  "minimumSupportedVersion": "..."
}
```

## Sécurité

Avant exécution :

- HTTPS ;
- SHA-256 ;
- signature cryptographique du manifest ;
- Authenticode du binaire ;
- vérifier éditeur attendu.

Ne jamais exécuter un update dont la validation échoue.

## Channels

- stable ;
- beta volontaire.

## Store

La build Microsoft Store ne doit pas contourner le système de mise à jour Store.

---

# 29. PHASE 19 — INSTALLATEUR / PORTABLE / DISTRIBUTION

Produire :

## Direct

- self-contained x64 ;
- `TraceZeroSetup.exe` ou MSI/EXE propre ;
- installé dans Program Files ;
- données utilisateur dans AppData ;
- uninstall propre.

## Microsoft Store

Préparer MSIX.

## Portable

Créer une build Portable :

- pas d'installation ;
- config à côté de l'exe ;
- aucune écriture cachée inutile ;
- updater configurable.

## Signature

Prévoir pipeline de signature.

Tous les exécutables sensibles doivent être signés en production :

- app ;
- updater ;
- installer ;
- helper elevated.

Timestamp obligatoire.

---

# 30. PHASE 20 — ÉLÉVATION DE PRIVILÈGES

L'application ne doit pas démarrer admin par défaut.

Créer un helper séparé :

`TraceZero.Elevated.exe`

avec surface minimale.

Communication inter-process contrôlée.

Le helper :

- n'accepte que des commandes structurées ;
- revalide les chemins ;
- applique sa propre safety validation ;
- refuse des chemins arbitraires ;
- logue ;
- s'arrête après action.

Ne jamais faire confiance au client UI.

---

# 31. PHASE 21 — LOCALISATION

Ressources :

- `fr-FR` défaut ;
- `en-US` ;
- `de-DE` ;
- `es-ES`.

Aucun texte UI hardcodé.

Les descriptions de règles doivent être localisées.

Les erreurs doivent être traduisibles.

---

# 32. PHASE 22 — ACCESSIBILITÉ

- navigation clavier ;
- lecteurs d'écran ;
- AutomationProperties ;
- contraste ;
- focus ;
- zoom/scaling ;
- aucun statut transmis uniquement par couleur ;
- labels explicites.

---

# 33. PHASE 23 — PERFORMANCE

Objectifs réalistes :

- aucun blocage UI ;
- progression immédiate ;
- scan annulable ;
- nombre de threads limité ;
- IO concurrency contrôlée ;
- mémoire raisonnable ;
- pas de chargement de millions de résultats UI d'un coup ;
- pagination / virtualisation ;
- enumeration streaming ;
- hashing duplicate finder optimisé.

Créer benchmarks pour :

- 100k fichiers ;
- 1M fichiers synthétiques si possible ;
- gros fichiers ;
- HDD-like slow storage simulé ;
- annulation.

---

# 34. PHASE 24 — TESTS DE SÉCURITÉ

Créer une vraie suite `TraceZero.SafetyTests`.

Tester explicitement :

- `C:\`
- `%USERPROFILE%`
- Documents
- Desktop
- Downloads
- Program Files
- Windows
- junction vers dossier sensible
- symlink
- UNC path
- long paths
- path traversal
- variable d'environnement invalide
- path vide
- wildcard `*`
- drive root
- volume inaccessible
- permission denied
- race condition
- fichier remplacé entre scan et clean.

Le test doit prouver que le moteur **refuse** une suppression hors règles.

---

# 35. PHASE 25 — TESTS DE NON-RÉGRESSION PAR « GOLDEN DATASET »

Créer dans l'outil de test un faux environnement :

```text
TestProfile/
  AppData/
  Chrome/
  Firefox/
  Temp/
  Documents/
  Downloads/
  Protected/
```

Les fichiers dangereux doivent être présents.

Après nettoyage automatique :

- les caches prévus sont absents ;
- les documents sont toujours présents ;
- les sessions protégées sont toujours présentes ;
- les bookmarks sont présents ;
- les exclusions sont présentes.

---

# 36. PHASE 26 — TESTS RÉELS EN VM

Créer `docs/testing/VM_TEST_MATRIX.md`.

Tester au minimum :

- Windows 10 x64 ;
- Windows 11 x64 ;
- utilisateur standard ;
- admin ;
- Chrome ouvert ;
- Edge ouvert ;
- Firefox ouvert ;
- plusieurs profils ;
- SSD ;
- HDD si possible ;
- portable ;
- installé ;
- offline ;
- proxy / réseau indisponible ;
- faible espace disque.

Ne pas considérer la release prête avant validation VM.

---

# 37. PHASE 27 — QUALITÉ RELEASE

Avant chaque release :

```text
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

Puis :

- tests sécurité ;
- tests intégration ;
- packaging ;
- signature ;
- vérification signature ;
- scan antivirus ;
- hash SHA-256 ;
- smoke test install ;
- smoke test uninstall ;
- test updater ;
- test clean réel en VM.

Créer script automatisé dans :

`build/scripts/release.ps1`

---

# 38. JOURNALISATION

Les logs doivent être utiles sans devenir de la télémétrie.

Local uniquement par défaut.

Ne pas logger :

- contenu de documents ;
- cookies ;
- mots de passe ;
- tokens ;
- URLs sensibles complètes si évitable.

Ajouter option :

> Masquer les chemins personnels dans les journaux.

Rotation des logs.

---

# 39. PRIVACY BY DESIGN

Par défaut :

- zéro télémétrie ;
- zéro tracking ;
- zéro publicité ;
- aucun identifiant publicitaire ;
- aucun profil utilisateur ;
- aucun compte obligatoire ;
- aucun upload de fichiers ;
- aucun upload des résultats de scan.

Connexion réseau autorisée seulement pour :

- update ;
- ouverture volontaire de page Supporter ;
- éventuellement vérification licence si l'utilisateur choisit un mode online.

Toute connexion doit être documentée.

Créer :

`docs/privacy/NETWORK_BEHAVIOR.md`

---

# 40. PARAMÈTRES

Inclure :

## Général

- langue ;
- thème ;
- lancement Windows ;
- notifications.

## Nettoyage

- mode Simple / Expert ;
- profil ;
- exclusions ;
- protection Corbeille ;
- âge minimum fichiers.

## Navigateurs

- connexions conservées ;
- domaines protégés ;
- fermeture automatique demandée.

## Confidentialité

- catégories ;
- traces sensibles ;
- historique local.

## Updates

- auto ;
- manual ;
- stable/beta.

## Supporter

- état licence ;
- gérer licence.

---

# 41. MESSAGES D'ERREUR

Jamais :

> Error 0x80070005

seul.

Afficher :

> TraceZero n'a pas l'autorisation de supprimer 3 fichiers. Aucun autre élément n'a été affecté.

Puis détail Expert :

> HRESULT / Win32 error.

---

# 42. NE PAS FAIRE

Interdictions produit :

- faux compteur d'erreurs ;
- score santé inventé ;
- « votre PC est en danger » sans preuve ;
- RAM Booster ;
- registry booster ;
- promesses « 300 % plus rapide » ;
- installation de bundle ;
- publicité ;
- antivirus scareware ;
- suppression agressive ;
- driver non signé ;
- update non signé ;
- contournement SmartScreen ;
- collecte cachée ;
- abonnement imposé pour nettoyer.

---

# 43. REGISTRY

Ne pas créer un « Registry Cleaner » générique présenté comme un accélérateur.

Le registre est utilisé uniquement pour :

- traces de confidentialité précises ;
- entrées de démarrage ;
- configuration app ;
- restes clairement attribuables ;
- backups/restores ciblés.

Toute suppression doit être justifiée par une règle.

---

# 44. PHILOSOPHIE « AUSSI BON QUE CCLEANER ET PRIVAZER »

Ne pas interpréter cela comme :

> copier chaque bouton.

Interpréter comme :

- même niveau de confiance ;
- même profondeur utile ;
- davantage de sécurité ;
- davantage de clarté ;
- capacité à traiter les mêmes problèmes principaux ;
- meilleures protections ;
- UX moderne ;
- aucune fausse promesse.

L'objectif release doit permettre à un utilisateur de remplacer raisonnablement CCleaner/PrivaZer pour :

- nettoyer Windows ;
- nettoyer navigateurs ;
- gérer les traces de confidentialité ;
- récupérer de l'espace ;
- gérer les gros fichiers ;
- détecter doublons ;
- gérer démarrage / désinstallation ;
- automatiser ;
- effacer de façon adaptée ;
- inspecter les traces avancées.

---

# 45. PRIORITÉ DES MODULES

Ordre de construction :

```text
1. Architecture
2. UI Shell
3. Safety layer
4. Scan engine
5. Windows cleaner
6. Browser cleaner
7. Privacy inspector
8. Cleaning preview
9. History / exclusions
10. Deep privacy
11. Secure erase
12. Disk manager
13. Duplicates
14. Apps/startup
15. Software updater
16. Driver health
17. Automation
18. Supporter
19. Updater
20. Packaging
21. Localization
22. VM validation
23. Release
```

Ne saute pas directement à une fonction sexy en laissant le moteur de sécurité incomplet.

---

# 46. MÉTHODE DE TRAVAIL CLAUDE CODE

À chaque session :

1. lire ce fichier ;
2. lire `PHASE_STATUS.md` ;
3. lire `DECISIONS.md` ;
4. détecter la première phase non terminée ;
5. travailler dessus ;
6. exécuter les tests ;
7. corriger avant de poursuivre ;
8. mettre `PHASE_STATUS.md` à jour ;
9. continuer vers la phase suivante sans attendre une confirmation si le contexte le permet.

## Ne pas demander l'autorisation pour

- créer les dossiers prévus ;
- écrire les tests ;
- corriger un bug ;
- refactorer pour respecter l'architecture ;
- ajouter une dépendance Microsoft stable nécessaire ;
- ajouter de la documentation.

## Demander seulement si

- une clé secrète est réellement nécessaire ;
- un certificat de production est requis ;
- une ressource externe payante est indispensable ;
- une décision commerciale non spécifiée bloque réellement.

Sinon prendre la meilleure décision technique et la noter dans `DECISIONS.md`.

---

# 47. PAS DE DETTE « TEMPORAIRE » QUI DEVIENT PERMANENTE

Si une phase utilise temporairement un stub, il doit être supprimé avant `DONE`.

Rechercher avant chaque DoD :

```text
TODO
FIXME
HACK
NotImplementedException
throw new NotSupportedException
mock
dummy
placeholder
fake
```

Un mot peut être légitime dans les tests, mais vérifier.

---

# 48. DEFINITION OF DONE GLOBALE

TraceZero 1.0 n'est prêt que si :

## Fonctionnel

- scan Windows réel ;
- clean Windows réel ;
- Chrome réel ;
- Edge réel ;
- Firefox réel ;
- Brave réel ;
- protection sessions ;
- privacy scan ;
- privacy clean ;
- disk manager ;
- duplicates ;
- app manager ;
- startup manager ;
- automation ;
- secure erase ;
- history ;
- exclusions ;
- settings ;
- Supporter ;
- updater ;
- installer ;
- uninstall.

## Sécurité

- path safety tests ;
- reparse protection ;
- preview ;
- risk levels ;
- backup ;
- no personal deletion default ;
- no unsigned update ;
- elevated helper locked down.

## Qualité

- Release build ;
- tests pass ;
- VM tests ;
- UI finalisée ;
- dark/light ;
- FR + EN au minimum finalisés, DE/ES prêts ou inclus selon progression ;
- logs ;
- docs ;
- versioning ;
- changelog.

## Distribution

- self-contained ;
- installateur ;
- SHA-256 ;
- pipeline de signature prêt ;
- MSIX prêt ;
- updater vérifié ;
- portable prêt ou planifié dans la release si prévu.

---

# 49. CHECKLIST RELEASE 1.0

Avant d'écrire `READY_FOR_RELEASE = true` :

- [ ] Dashboard sans données fake
- [ ] Scan complet fonctionnel
- [ ] Nettoyage recommandé fonctionnel
- [ ] Mode Expert fonctionnel
- [ ] Windows cleaner fonctionnel
- [ ] Chrome
- [ ] Edge
- [ ] Firefox
- [ ] Brave
- [ ] Privacy Inspector
- [ ] Traces Windows expliquées
- [ ] Cookie allowlist
- [ ] Connexions conservées par défaut
- [ ] Exclusions
- [ ] CleaningPlan
- [ ] Preview
- [ ] Historique
- [ ] Restore lorsque possible
- [ ] Secure delete
- [ ] Free-space strategy
- [ ] HDD/SSD distinction
- [ ] Disk analyzer
- [ ] Large files
- [ ] Duplicate finder
- [ ] Applications
- [ ] Startup manager
- [ ] Software updater
- [ ] Driver Health
- [ ] Automation
- [ ] Supporter
- [ ] License service
- [ ] Update service
- [ ] Signed manifest support
- [ ] Elevated helper
- [ ] Settings
- [ ] Dark mode
- [ ] Light mode
- [ ] FR
- [ ] EN
- [ ] Installer
- [ ] Uninstaller
- [ ] Portable
- [ ] MSIX
- [ ] Release script
- [ ] Safety tests
- [ ] Integration tests
- [ ] VM tests
- [ ] No TODO in production code
- [ ] No mock values
- [ ] No non-functional buttons
- [ ] Privacy documentation
- [ ] Network behavior documented

---

# 50. PREMIÈRE ACTION À EXÉCUTER MAINTENANT

Commence immédiatement par :

1. inspecter le repository existant ;
2. ne supprimer aucun travail existant utile ;
3. créer ou corriger la structure de solution ;
4. créer `PHASE_STATUS.md` ;
5. créer `DECISIONS.md` ;
6. créer `KNOWN_LIMITATIONS.md` ;
7. établir les projets ;
8. mettre en place les tests de sécurité avant les premières suppressions ;
9. construire le Shell WPF ;
10. lancer `dotnet build` et `dotnet test`.

Ensuite poursuis les phases dans l'ordre.

**Ne t'arrête pas après l'interface.**
**Ne t'arrête pas après le premier scan.**
**Ne déclare aucune fonctionnalité terminée sans test réel.**

Le but n'est pas de produire quelque chose qui ressemble à TraceZero.

Le but est de produire **TraceZero**.
