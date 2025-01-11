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
        public void OnEnable()
        {
            try
            {
                Logger.LogDebug("Ambulant's Plugin loading...");
                //On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

                On.Player.CanBeSwallowed += canSwallowThis;
                On.Player.MovementUpdate += StopRollingLmao;

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
            if (isTrue2)
            {
                self.rollDirection = 0;
                self.rollCounter = 20;
                self.slideCounter = 0;
                self.initSlideCounter = 0;
                self.stopRollingCounter = 20;
                self.longBellySlide = false;
            }
            orig(self, eu);
        }
        
        private static string Ambulant_Jolly_Name(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_GetPupButtonOffName orig, JollyPlayerSelector self)
        {
            SlugcatStats.Name playerClass = self.JollyOptions(self.index).playerClass;
            if (playerClass != null && playerClass.value == "Ambulant" && !self.JollyOptions(self.index).isPup)
            {
                return "ambulant_pup_off";
            } else
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
    }
}