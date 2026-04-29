using UnityEngine;
using BreakInfinity;
using System;

/// <summary>
/// Classe pour les améliorations qui augmentent le DPC (Damage Per Click).
/// </summary>
/// // Permet de créer l'asset dans Unity : clic droit -> Create -> Shop -> Upgrade Definition
[CreateAssetMenu(fileName = "NewDPCUpgrade", menuName = "Shop/DPC Upgrade Definition")] 
public class DPCUpgradeSO : StatsUpgrade
{
    public override void Purchase(PlayerData data)
    {
        int amount = ShopManager.instance.getBuyAmount();
        int currentLevel = GetLevel();

        BigDouble totalCost = 0;
        if (growthCostFactor == 1f)
        {
            totalCost = baseCost * amount;
        }
        else
        {
            BigDouble firstLevelCost = baseCost * BigDouble.Pow(growthCostFactor, currentLevel);
            totalCost = firstLevelCost * (BigDouble.Pow(growthCostFactor, amount) - 1) / (growthCostFactor - 1);
        }

        if(data.SpendCurrency(totalCost))
        {
            data.IncrementUpgradeLevel(this.upgradeID, amount);
        }
    }

    public override BigDouble CalculateTotalStatValue(int currentLevel)
    {
        BigDouble totalStatGain = 0;
        
        // On commence avec un multiplicateur de base de 1 (ou ta valeur par défaut)
        BigDouble currentMultiplier = 1; 
        
        // Le dernier niveau que nous avons déjà comptabilisé dans le calcul
        int levelsAlreadyCalculated = 0;

        // 2. On parcourt chaque palier
        foreach (BaseMilestone milestone in milestones)
        {
            // Si notre niveau actuel est inférieur à ce palier, on s'arrête ici pour la boucle
            // Mais on doit quand même ajouter les niveaux restants avant de sortir
            if (currentLevel < milestone.milestoneLevel)
            {
                break; 
            }

            // --- CALCUL DU BLOC ---
            // Nombre de niveaux dans cette tranche (ex: entre le niv 10 et 50, il y a 40 niveaux)
            int levelsInThisChunk = milestone.milestoneLevel - levelsAlreadyCalculated;

            if (levelsInThisChunk > 0)
            {
                // On ajoute la valeur de ces niveaux avec le multiplicateur ACTUEL (avant ce palier)
                totalStatGain += levelsInThisChunk * baseStatGain * currentMultiplier;
            }

            // Maintenant qu'on a franchi ce palier, on met à jour le tracking
            levelsAlreadyCalculated = milestone.milestoneLevel;

            // Et on applique le boost du palier pour les PROCHAINS niveaux
            // Attention : Si tes bonus s'additionnent (+2) ou se multiplient (*2)
            // Ici je suppose qu'ils se multiplient comme dans ta description (x2, puis x4...)
            currentMultiplier *= milestone.statBonusMultiplier; 
        }

        // 3. Ajouter le reste des niveaux (ceux après le dernier palier franchi)
        int remainingLevels = currentLevel - levelsAlreadyCalculated;
        
        if (remainingLevels > 0)
        {
            totalStatGain += remainingLevels * baseStatGain * currentMultiplier;
        }

        return totalStatGain;
    }
    
    public override bool IsRequirementsMet(int amount = 1)
    {   
        int currentLevel = GetLevel();

        PlayerData data = StatsManager.Instance.currentPlayerData;

        BigDouble totalCost = 0;
        if (growthCostFactor == 1f)
        {
            totalCost = baseCost * amount;
        }
        else
        {
            BigDouble firstLevelCost = baseCost * BigDouble.Pow(growthCostFactor, currentLevel);
            totalCost = firstLevelCost * (BigDouble.Pow(growthCostFactor, amount) - 1) / (growthCostFactor - 1);
        }

        if (levelMax == 0 && totalCost <= data.monnaiePrincipale)
        {
            return true;
        }
        return totalCost <= data.monnaiePrincipale && (currentLevel + amount) <= levelMax;
    }
}