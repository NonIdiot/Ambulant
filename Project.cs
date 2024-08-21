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
            Logger.LogDebug("Ambulant's Plugin loading...");
            //On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            On.Player.CanBeSwallowed += canSwallowThis;
            IL.Player.UpdateBodyMode += playerUpdateBody;
            Logger.LogDebug("Ambulant's Plugin successfully loaded!");
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

        private void playerUpdateBody(ILContext il)
        {
            try
            {
                // thank you so much jacob_v_thaumiel and alphappy on discord you are so awesome
                ILCursor c = new ILCursor(il);
                // find the place before it's checking the x/y velocity
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdlocs(13),
                    x => x.MatchLdfld(typeof(Vector2).GetField(nameof(Vector2.x))),
                    x => x.MatchLdcR4(0.0f)
                );
                c.GotoPrev(MoveType.Before, x => x.MatchLdlocs(13));
                // start adding stuff, like the delegate to detect if the player has reduced tech
                c.MoveAfterLabels();
                c.EmitDelegate<Func<Player, bool>>((Player self) =>
                {
                    var returner = false;
                    if (ReducedTech.TryGet(self, out var reducedTeche)) {
                        returner = reducedTeche;
                    }
                    return returner;
                });
                // but wait, we gotta find where we want to send the code off to if the player has reduced tech
                ILCursor c2 = new ILCursor(il);
                c2.GotoNext(
                    MoveType.After,
                    x => x.MatchStlfd(typeof(int32).GetField(nameof(int32))),
                    x => x.MatchBr(out _),
                    x => x.MatchLdlocs(9),
                );
                c2.GotoPrev(MoveType.Before, x => x.MatchLdlocs(9));
                // okay now we can finish by directing to where we send the code off to if player has reduced tech
                c.Emit(OpCodes.Brtrue, c2.Next);
                // UnityEngine.Debug.Log(il);
            }
            catch (Exception e) { UnityEngine.Debug.Log(e); }
        }
    }
}