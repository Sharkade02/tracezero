# VM_TEST_MATRIX — Tests réels en VM (Phase 26, §36)

> **Règle (§36) :** ne pas considérer une release prête avant validation VM.
> Ce document est la **matrice de test**. Son exécution requiert des machines virtuelles réelles
> (assets externes) ; les cases `Résultat` sont à remplir lors de chaque campagne de release.

## Prérequis

- Build à tester : produite par `build/scripts/release.ps1` (portable via `build/scripts/publish-portable.ps1`).
- Snapshots VM propres, restaurés avant chaque scénario.
- **Jamais** de wipe destructif / effacement sécurisé sur le disque de l'hôte de développement (§19) —
  uniquement dans des VM jetables.

## 1. Environnements (matrice OS × compte × distribution)

| # | OS | Compte | Distribution | Statut VM | Résultat |
|--:|----|--------|--------------|-----------|----------|
| E1 | Windows 10 x64 | Standard | Installé | ☐ | |
| E2 | Windows 10 x64 | Administrateur | Installé | ☐ | |
| E3 | Windows 10 x64 | Standard | Portable | ☐ | |
| E4 | Windows 11 x64 | Standard | Installé | ☐ | |
| E5 | Windows 11 x64 | Administrateur | Installé | ☐ | |
| E6 | Windows 11 x64 | Standard | Portable | ☐ | |

## 2. Stockage

| # | Type | Vérification | Résultat |
|--:|------|--------------|----------|
| S1 | SSD/NVMe | Effacement sécurisé affiche l'**avertissement SSD honnête** (non garanti) | |
| S2 | HDD (si dispo) | Effacement d'espace libre effectif ; média détecté « HDD » | |
| S3 | Faible espace disque | Scan/nettoyage/wipe ne plantent pas ; messages clairs ; wipe s'arrête proprement à disque plein | |

## 3. Navigateurs (ouverts pendant le test)

| # | Navigateur | Profils | Vérification | Résultat |
|--:|-----------|---------|--------------|----------|
| B1 | Chrome ouvert | 1 | Caches nettoyés ; **connexions/cookies/mots de passe/favoris préservés** ; fichiers verrouillés ignorés | |
| B2 | Edge ouvert | 1 | Idem B1 | |
| B3 | Firefox ouvert | 1 | Idem B1 | |
| B4 | Chrome + Edge + Firefox | plusieurs profils | Détection de tous les profils ; état « en cours d'exécution » correct | |

## 4. Réseau

| # | Condition | Vérification | Résultat |
|--:|-----------|--------------|----------|
| N1 | Hors ligne | L'app fonctionne (local-first) ; « Mises à jour » signale winget indispo si applicable, sans planter | |
| N2 | Proxy / réseau indisponible | Aucun blocage UI ; aucune fonctionnalité locale dégradée ; timeouts propres | |

## 5. Fonctions critiques (à repasser sur E1, E4 au minimum)

| # | Fonction | Vérification | Résultat |
|--:|----------|--------------|----------|
| F1 | Analyse + Nettoyage | Tailles réelles ; suppression validée ; historique enregistré (sans chemin personnel) | |
| F2 | Confidentialité | Traces expliquées ; sauvegarde registre avant effacement ; **restauration** fonctionne | |
| F3 | Effacement sécurisé | Fichier écrasé+supprimé ; garde-fou refuse système/dossier/racine ; wipe annulable | |
| F4 | Élévation (C:\Windows\Temp) | Invite UAC ; helper agit puis se ferme ; **app jamais admin** ; refus UAC géré sans crash | |
| F5 | Automatisation | Tâche planifiée créée/supprimée ; `--autoclean` s'exécute sans fenêtre | |
| F6 | Santé système / Pilotes | Données WMI réelles ; « Ouvrir Windows Update » fonctionne | |
| F7 | Mises à jour (winget) | Détection réelle ; lancement winget visible ; message honnête si winget absent | |
| F8 | Langue | Bascule fr/en/de/es à chaud ; persistance après redémarrage | |

## 6. Golden safety (doit toujours passer — cf. Phases 24/25)

- Aucune suppression hors des règles (documents/bureau/téléchargements intacts après nettoyage auto).
- Sessions/favoris/mots de passe navigateurs intacts.
- Exclusions respectées.
- Aucune valeur affichée simulée ; les chiffres n'apparaissent qu'après analyse réelle.

## 7. Portable vs installé

| # | Vérification | Résultat |
|--:|--------------|----------|
| P1 | Portable : données dans `<exe>\Data`, **aucune écriture** ailleurs (vérifier %LOCALAPPDATA%) | |
| P2 | Installé : données dans `%LOCALAPPDATA%\TraceZero` ; désinstallation propre | |

## Sortie de campagne

Une release n'est **prête** que si toutes les cases `Résultat` critiques (sections 5, 6, 7) sont ✅ sur
Windows 10 **et** Windows 11, en compte standard **et** administrateur. Consigner la version testée, la date
et l'opérateur en tête de chaque campagne.
