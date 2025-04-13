# Detective Conan Episode Renamer

Un outil en ligne de commande pour renommer automatiquement les fichiers d'épisodes de Détective Conan en utilisant les informations de Wikipédia.

## Fonctionnalités

- 🎯 Renommage automatique des fichiers d'épisodes
- 🔍 Détection intelligente des numéros d'épisodes via des patterns regex configurables
- 📝 Gestion des patterns regex via une interface utilisateur intuitive
- 💾 Sauvegarde automatique des fichiers avant renommage
- 📊 Récupération des informations d'épisodes depuis Wikipédia
- 📋 Interface utilisateur en ligne de commande avec Spectre.Console
- 📝 Journalisation détaillée des opérations

## Prérequis

- .NET 8.0
- Windows

## Installation

1. Clonez le dépôt :
```bash
git clone https://github.com/votre-username/Detective-Conan-Episode-Renamer.git
cd Detective-Conan-Episode-Renamer
```

2. Compilez le projet :
```bash
dotnet build
```

3. Exécutez l'application :
```bash
dotnet run
```

## Structure du Projet

- `Program.cs` : Point d'entrée de l'application
- `Services/` : Services principaux de l'application
  - `MenuService.cs` : Gestion de l'interface utilisateur
  - `FileRenamerService.cs` : Service de renommage des fichiers
  - `RegexPatternService.cs` : Gestion des patterns regex
  - `WikiScraperService.cs` : Récupération des données depuis Wikipédia
  - `BackupService.cs` : Gestion des sauvegardes
  - `LoggingService.cs` : Journalisation
- `Models/` : Classes de modèle
  - `AppSettings.cs` : Constantes de l'application
- `Interfaces/` : Interfaces des services
- `Utils/` : Utilitaires

## Fichiers et Dossiers

L'application utilise les dossiers et fichiers suivants :
- `Data/` : Dossier principal pour les données
  - `episodes.yaml` : Informations sur les épisodes
  - `regex-patterns.yaml` : Patterns regex pour la détection des numéros d'épisodes
- `Backups/` : Dossier pour les sauvegardes des fichiers
- `logs/` : Dossier pour les fichiers de log

## Utilisation

1. Lancez l'application
2. Utilisez le menu principal pour :
   - Gérer les patterns regex
   - Renommer des fichiers
   - Consulter les logs
   - Accéder aux paramètres

## Patterns Regex

Les patterns regex sont stockés au format YAML dans le dossier `Data/`. Chaque pattern contient :
- Un nom descriptif
- L'expression régulière
- Le groupe de capture pour le numéro d'épisode
- Un indicateur pour définir le pattern par défaut

## Logs

Les logs sont stockés dans le dossier `logs/` et incluent :
- Les opérations de renommage
- Les erreurs et avertissements
- Les modifications de configuration

## Contribution

Les contributions sont les bienvenues ! N'hésitez pas à :
1. Fork le projet
2. Créer une branche pour votre fonctionnalité
3. Commiter vos changements
4. Pousser vers la branche
5. Ouvrir une Pull Request

## Licence

Ce projet est sous licence MIT. Voir le fichier `LICENSE` pour plus de détails. 