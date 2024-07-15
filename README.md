# Detective Conan Episode Renamer

Cette application console permet de renommer tous les épisodes de Détective Conan d'un dossier en utilisant un fichier texte (`DetectiveConanTitre.txt`) basé sur la liste des épisodes disponible sur [Wikipedia](https://fr.wikipedia.org/wiki/Liste_des_%C3%A9pisodes_de_D%C3%A9tective_Conan). Le nouveau nom inclura le titre de l'épisode.

## Exemple

`Détective Conan Épisode 0001.mkv` devient `Détective Conan Épisode 0001 - Le Plus Grand Détective du siècle.mkv`

## Fonctionnalités

- **Renommer les épisodes** : Utilisez un fichier JSON contenant les titres des épisodes pour renommer les fichiers d'épisodes dans un dossier spécifié.
- **Menu de développement** :
  - Créer un répertoire de test et des fichiers d'épisode fictifs.
  - Renommer les fichiers de test.
  - Supprimer le répertoire de test.
  - Ouvrir le répertoire de test.
  - Convertir les épisodes en JSON à partir du fichier texte.

## TODO

- [x] Convertir le fichier `DetectiveConanTitre.txt` en fichier JSON pour une meilleure maniabilité.

## Prérequis

- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
