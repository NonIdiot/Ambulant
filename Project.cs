using BepInEx;
using UnityEngine;
using System.Collections.Generic;
using System;
using Menu;
using System.Linq;
using System.IO;
using BepInEx.Logging;
using SlugBase.SaveData;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using JollyCoop.JollyMenu;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Ambulant
{
    [BepInDependency("slime-cubed.slugbase", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(MOD_ID, "The Ambulant", "0.0.1")]
    class Plugin : BaseUnityPlugin
    {
        public const string MOD_ID = "nassoc.ambulant";
        public static readonly PlayerFeature<bool> ReducedTech = PlayerBool("ambulant/reduced_tech");
        public static readonly PlayerFeature<bool> SwallowOnlyPearls = PlayerBool("ambulant/swallow_only_pearls");
        public static readonly PlayerFeature<bool> AdditionalTech = PlayerBool("ambulant/additional_tech");
        public static readonly PlayerFeature<bool> InefectiveShrooms = PlayerBool("ambulant/ineffective_shrooms");
        public static readonly PlayerFeature<bool> Escapist = PlayerBool("ambulant/escapist");
        public static readonly PlayerFeature<float> DeathByBiteMultiplier = PlayerFloat("ambulant/death_by_bite_multiplier");
        public void OnEnable()
        {
            try
            {
                Logger.LogDebug("Ambulant's Plugin loading...");
                //On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

                On.Player.CanBeSwallowed += canSwallowThis;
                On.Player.MovementUpdate += StopRollingLmao;
                On.Player.DeathByBiteMultiplier += deathByBite;
                On.Player.Update += StopRollingLmao2;

                //On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.HasUniqueSprite += Ambulant_Jolly_Sprite;
                //    On.JollyCoop.JollyMenu.JollyPlayerSelector.GetPupButtonOffName += Ambulant_Jolly_Name;
                //IL.Player.UpdateBodyMode += playerUpdateBody;
                Logger.LogDebug("Ambulant's Plugin successfully loaded!");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnDisable()
        {
            //if (restartMode)
            //{
            //    Hooks.RemoveHooks();
            //};
        }

        private void StopRollingLmao(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            var isTrue2 = false;
            if (ReducedTech.TryGet(self, out var reducetech))
            {
                if (reducetech == true)
                {
                    isTrue2 = true;
                }
            }
            var isTrue3 = false;
            if (AdditionalTech.TryGet(self, out var additiontech))
            {
                if (additiontech == true)
                {
                    isTrue3 = true;
                }
            }
            var isTrue4 = false;
            if (InefectiveShrooms.TryGet(self, out var noshroom))
            {
                if (noshroom == true)
                {
                    isTrue4 = true;
                }
            }
            if (isTrue2)
            {
                self.rollDirection = 0;
                self.rollCounter = 20;
                self.slideCounter = 0;
                self.initSlideCounter = 0;
                self.stopRollingCounter = 20;
                self.longBellySlide = false;
            }
            if (isTrue3)
            {
                self.allowRoll = 15;
                if (self.input[0].downDiagonal != 0)
                {
                    self.rollCounter = 11;
                }
            }
            if (isTrue4 && self.mushroomCounter > 40)
            {
                self.mushroomCounter = 40;
            }
            orig(self, eu);
        }

        private static string Ambulant_Jolly_Name(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_GetPupButtonOffName orig, JollyPlayerSelector self)
        {
            SlugcatStats.Name playerClass = self.JollyOptions(self.index).playerClass;
            if (playerClass != null && playerClass.value == "Ambulant" && !self.JollyOptions(self.index).isPup)
            {
                return "ambulant_pup_off";
            }
            else
            {
                //Logger.LogDebug("ambulant says: _"+playerClass.value+"_");
            }
            return orig(self);
        }

        private bool canSwallowThis(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
        {
            var isTrue = false;
            if (orig(self, testObj))
            {
                isTrue = true;
            }

            if (SwallowOnlyPearls.TryGet(self, out bool swallowporl) && swallowporl == true)
            {
                if (!(testObj is DataPearl))
                {
                    isTrue = false;
                }
            }
            return isTrue;
        }

        // thank u olaycolay for this code i would've never figured it out without it
        private static bool Ambulant_Jolly_Sprite(On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.orig_HasUniqueSprite orig, SymbolButtonTogglePupButton self)
        {
            // TODO: Make pup sprite
            if (self.symbolNameOff.Contains("ambulant") && !self.isToggled) return true;
            return orig(self);
        }
        private float deathByBite(On.Player.orig_DeathByBiteMultiplier orig, Player self)
        {
            //Logger.LogWarning("lets go gambling!");
            if (DeathByBiteMultiplier.TryGet(self, out float dbbm))
            {
                //Logger.LogWarning(dbbm);
                return dbbm;
            }
            //Logger.LogWarning("aw dangit");
            return orig(self);
        }

        private void StopRollingLmao2(On.Player.orig_Update orig, Player self, bool eu)
        {
            var isTrue5 = false;
            if (Escapist.TryGet(self, out var escape))
            {
                if (escape == true)
                {
                    isTrue5 = true;
                }
            }
            if (isTrue5 && !self.dead && self.dangerGrasp != null && self.dangerGrasp.grabber != null)
            {
                //if ((self.dangerGraspTime == 10 && UnityEngine.Random.value < 0.2)||(self.dangerGraspTime == 29 && UnityEngine.Random.value < 0.375))
                // haha 1-((1-x)(1-y))=z go brr where x is the first random value y is the second random value and z is the desired chance
                //{
                self.dangerGrasp.grabber.Stun(5);
                var room = self.room;
                var pos = self.mainBodyChunk.pos;
                var color = self.ShortCutColor();
                room.AddObject(new ExplosionSpikes(room, pos, 14, 30f, 9f, 7f, 170f, color));
                room.AddObject(new ShockWave(pos, 330f, 0.045f, 5, false));

                room.PlaySound(SoundID.Slugcat_Belly_Slide_Init, pos);
                room.InGameNoise(new Noise.InGameNoise(pos, 9000f, self, 1f));
                //}
            }
            orig(self, eu);
        }
    }
}