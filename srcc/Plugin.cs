using BepInEx;
using UnityEngine;
using System.Collections.Generic;
using ImprovedInput;
using System;
using Menu;
using SlugBase.SaveData;
using System.Linq;
using System.IO;
using BepInEx.Logging;

namespace Ambulant
{
    [BepInPlugin(MOD_ID, "The Ambulant", "0.0.1")]
    class Plugin : BaseUnityPlugin
    {
        public const string MOD_ID = "nassoc.ambulant";
        public static readonly PlayerFeature<bool> ReducedTech = PlayerFloat("ambulant/reduced_tech");
        public static readonly PlayerFeature<bool> SwallowOnlyPearls = PlayerFloat("ambulant/swallow_only_pearls");
        private void OnEnable() 
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            On.Player.hook_CanBeSwallowed += canSwallowThis;
        }

        private void OnDisable()
        {
            if (restartMode) {
                Hooks.RemoveHooks();
            };
        }

        private void canSwallowThis(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
        {
            orig(self);

            if (SwallowOnlyPearls.TryGet(self, out var swallowporl)) {
                if (swallowporl==true) {
                    //something here
                }
            }
        }
    }
}