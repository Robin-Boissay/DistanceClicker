# DistanceClicker

## Table des matieres

1. [Description du projet](#description-du-projet)
2. [Fonctionnalites principales](#fonctionnalites-principales)
3. [Architecture technique](#architecture-technique)
4. [Technologies utilisees](#technologies-utilisees)
5. [Prerequis](#prerequis)
6. [Installation](#installation)
7. [Configuration ML-Agents](#configuration-ml-agents)
8. [Structure du projet](#structure-du-projet)
9. [Equipe](#equipe)
10. [Licence](#licence)

---

## Description du projet

DistanceClicker est un jeu mobile de type **idle/clicker** developpe avec Unity, integrant un systeme d'apprentissage par renforcement (Reinforcement Learning) via **Unity ML-Agents**. Le joueur accumule de la distance en cliquant sur des cibles et des cercles bonus, ameliore ses statistiques via un systeme de boutique, et peut rivaliser contre des agents IA entraines.

Le projet combine gameplay traditionnel de clicker avec une dimension competitive ou des agents ML jouent en parallele, permettant de mesurer les performances du joueur face a des strategies apprises automatiquement.

---

## Fonctionnalites principales

### Gameplay Core

- **Systeme de clic** : Cliquez sur la cible principale pour accumuler de la distance
- **Cercles bonus** : Cercles temporaires offrant des recompenses variables selon leur duree d'apparition
- **Progression par niveaux** : Systeme d'experience et de niveaux pour le joueur
- **DPC et DPS** : Distance Par Clic et Distance Par Seconde ameliorables

### Systeme economique

- **Boutique d'ameliorations** : Multiples categories d'upgrades pour ameliorer les statistiques
- **Systeme de monnaie** : Accumulation et gestion de ressources
- **Multiplicateurs** : Differents bonus et multiplicateurs de recompenses

### Fonctionnalites sociales

- **Authentification Firebase** : Connexion et sauvegarde des donnees utilisateur
- **Classement (Leaderboard)** : Comparaison avec d'autres joueurs
- **Profil utilisateur** : Personnalisation et statistiques detaillees

### Intelligence Artificielle

- **Agent ML** : Agent autonome base sur Unity ML-Agents (algorithme PPO)
- **Multi-environnement** : Support de plusieurs agents entraines en parallele
- **Competition ML vs Joueur** : Mode competitif entre le joueur et les bots IA
- **Systeme de recompenses** : Design de recompenses optimise pour l'apprentissage

---

## Architecture technique

### Managers principaux

| Manager | Role |
|---------|------|
| `GameManager` | Orchestration globale et pattern Singleton |
| `StatsManager` | Gestion des statistiques du joueur (DPC, DPS, multiplicateurs) |
| `DistanceManager` | Suivi de la progression et des cibles |
| `ShopManager` | Gestion des achats et ameliorations |
| `SaveManager` | Persistance des donnees (locale et Firebase) |
| `UIManager` | Interface utilisateur et affichage |
| `FirebaseManager` | Integration avec Firebase (Auth + Firestore) |
| `LeaderboardManager` | Classements et comparaisons |
| `IdleManager` | Gestion des gains hors-ligne |

### Architecture ML-Agents

| Composant | Role |
|-----------|------|
| `DistanceClickerAgent` | Agent ML principal pour jeu standard |
| `DistanceClickerAgentMultiEnv` | Agent adapte aux environnements multiples |
| `GameEnvironment` | Encapsulation d'un environnement isole |
| `EnvironmentFactory` | Creation dynamique d'environnements ML |
| `CompetitionManager` | Gestion des competitions joueur vs bots |
| `LocalStatsManager` / `LocalDistanceManager` | Managers isoles par environnement |

### Modele de donnees

- `PlayerData` : Donnees du joueur (experience, niveau, monnaie)
- `DistanceObjectSO` : ScriptableObject definissant les cibles
- `UpgradeSO` : ScriptableObject definissant les ameliorations

---

## Technologies utilisees

| Technologie | Version | Usage |
|-------------|---------|-------|
| Unity | 2022.x+ | Moteur de jeu |
| C# | .NET Standard 2.1 | Langage principal |
| Unity ML-Agents | 2.0+ | Apprentissage par renforcement |
| Firebase Auth | - | Authentification utilisateurs |
| Firebase Firestore | - | Base de donnees cloud |
| BreakInfinity | - | Gestion des grands nombres |
| TextMesh Pro | - | Interface utilisateur |

---

## Prerequis

- **Unity** 2022.3 LTS ou superieur
- **Python** 3.8+ (pour l'entrainement ML-Agents)
- **mlagents** Python package (`pip install mlagents`)
- Compte Firebase avec projet configure

---

## Installation

### 1. Cloner le projet

```bash
git clone https://github.com/[votre-repo]/DistanceClicker.git
cd DistanceClicker
```

### 2. Ouvrir avec Unity

1. Lancez Unity Hub
2. Ajoutez le projet depuis le dossier clone
3. Le projet s'ouvrira en **Safe Mode** (erreurs attendues)

### 3. Installer Firebase

1. Telechargez le SDK Firebase Unity : https://firebase.google.com/download/unity?hl=fr
2. Dezippez le dossier telecharge
3. Dans Unity (Safe Mode) :
   - Double-cliquez sur `FirebaseAuth.unitypackage` pour l'importer
   - Double-cliquez sur `FirebaseFirestore.unitypackage` pour l'importer
4. Les erreurs devraient disparaitre automatiquement

### 4. Verifier la scene

1. Dans le Project, naviguez vers `Assets/Scenes`
2. Double-cliquez sur `SampleScene.unity`
3. Le projet devrait maintenant fonctionner correctement

---

## Configuration ML-Agents

### Fichier de configuration

Le fichier de configuration d'entrainement se trouve dans `Assets/ML-Agents/ml-agents-config.yaml` :

```yaml
behaviors:
  DistanceClickerAgent:
    trainer_type: ppo
    hyperparameters:
      batch_size: 128
      buffer_size: 2048
      learning_rate: 3.0e-4
      beta: 5.0e-3
      epsilon: 0.2
      lambd: 0.95
      num_epoch: 3
      learning_rate_schedule: linear
    network_settings:
      normalize: false
      hidden_units: 256
      num_layers: 2
      vis_encode_type: simple
    reward_signals:
      extrinsic:
        gamma: 0.99
        strength: 1.0
    max_steps: 5000000
    time_horizon: 64
    summary_freq: 10000
    keep_checkpoints: 5
    checkpoint_interval: 50000
    threaded: false
```

### Lancer l'entrainement

```bash
# Depuis la racine du projet
mlagents-learn Assets/ML-Agents/ml-agents-config.yaml --run-id=DistanceClicker_Run1
```

### Visualiser avec TensorBoard

```bash
tensorboard --logdir ./results/DistanceClicker_Run1/
```

Les resultats d'entrainement sont stockes dans le dossier `results/`.

---

## Structure du projet

```
DistanceClicker/
├── Assets/
│   ├── Animation/           # Animations du jeu
│   ├── DistanceObject/      # Assets des cibles
│   ├── Firebase/            # SDK Firebase
│   ├── ML-Agents/           # Configuration ML
│   │   ├── Model/           # Modeles entraines (.onnx)
│   │   ├── Timers/          # Gestion des timers ML
│   │   └── ml-agents-config.yaml
│   ├── Prefabs/             # Prefabs du jeu
│   ├── Scenes/              # Scenes Unity
│   │   └── SampleScene.unity
│   ├── Scripts/
│   │   ├── Environment/     # Multi-environnement ML
│   │   ├── Libs/            # Bibliotheques utilitaires
│   │   ├── Manager/         # Managers du jeu
│   │   ├── ML/              # Agents ML
│   │   ├── Model/           # Modeles de donnees
│   │   ├── Shop/            # Systeme de boutique
│   │   └── UI/              # Interface utilisateur
│   ├── Settings/            # Parametres Unity
│   ├── ShopUpgrade/         # Assets des upgrades
│   ├── Sprite/              # Images et sprites
│   └── TextMesh Pro/        # Ressources TMP
├── Packages/                # Packages Unity
├── ProjectSettings/         # Configuration Unity
├── results/                 # Resultats d'entrainement ML
└── README.md
```

---

## Equipe

### M1 Data Engineer

| Nom | Role |
|-----|------|
| **FABBRI Yohann** | Ingenieur Agent IA |
| **SANNA Thomas** | Ingenieur Environnement Unity et Entrainements |
| **FURFARO Thomas** | Reward Designer |

### M1 Developpement Full Stack

| Nom | Role |
|-----|------|
| **BOISSAY Nathan** | Lead Developpeur Full-Stack |
| **BOISSAY Robin** | Developpeur et Administrateur de BDD |
| **AHISSOU Meldi** | Developpeur et Artiste |
| **GEORGET Korentin** | Developpeur et Artiste |

---

## Licence

Ce projet est developpe dans un cadre academique.

---

## Contact

Pour toute question concernant le projet, veuillez contacter l'equipe via les canaux universitaires appropries.
