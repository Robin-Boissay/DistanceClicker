# 🎮 Guide de Configuration Multi-Environnement ML-Agents

## 📸 Ta Hiérarchie Actuelle

```
▼ SampleScene
    ├── Main Camera
    ├── Directional Light
    ├── Global Volume
    ├── Canvas
    │   ├── BackgroundImage
    │   ├── Header
    │   ├── middleGame
    │   ├── LeaderBoardPanelBackground
    │   ├── LeaderBoardBtn
    │   ├── SettingPanel
    │   ├── SettingBtn
    │   └── GlobalShop
    ├── EventSystem
    ├── _GameManager_
    ├── _ShopManager_
    ├── _DistanceManager_
    ├── _UIManager_
    ├── _ClickCircleSpawner_
    ├── _StatsManager_
    ├── _SaveManager_
    ├── _FireBaseManager_
    ├── _LeaderBoardManager_
    ├── _ProfileManager_
    ├── _IdleManager_
    └── ML-Agent              ← À TRANSFORMER EN PREFAB
```

---

## 🗂️ OÙ TROUVER LES ÉLÉMENTS RÉFÉRENCÉS

Avant de commencer, voici où trouver les éléments que tu devras assigner :

| Élément à trouver | Où le trouver |
|-------------------|---------------|
| **Prefab de cercle bonus** | Regarde dans `_ClickCircleSpawner_` → champ `Click Circle Prefab` |
| **Première cible (DistanceObjectSO)** | Regarde dans `_DistanceManager_` → champ `Premiere Cible` |
| **Liste des upgrades** | Regarde dans `_StatsManager_` → champ `All Upgrades Database` |
| **MLAgentConfiguration** | Regarde dans `ML-Agent` → composant `DistanceClickerAgent` → champ `Config` |
| **Zone d'apparition des cercles** | Regarde dans `_ClickCircleSpawner_` → champ `Zone Apparition` |

> **Astuce** : Tu peux cliquer sur un champ dans l'Inspector et noter le nom de l'asset, puis le chercher dans le dossier `Assets/` avec Ctrl+F dans le Project.

---

## 🎯 Objectif Final

```
▼ SampleScene
    ├── Main Camera
    ├── Directional Light
    ├── Global Volume
    ├── Canvas (UI du joueur - INCHANGÉ)
    ├── EventSystem
    │
    ├── [MANAGERS EXISTANTS - INCHANGÉS]
    │   ├── _GameManager_
    │   ├── _ShopManager_
    │   ├── _DistanceManager_    ← Utilisé par le JOUEUR
    │   ├── _UIManager_
    │   ├── _ClickCircleSpawner_ ← Utilisé par le JOUEUR
    │   ├── _StatsManager_       ← Utilisé par le JOUEUR
    │   ├── _SaveManager_        ← Firebase (joueur)
    │   ├── _FireBaseManager_
    │   ├── _LeaderBoardManager_
    │   ├── _ProfileManager_
    │   └── _IdleManager_
    │
    ├── [NOUVEAUX MANAGERS]
    │   ├── _EnvironmentFactory_     ← NOUVEAU
    │   ├── _CompetitionManager_     ← NOUVEAU
    │   └── _MLEnvironmentSaveManager_ ← NOUVEAU
    │
    └── [ENVIRONNEMENTS ML - Créés automatiquement]
        ├── Environment_Bot 1
        │   └── (GameEnvironment + Agent)
        └── Environment_Bot 2
            └── (GameEnvironment + Agent)
```

---

## 🔧 ÉTAPE 1 : Créer le Prefab d'Environnement ML

### 1.1 - Transformer ton ML-Agent actuel

1. **Sélectionne `ML-Agent`** dans ta hiérarchie

2. **Crée un nouveau GameObject vide** comme parent :
   - Clic droit sur `ML-Agent` → `Create Empty Parent`
   - Renomme ce parent en : `ML-Agent Environment Template`

3. **Ta hiérarchie devrait ressembler à :**
   ```
   ▼ ML-Agent Environment Template
       └── ML-Agent (ton agent existant)
   ```

### 1.2 - Ajouter les composants au parent

1. **Sélectionne `ML-Agent Environment Template`** (le parent)

2. **Ajoute le script `GameEnvironment`** :
   - Dans l'Inspector → Add Component → Cherche "GameEnvironment"

3. **Configure `GameEnvironment`** :

   | Champ | Valeur | Où trouver ? |
   |-------|--------|--------------|
   | Environment Name | *(laisse vide)* | Sera assigné auto par EnvironmentFactory |
   | Is Player Controlled | ❌ Décoché | - |
   | **Références Locales** | *(laisse tout vide)* | Auto-détecté au runtime |
   | **Données du Joueur** | *(laisse vide)* | Créé au runtime |
   | Shared Upgrades Database | *(laisse vide)* | Sera assigné par EnvironmentFactory |
   | Premiere Cible | *(laisse vide)* | Sera assigné par EnvironmentFactory |

   > **Note** : On laisse tout vide dans le prefab car `EnvironmentFactory` assignera les bonnes valeurs !

### 1.3 - Ajouter les Managers Locaux

1. **Crée un GameObject enfant** nommé `LocalManagers` :
   - Clic droit sur `ML-Agent Environment Template` → Create Empty
   - Renomme-le `LocalManagers`
   
   ```
   ▼ ML-Agent Environment Template
       ├── LocalManagers          ← NOUVEAU
       └── ML-Agent
   ```

2. **Ajoute 3 scripts sur `LocalManagers`** :
   - Add Component → `LocalDistanceManager`
   - Add Component → `LocalStatsManager`
   - Add Component → `LocalClickCircleSpawner`

3. **Configure `LocalDistanceManager`** :
   | Champ | Valeur |
   |-------|--------|
   | Environment | *(laisse vide)* - Auto-assigné |
   | Premiere Cible | *(laisse vide)* - Reçu de GameEnvironment |

4. **Configure `LocalStatsManager`** :
   | Champ | Valeur |
   |-------|--------|
   | Environment | *(laisse vide)* - Auto-assigné |
   | All Upgrades Database | *(laisse vide)* - Reçu de GameEnvironment |

5. **Configure `LocalClickCircleSpawner`** :
   | Champ | Valeur | Où trouver ? |
   |-------|--------|--------------|
   | Environment | *(laisse vide)* | Auto-assigné |
   | Click Circle Prefab | **Le même que dans `_ClickCircleSpawner_`** | Sélectionne `_ClickCircleSpawner_` dans ta scène → copie le prefab du champ `Click Circle Prefab` |
   | Zone Apparition | *(laisse vide pour l'instant)* | Voir section 1.4 |
   | Temps Entre Apparitions | *(laisse par défaut)* | Calculé au runtime |

### 1.4 - (Optionnel) Zone d'apparition des cercles pour les bots

**Si tu veux que les bots aient leurs propres cercles bonus** (recommandé pour l'entraînement) :

1. Crée un Canvas enfant sous `ML-Agent Environment Template` :
   - Clic droit → UI → Canvas
   - Renomme-le `BotCanvas`
   - Configure le Canvas :
     - Render Mode : `Screen Space - Overlay`
     - Sort Order : différent du Canvas principal (ex: 10)

2. Crée un RectTransform enfant :
   - Clic droit sur `BotCanvas` → Create Empty
   - Renomme-le `BonusCircleZone`
   - Configure le RectTransform :
     - Anchors : Stretch (tout l'écran) ou une zone spécifique
     - **Copie les mêmes dimensions que la zone dans `_ClickCircleSpawner_`**

3. Assigne `BonusCircleZone` dans `LocalClickCircleSpawner` → `Zone Apparition`

> **Alternative simple** : Si tu ne veux PAS de cercles bonus pour les bots, laisse `Zone Apparition` vide et le composant ne créera pas de cercles.

### 1.5 - Modifier le script de l'Agent

1. **Sélectionne `ML-Agent`** (l'enfant, pas le parent)

2. **Remplace le script `DistanceClickerAgent`** par `DistanceClickerAgentMultiEnv` :
   - Clique sur le composant `DistanceClickerAgent` dans l'Inspector
   - Clic droit sur le nom du composant → Remove Component
   - Add Component → `DistanceClickerAgentMultiEnv`

3. **Configure `DistanceClickerAgentMultiEnv`** :
   | Champ | Valeur | Où trouver ? |
   |-------|--------|--------------|
   | Game Environment | *(laisse vide)* | Auto-détecté (cherche le parent) |
   | Config | **Le même que l'ancien agent** | C'était dans l'ancien `DistanceClickerAgent` → champ `Config`. Probablement dans `Assets/Resources/MLAgentConfig` ou `Assets/ML-Agents/` |

4. **Garde les composants ML-Agents existants** :
   - `Behavior Parameters` - Ne touche pas
   - `Decision Requester` - Ne touche pas

### 1.6 - Créer le Prefab

1. **Glisse `ML-Agent Environment Template`** de la Hiérarchie vers le dossier `Assets/Prefabs/`

2. **Une fenêtre apparaît** : Choisis "Original Prefab"

3. **Supprime `ML-Agent Environment Template`** de la scène (Hierarchy)
   - Clic droit → Delete
   - Le Prefab est maintenant sauvegardé dans `Assets/Prefabs/` !

**Structure finale du Prefab :**
```
▼ ML-Agent Environment Template (Prefab)
    ├── GameEnvironment (Script)
    │
    ├── LocalManagers
    │   ├── LocalDistanceManager (Script)
    │   ├── LocalStatsManager (Script)
    │   └── LocalClickCircleSpawner (Script)
    │
    ├── (Optionnel) BotCanvas
    │   └── BonusCircleZone
    │
    └── ML-Agent
        ├── DistanceClickerAgentMultiEnv (Script)
        ├── Behavior Parameters
        └── Decision Requester
```

---

## 🔧 ÉTAPE 2 : Ajouter les Nouveaux Managers

### 2.1 - Créer `_EnvironmentFactory_`

1. **Clic droit dans Hierarchy** → Create Empty
2. **Renomme-le** `_EnvironmentFactory_`
3. **Add Component** → `EnvironmentFactory`

4. **Configure dans l'Inspector** :
   | Champ | Valeur | Où trouver ? |
   |-------|--------|--------------|
   | Environment Prefab | Ton prefab `ML-Agent Environment Template` | **Dans `Assets/Prefabs/`** - le prefab que tu viens de créer |
   | Shared Upgrades | La liste des upgrades | **Copie depuis `_StatsManager_`** → champ `All Upgrades Database` |
   | Premiere Cible | Le premier DistanceObjectSO | **Copie depuis `_DistanceManager_`** → champ `Premiere Cible` |
   | Environments Parent | *(laisse vide)* | Les environnements seront créés sous ce GameObject |
   | Number Of ML Agents To Create | **2** | Nombre de bots |
   | Auto Create On Start | ✅ Coché | Crée les bots automatiquement |

### 2.2 - Créer `_CompetitionManager_`

1. **Clic droit dans Hierarchy** → Create Empty
2. **Renomme-le** `_CompetitionManager_`
3. **Add Component** → `CompetitionManager`

4. **Configure dans l'Inspector** :
   | Champ | Valeur |
   |-------|--------|
   | Ml Agents Play In Real Time | ✅ Coché |
   | Leaderboard Update Interval | 1 |
   | ML Agent Environments | *(laisse vide)* - Rempli auto par EnvironmentFactory |
   | **UI du Classement** | *(tout optionnel, laisse vide pour l'instant)* |
   | **Panneau Mini-Classement** | *(tout optionnel, laisse vide pour l'instant)* |

### 2.3 - Créer `_MLEnvironmentSaveManager_`

1. **Clic droit dans Hierarchy** → Create Empty
2. **Renomme-le** `_MLEnvironmentSaveManager_`
3. **Add Component** → `MLEnvironmentSaveManager`

4. **Configure dans l'Inspector** :
   | Champ | Valeur |
   |-------|--------|
   | Save File Name | ml_environments_save.json |
   | Auto Save | ✅ Coché |
   | Auto Save Interval | 30 |

---

## 🔧 ÉTAPE 3 : Configuration Optionnelle de l'UI

### 3.1 - Mini-Classement (toujours visible)

Si tu veux afficher le rang du joueur en permanence :

1. **Dans ton Canvas**, crée un Panel nommé `MiniLeaderboard`
2. Ajoute 2 TextMeshProUGUI :
   - `RankText` (ex: "🥇 #1/3")
   - `GapText` (ex: "+1.5K d'avance")

3. **Dans `_CompetitionManager_`**, assigne :
   | Champ | Valeur |
   |-------|--------|
   | Mini Leaderboard Panel | Ton Panel `MiniLeaderboard` |
   | Mini Rank Text | Ton `RankText` |
   | Mini Score Gap Text | Ton `GapText` |

---

## ✅ ÉTAPE 4 : Vérification Finale

Ta hiérarchie devrait maintenant ressembler à :

```
▼ SampleScene
    ├── Main Camera
    ├── Directional Light
    ├── Global Volume
    │
    ├── Canvas
    │   ├── BackgroundImage
    │   ├── Header
    │   ├── middleGame
    │   ├── LeaderBoardPanelBackground
    │   ├── LeaderBoardBtn
    │   ├── SettingPanel
    │   ├── SettingBtn
    │   ├── GlobalShop
    │   └── (Optionnel) MiniLeaderboard
    │
    ├── EventSystem
    │
    ├── _GameManager_
    ├── _ShopManager_
    ├── _DistanceManager_
    ├── _UIManager_
    ├── _ClickCircleSpawner_
    ├── _StatsManager_
    ├── _SaveManager_
    ├── _FireBaseManager_
    ├── _LeaderBoardManager_
    ├── _ProfileManager_
    ├── _IdleManager_
    │
    ├── _EnvironmentFactory_        ← NOUVEAU
    ├── _CompetitionManager_        ← NOUVEAU
    └── _MLEnvironmentSaveManager_  ← NOUVEAU
```

> **Note importante** : `ML-Agent` n'est plus dans la scène ! Il sera créé automatiquement par `EnvironmentFactory`.

---

## 🎮 ÉTAPE 5 : Tester

1. **Lance le jeu** en mode Play

2. **Vérifie dans la Console** que tu vois :
   ```
   EnvironmentFactory: 2 environnements créés.
   [Bot 1] Environnement initialisé avec succès.
   [Bot 2] Environnement initialisé avec succès.
   CompetitionManager: 3 compétiteurs initialisés (1 joueur + 2 bots)
   CompetitionManager: Compétition démarrée!
   ```

3. **Vérifie dans la Hiérarchie** que les environnements sont créés :
   ```
   ├── _EnvironmentFactory_
   │   ├── Environment_Bot 1
   │   └── Environment_Bot 2
   ```

---

## 💾 Comportement de la Sauvegarde

| Événement | Joueur | Bots |
|-----------|--------|------|
| Ouvre l'app | Charge depuis Firebase | Charge depuis fichier local |
| Toutes les 30s | *(via SaveManager)* | Sauvegarde automatique |
| App en arrière-plan | *(via SaveManager)* | Sauvegarde automatique |
| Ferme l'app | *(via SaveManager)* | Sauvegarde automatique |

---

## 🔄 Résumé des Éléments à Copier

| Tu dois assigner | Copie depuis | Champ |
|------------------|--------------|-------|
| Environment Prefab | *(le prefab que tu crées)* | `Assets/Prefabs/ML-Agent Environment Template` |
| Shared Upgrades | `_StatsManager_` | `All Upgrades Database` |
| Premiere Cible | `_DistanceManager_` | `Premiere Cible` |
| Click Circle Prefab | `_ClickCircleSpawner_` | `Click Circle Prefab` |
| MLAgentConfiguration | Ancien `ML-Agent` | `Config` (probablement dans `Assets/Resources/`) |

---

## ❓ FAQ

### Q: Les bots jouent-ils quand l'app est fermée ?
**Non.** Les bots jouent uniquement quand l'app est ouverte. Leur progression est sauvegardée localement.

### Q: Où sont stockées les données des bots ?
Dans `Application.persistentDataPath/ml_environments_save.json`
- **Android** : `/data/data/[package]/files/`
- **iOS** : `/var/mobile/.../Documents/`
- **PC** : `C:\Users\[user]\AppData\LocalLow\[company]\[product]\`

### Q: Puis-je avoir plus de 2 bots ?
Oui ! Change `Number Of ML Agents To Create` dans `EnvironmentFactory`.

### Q: Comment réinitialiser les bots ?
Appelle `CompetitionManager.Instance.ResetCompetition()` depuis un bouton.

### Q: Les champs "Références Locales" dans GameEnvironment doivent-ils être remplis ?
**Non !** Ils sont auto-détectés. Laisse-les vides.

---

Bonne chance ! 🎮🤖
