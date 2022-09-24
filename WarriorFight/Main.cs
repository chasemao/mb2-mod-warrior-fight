using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using HarmonyLib;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Party;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.SandBox.GameComponents;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;
using TaleWorlds.CampaignSystem.ViewModelCollection.Craft.WeaponDesign;
using System.Collections.ObjectModel;
using TaleWorlds.Library;
using TaleWorlds.Engine;
using Newtonsoft.Json;

namespace WarriorFight
{
    public class Main : MBSubModuleBase
    {
        private Harmony harmonyKit;
        public override void OnGameLoaded(Game game, object initializerObject)
        {
            InformationManager.DisplayMessage(new InformationMessage("hello world"));
        }
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            try
            {
                this.harmonyKit = new Harmony("AutoCreateNewParty.harmony");
                this.harmonyKit.PatchAll();
                InformationManager.DisplayMessage(new InformationMessage("AutoCreateNewParty loaded"));
            }
            catch (Exception ex)
            {
                FileLog.Log("err:" + ex.ToString());
                FileLog.FlushBuffer();
                InformationManager.DisplayMessage(new InformationMessage("err:" + ex.ToString()));
            }
        }
    }

    [HarmonyPatch(typeof(Mission), "DecideAgentKnockedByBlow")]
    public class PatchDecideAgentKnockedByBlow
    {
        public static void Postfix(
            Agent attacker,
            Agent victim,
            in AttackCollisionData collisionData,
            WeaponComponentData attackerWeapon,
            bool isInitialBlowShrugOff,
            ref Blow blow)
        {
            if (victim.IsMainAgent)
            {
                blow.BlowFlag &= ~BlowFlags.KnockBack;
                blow.BlowFlag &= ~BlowFlags.KnockDown;
            }
        }
    }

    [HarmonyPatch(typeof(Mission), "DecideMountRearedByBlow")]
    public class PatchDecideMountRearedByBlow
    {
        public static void Postfix(
            Agent attackerAgent,
            Agent victimAgent,
            in AttackCollisionData collisionData,
            WeaponComponentData attackerWeapon,
            float rearDamageThresholdMultiplier,
            Vec3 blowDirection,
            ref Blow blow)
        {
            if (victimAgent.IsMainAgent)
            {
                blow.BlowFlag &= ~BlowFlags.MakesRear;
            }
        }
    }

    [HarmonyPatch(typeof(Mission), "DecideAgentShrugOffBlow")]
    public class PatchDecideAgentShrugOffBlow
    {
        protected static void Postfix(
            Agent victimAgent,
            AttackCollisionData collisionData,
            ref Blow blow,
            ref bool __result)
        {
            if (victimAgent.IsMainAgent)
            {
                blow.BlowFlag |= BlowFlags.ShrugOff;
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Mission), "UpdateMomentumRemaining")]
    public class PatchUpdateMomentumRemaining
    {
        protected static bool Prefix(
            ref float momentumRemaining,
            Blow b,
            in AttackCollisionData collisionData,
            Agent attacker,
            Agent victim,
            in MissionWeapon attackerWeapon,
            bool isCrushThrough)
        {
            if (attacker.IsMainAgent)
            {
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Mission), "MeleeHitCallback")]
    public class PatchMeleeHitCallback
    {
        protected static void Prefix(
            ref AttackCollisionData collisionData,
            Agent attacker,
            Agent victim,
            GameEntity realHitEntity,
            ref float inOutMomentumRemaining,
            ref MeleeCollisionReaction colReaction,
            ref CrushThroughState crushThroughState,
            Vec3 blowDir,
            Vec3 swingDir,
            ref HitParticleResultData hitParticleResultData,
            bool crushedThroughWithoutAgentCollision)
        {
            if (attacker.IsMainAgent)
            {
                crushThroughState = CrushThroughState.CrushedThisFrame;
                object boxed = collisionData;
                Traverse.Create(boxed).Field("_attackBlockedWithShield").SetValue(false);
                Traverse.Create(boxed).Field("_collisionResult").SetValue(1);
                collisionData = (AttackCollisionData)boxed;
                FileLog.Log("mylog\n" +
                    "victim=" + victim.Name + "\n" +
                    "collisionData=" + JsonConvert.SerializeObject(collisionData) + "\n" +
                    "inOutMomentumRemaining=" + inOutMomentumRemaining.ToString() + "\n" +
                    "colReaction=" + JsonConvert.SerializeObject(colReaction) + "\n" +
                    "crushThroughState=" + JsonConvert.SerializeObject(crushThroughState) + "\n" +
                    "hitParticleResultData=" + JsonConvert.SerializeObject(hitParticleResultData) + "\n" +
                    "crushedThroughWithoutAgentCollision=" + crushedThroughWithoutAgentCollision.ToString() + "\n");
            }
        }
        protected static void Postfix(
            ref AttackCollisionData collisionData,
            Agent attacker,
            Agent victim,
            GameEntity realHitEntity,
            ref float inOutMomentumRemaining,
            ref MeleeCollisionReaction colReaction,
            ref CrushThroughState crushThroughState,
            Vec3 blowDir,
            Vec3 swingDir,
            ref HitParticleResultData hitParticleResultData,
            bool crushedThroughWithoutAgentCollision)
        {
            if (attacker.IsMainAgent)
            {
                FileLog.Log("mylog\n" +
                    "victim=" + victim.Name + "\n" +
                    "collisionData=" + JsonConvert.SerializeObject(collisionData) + "\n" +
                    "inOutMomentumRemaining=" + inOutMomentumRemaining.ToString() + "\n" +
                    "colReaction=" + JsonConvert.SerializeObject(colReaction) + "\n" +
                    "crushThroughState=" + JsonConvert.SerializeObject(crushThroughState) + "\n" +
                    "hitParticleResultData=" + JsonConvert.SerializeObject(hitParticleResultData) + "\n" +
                    "crushedThroughWithoutAgentCollision=" + crushedThroughWithoutAgentCollision.ToString() + "\n");
            }
        }
    }
    [HarmonyPatch(typeof(WeaponComponentData), "CanHitMultipleTargets", MethodType.Getter)]
    public class PatchCanHitMultipleTargets
    {
        public static void Postfix(ref bool __result)
        {
            __result = true;
        }
    }   
}
