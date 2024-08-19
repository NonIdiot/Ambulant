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
            Logger.LogDebug("OnEnable start");
            //On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            On.Player.CanBeSwallowed += canSwallowThis;
            Logger.LogDebug("OnEnable end");
        }

        private void OnDisable()
        {
            //if (restartMode)
            //{
            //    Hooks.RemoveHooks();
            //};
        }

        private bool canSwallowThis(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
        {
            var isTrue = false;
            if (orig(self, testObj))
            {
                isTrue = true;
            }

            if (SwallowOnlyPearls.TryGet(self, out var swallowporl))
            {
                if (swallowporl == true)
                {
                    if (!(testObj is DataPearl))
                    {
                        isTrue = false;
                    }
                }
            }
            return isTrue;
        }
    }
}