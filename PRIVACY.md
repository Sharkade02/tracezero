# Politique de confidentialité — TraceZero

_Dernière mise à jour : 2026-08-17._

TraceZero est conçu **local-first** et **privacy-first**. En clair :

## Ce que TraceZero NE fait PAS

- **Aucune collecte de données personnelles.** TraceZero n'envoie ni vos fichiers, ni vos chemins, ni
  votre historique, ni aucune donnée d'usage vers un serveur.
- **Aucune télémétrie, aucun traçage, aucune publicité, aucun réseau publicitaire.**
- **Aucun compte requis.** L'application fonctionne entièrement hors ligne pour ses fonctions de
  nettoyage, de confidentialité, d'espace disque et de maintenance.
- L'**historique de nettoyage** est stocké **localement** (base SQLite sur votre PC) et
  **n'enregistre jamais de chemin personnel** — uniquement des totaux (octets libérés, nombre d'éléments,
  date). Il ne quitte jamais votre machine.

## La seule connexion réseau : la vérification de mise à jour (optionnelle)

Si la vérification de mise à jour est **activée** (elle est désactivée tant qu'aucune URL de manifeste
n'est configurée), TraceZero télécharge un petit fichier **`manifest.json` signé** en HTTPS pour savoir
si une nouvelle version existe. Comme toute requête HTTPS, le serveur qui héberge ce fichier peut voir
votre **adresse IP** et l'heure de la requête (donnée technique inhérente à toute connexion Internet).
TraceZero **n'envoie aucune autre information** lors de cette vérification, et n'exécute **jamais** une
mise à jour dont la signature n'est pas valide.

Vous pouvez laisser cette vérification désactivée : TraceZero reste pleinement fonctionnel hors ligne.

## Dons (facultatif)

Si vous choisissez de soutenir le projet, le **paiement est traité par une plateforme tierce**
(par ex. PayPal). Les données de paiement sont gérées par cette plateforme selon **sa** politique de
confidentialité ; TraceZero ne reçoit ni ne stocke vos informations bancaires.

## Vos données restent les vôtres

Tout ce que TraceZero analyse ou supprime reste sur votre ordinateur, sous votre contrôle. Aucune donnée
n'est mise en cache dans le cloud, indexée, ou partagée.

## Contact

Pour toute question : ouvrez une issue sur le dépôt du projet, ou écrivez à l'adresse de contact indiquée
sur la page du projet.
