using BepInEx;
using UnityEngine;
using Expedition;
using System;
using Menu;
using Menu.Remix.MixedUI;
using System.Linq;
using BepInEx.Logging;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using JollyCoop.JollyMenu;
using MSCSceneID = MoreSlugcats.MoreSlugcatsEnums.MenuSceneID;
using System.Runtime.CompilerServices;
using HUD;
using DevConsole.Commands;
using DevConsole;
using RWCustom;
using System.Collections.Generic;
//using PupBase;
//using PupOnHibernation;

namespace Ambulant
{
    public static class GeneralCWT
    {
        static ConditionalWeakTable<Player, Data> table = new ConditionalWeakTable<Player, Data>();
        public static Data GetCustomData(this Player self) => table.GetOrCreateValue(self);

        public class Data
        {
            // variables
            public int dizziness = 0;
            public int timeUntilUndizzy = 0;
            public int stopFuckingDying = 0;

            public int throwBoostCool = 0;
        }
    }

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
        public static readonly PlayerFeature<bool> DizzyMechanicActive = PlayerBool("ambulant/has_dizzy_mechanic");
        public static readonly PlayerFeature<bool> ThrowFixed = PlayerBool("ambulant/throw_fixed");
        public static readonly PlayerFeature<float> DeathByBiteMultiplier = PlayerFloat("ambulant/death_by_bite_multiplier");

        //public class AmbulantValuesNShit
        //{
        //    public static SlugcatStats.Name CombustivePup;
        
        //    public static void RegisterValues()
        //    {
        //        CombustivePup = new SlugcatStats.Name("Stalkerpup", register: true);
        //    }

        //    public static void UnregisterValues()
        //    {
        //        CombustivePup?.Unregister();
        //        CombustivePup = null;
        //    }
        //}

        // thank you alphappy for logging help too
        internal static BepInEx.Logging.ManualLogSource logger;
        internal static void Log(LogLevel loglevel, object msg) => logger.Log(loglevel, msg);
        public Plugin()
        {
            logger = Logger;
        }

        public static readonly bool[][] ScugBalance = { 
            // crawlflip, beamtipflip, verticalchargepounce, lowermovstun, fastpoleclimb,
            // fastcorridorclimb, funnyflip, escapeliz, escapemiros, fastcorrturn,
            // lessfastcorrturn, leechroll, maggotroll, throwfix, crawlturnroll

            //
            // survivor
            new bool[] {false,false,false,true,true,
                            false,false,false,false,false,
                            false,false,false,false,false},
            // monk
            new bool[] {false,false,false,false,false,
                            false,false,false,false,false,
                            false,true,true,false,false},
            // hunter
            new bool[] {false,false,false,false,false,
                            false,false,false,false,true,
                            true,false,true,true,false},
            // watcher
            new bool[] {false,false,false,true,false,
                            true,true,false,false,false,
                            false,true,true,true,false},
            // gourmand
            new bool[] {true,false,false,false,false,
                            false,true,false,false,false,
                            false,true,true,true,true},
            //
            // artificer
            new bool[] {true,true,true,true,true,
                            false,true,false,false,true,
                            true,true,false,false,false},
            // spearmaster
            new bool[] {false,false,false,true,true,
                            true,true,false,false,true,
                            true,false,false,true,false},
            // rivulet
            new bool[] {true,true,false,true,true,
                            false,false,false,false,true,
                            false,false,false,false,false},
            // saint
            new bool[] {false,false,false,false,false,
                            false,false,false,false,false,
                            false,false,false,false,false},
            // inv
            new bool[] {false,false,false,true,true,
                            false,false,false,false,true,
                            true,true,true,false,true},
            //
            // vinki
            new bool[] {true,true,true,true,false,
                            false,true,false,true,true,
                            true,true,true,false,false},
            // pearlcat
            new bool[] {false,true,false,false,false,
                            false,true,false,true,true,
                            true,false,false,false,false},
            // beecat
            new bool[] {false,false,false,true,true,
                            false,false,false,false,false,
                            false,true,true,false,true},
            // mariner
            new bool[] {true,true,false,true,false,
                            true,false,true,false,true,
                            false,false,true,true,false},
            // nightstriker
            new bool[] {false,false,false,true,false,
                            false,false,false,false,true,
                            true,true,false,true,false},
            //
            // gravel eater
            new bool[] {true,false,false,false,false,
                            false,true,false,true,false,
                            false,true,true,false,true},
            // forager [copied from gourmand]
            new bool[] {true,false,false,false,false,
                            false,true,false,false,false,
                            false,true,true,true,true},
            // unbound
            new bool[] {true,false,true,false,false,
                            false,false,false,true,true,
                            true,false,true,false,false},
            // entropy (QuesInt)
            new bool[] {false,false,false,true,false,
                            true,true,false,false,false,
                            false,false,false,false,false},
            // marauder (QuesInt)
            new bool[] {false,false,false,false,false,
                            false,true,false,false,true,
                            true,false,false,true,false},
            //
            // snowflake (WintEnd)
            new bool[] {false,true,true,true,false,
                            true,true,false,false,true,
                            false,false,false,true,false},
            // murderer
            new bool[] {true,false,true,true,false,
                            true,true,false,true,true,
                            false,false,true,false,false},
            // bombadier
            new bool[] {false,false,false,false,false,
                            false,true,true,false,true,
                            true,false,false,true,false},
            // incandescent (Hailstorm)
            new bool[] {false,true,false,false,false,
                            false,false,true,false,true,
                            true,false,false,false,false},
            // seer
            new bool[] {false,false,false,true,false,
                            false,false,false,false,true,
                            true,true,true,false,false},
            //
            // friend
            new bool[] {false,false,false,false,false,
                            false,false,false,false,true,
                            true,true,false,false,false},
            // noircatto
            new bool[] {false,false,true,false,false,
                            false,false,false,false,true,
                            false,true,true,false,false},
            // sacrifice
            new bool[] {false,false,true,false,false,
                            false,false,false,true,false,
                            false,false,false,false,false},
            // executioner
            new bool[] {false,false,false,true,false,
                            false,false,false,false,true,
                            true,true,true,true,false},
            // hermit
            new bool[] {false,false,false,true,false,
                            false,false,false,true,false,
                            false,true,true,false,true},
            //
            // beacon
            new bool[] {false,false,false,true,false,
                            false,false,false,true,true,
                            true,false,true,false,false},
            // impossible
            new bool[] {false,false,false,true,false,
                            false,false,false,false,false,
                            false,true,true,false,true},
            // wingcat
            new bool[] {true,true,false,false,false,
                            false,false,false,false,false,
                            false,true,false,false,false},
            // guide
            new bool[] {true,false,false,false,false,
                            false,false,false,false,false,
                            false,false,true,false,true},
            //
            //
            // placeholder
            new bool[] {false,false,false,false,false,
                            false,false,false,false,false,
                            false,false,false,false,false},
            // pouncer
            new bool[] {false,true,false,true,true,
                            false,false,false,false,true,
                            false,true,false,true,false},
            // barreltail [copied from spearmaster]
            new bool[] {false,false,false,true,true,
                            true,true,false,false,true,
                            true,false,false,true,false}
        };

        public static readonly string[] ScugBalanceIDs = new string[] {
            "White", "Yellow", "Red", "Watcher", "Gourmand",
            "Artificer", "Spearmaster", "Rivulet", "Saint", "Inv",
            "vinki", "Pearlcat", "bee", "Mariner", "The Nightmare",
            "Gravelslug", "Cloudtail", "NCRunbound", "NCREntropy", "NCRMarauder",
            "SnowflakeCat", "Murderer", "Bombardier", "Incandescent", "Seer",
            "Friend", "noircatto", "Sacrifice", "Executioner", "Hermit",
            "Beacon", "Impossible", "MMWingcat", "Guide"
        };

        private static bool[] ScugHasAbility(Player self)
        {
            bool[] fakeBalance = new bool[15];
            bool[] currentBalance = new bool[15];
            int[] configOptions = new int[] {
                1, 1, 1, 1, 1,
                1, 1, 1, 1, 1,
                1, 1, 1, 1, 1
            };
            int coolIndex = -1;
            Plugin.Log(LogLevel.Info, self.SlugCatClass.value);
            for (var a = 0; a < ScugBalanceIDs.Length; a++)
            {
                if ((ScugBalanceIDs[a]).ToLower() == (self.SlugCatClass.value).ToLower())
                {
                    coolIndex = a;
                    break;
                }
            }
            if (coolIndex != -1)
            {
                Plugin.Log(LogLevel.Info, "youre correc");
                fakeBalance = ScugBalance[coolIndex];
                for (var a = 0; a < 15; a++)
                {
                    if (configOptions[a] == 2) // if the given ability is set to ALL
                    {
                        currentBalance[a] = true;
                    }
                    else if (configOptions[a] == 1) // if the given ability is set to Rebalanced
                    {
                        currentBalance[a] = fakeBalance[a];
                    }

                }
            }
            else
            {
                if (AdditionalTech.TryGet(self, out var additiontech) && additiontech == true)
                {
                    currentBalance[0] = true;
                    currentBalance[1] = true;
                    currentBalance[2] = true;
                    currentBalance[3] = true;
                    currentBalance[4] = true;
                    currentBalance[5] = true;
                    currentBalance[6] = true;
                    currentBalance[9] = true;
                    currentBalance[14] = true;
                }
                if (Escapist.TryGet(self, out var escape) && escape == true)
                {
                    currentBalance[7] = true;
                    currentBalance[8] = true;
                    currentBalance[11] = true;
                    currentBalance[12] = true;
                }
                if (ThrowFixed.TryGet(self, out var throwfixeed2) && throwfixeed2 == true)
                {
                    currentBalance[13] = true;
                }
            }
            return currentBalance;
        }

        public static bool[] modsOn = {
            false, // rotund world
            false, // lancer
            false, // pupbase
            false // genetic slugpups
        };

        public void OnLoad()
        {
        }

        //private bool weInitializedYet = false;
        public void OnEnable()
        {
            try
            {
                Logger.LogDebug("Ambulant's Plugin loading...");
                //On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

                On.Player.MovementUpdate += StopRollingLmao;
                On.Player.DeathByBiteMultiplier += DeathByBite;
                On.Player.Update += StopRollingLmao2;
                On.Player.UpdateAnimation += StopRollingLmao3;

                On.DartMaggot.Update += DartMaggot_Update;
                On.Leech.Update += Leech_Update;
                On.MirosBird.JawSlamShut += MirosBird_JawSlamShut;
                On.Vulture.JawSlamShut += Vulture_JawSlamShut;

                On.Menu.CharacterSelectPage.UpdateSelectedSlugcat += CharacterSelectPage_UpdateSelectedSlugcat;

                On.RainWorld.OnModsInit += LoadResources;
                On.RainWorld.PostModsInit += ShePostOnMyInit;

                //new Hook(typeof(Player).GetProperty(nameof(Player.ThrowDirection)).GetGetMethod(), GetType().GetMethod(nameof(Player_ThrowDirection)));
                //Logger.Log(LogLevel.Debug,(GetType().GetMethod(nameof(Player_ThrowDirection))==null));
                On.Weapon.Thrown += Weapon_Thrown;
                On.Weapon.Shoot += Weapon_Shoot;

                On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.HasUniqueSprite += Ambulant_Jolly_Sprite;
                On.JollyCoop.JollyMenu.JollyPlayerSelector.GetPupButtonOffName += Ambulant_Jolly_Name;
                //IL.Player.UpdateBodyMode += playerUpdateBody;

                //weInitializedYet = true;
                Logger.LogDebug("Ambulant's Plugin successfully loaded!");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try { RegisterAmbulantCommands(); }
            catch
            {
                Logger.Log(LogLevel.Info, "Ambulant's DevConsole commands didn't load. Is DevConsole installed?");
            }
        }

        private void OnDisable()
        {
            //if (restartMode)
            //{
            //    Hooks.RemoveHooks();
            //};
        }

        private void RegisterAmbulantCommands()
        {
            new CommandBuilder("amb_get_dizzy")
                .RunGame((game, args) =>
                {
                    var ooble = 0;
                    foreach (var player in game.Players.Select(ply => ply.realizedObject as Player))
                    {
                        if (args == null || args[0] == "all" || (ooble + 1).ToString() == args[0])
                        {
                            if (DizzyMechanicActive.TryGet(player, out var getdizzy) && getdizzy == true)
                            {
                                GameConsole.WriteLine("Player " + player.SlugCatClass + " has " + player.GetCustomData().dizziness + " dizziness and " + player.GetCustomData().timeUntilUndizzy + " dizzy cooldown.", Color.white);
                            }
                            else
                            {
                                GameConsole.WriteLine("Player " + player.SlugCatClass + " cannot get dizzy.", Color.white);
                            }
                        }
                        ooble++;
                    }
                })
                .AutoComplete(args =>
                {
                    if (args.Length == 0) return new string[] { "1", "2", "3", "4", "all" };
                    return null;
                })
                .Register();
            new CommandBuilder("amb_set_dizzy")
                .RunGame((game, args) =>
                {
                    var ooble = 0;
                    foreach (var player in game.Players.Select(ply => ply.realizedObject as Player))
                    {
                        if (args == null || args[0] == "all" || (ooble + 1).ToString() == args[0])
                        {
                            if (DizzyMechanicActive.TryGet(player, out var getdizzy) && getdizzy == true)
                            {
                                int booble = (args == null || args[1] == null || args[2] == null) ? 0 : Int32.Parse(args[2]);
                                if (args == null || args[1] == null || args[1] == "dizziness")
                                {
                                    player.GetCustomData().dizziness = booble;
                                }
                                if (args == null || args[1] == null || args[1] == "cooldown")
                                {
                                    player.GetCustomData().timeUntilUndizzy = booble;
                                }
                                GameConsole.WriteLine("Player " + player.SlugCatClass + " has been set to " + player.GetCustomData().dizziness + " dizziness and " + player.GetCustomData().timeUntilUndizzy + " dizzy cooldown.", Color.white);
                            }
                            else
                            {
                                GameConsole.WriteLine("Player " + player.SlugCatClass + " cannot get dizzy.", Color.white);
                            }
                        }
                        ooble++;
                    }
                })
                .AutoComplete(args =>
                {
                    if (args.Length == 0) return new string[] { "1", "2", "3", "4", "all" };
                    if (args.Length == 1) return new string[] { "dizziness", "cooldown" };
                    return null;
                })
                .Register();
            new CommandBuilder("amb_sillymode")
                .RunGame((game, args) =>
                {
                    var ooble = 0;
                    foreach (var player in game.Players.Select(ply => ply.realizedObject as Player))
                    {
                        if (args == null || args[0] == "all" || (ooble + 1).ToString() == args[0])
                        {
                            if (DizzyMechanicActive.TryGet(player, out var getdizzy) && getdizzy == true)
                            {
                                string extraMSG = "";
                                Color extraColor = Color.white;
                                if (args == null || args[1] == null || args[1] == "reset")
                                {
                                    player.GetCustomData().dizziness = 0;
                                    player.GetCustomData().timeUntilUndizzy = 0;
                                }
                                if (args != null && args[1] != null && args[1] == "alwaystired")
                                {
                                    player.GetCustomData().dizziness = 499;
                                    player.GetCustomData().timeUntilUndizzy = 29000;
                                    extraMSG = "The " + player.SlugCatClass + " is very eepy.";
                                }
                                else if (args != null && args[1] != null && args[1] == "alwaystired_danger")
                                {
                                    player.GetCustomData().dizziness = 499;
                                    player.GetCustomData().timeUntilUndizzy = 30000;
                                    extraMSG = "Good luck, " + player.SlugCatClass + ".";
                                    extraColor = Color.red;
                                }
                                GameConsole.WriteLine("Player " + player.SlugCatClass + " has been set to " + player.GetCustomData().dizziness + " dizziness and " + player.GetCustomData().timeUntilUndizzy + " dizzy cooldown.", Color.white);
                                if (extraMSG != "")
                                {
                                    GameConsole.WriteLine(extraMSG, extraColor);
                                }
                            }
                            else
                            {
                                GameConsole.WriteLine("Player " + player.SlugCatClass + " cannot get dizzy.", Color.white);
                            }
                        }
                        ooble++;
                    }
                })
                .AutoComplete(args =>
                {
                    if (args.Length == 0) return new string[] { "1", "2", "3", "4", "all" };
                    if (args.Length == 1) return new string[] { "reset", "alwaystired", "alwaystired_danger" };
                    return null;
                })
                .Register();
            new CommandBuilder("amb_dist").RunGame((game, args) =>
            {
                foreach (var player in game.Players.Select(ply => ply.realizedObject as Player))
                {
                    GameConsole.WriteLine("Player " + player.SlugCatClass + " has an distance of " + (player.bodyChunks[1].pos.y-player.bodyChunks[0].pos.y), Color.white);
                }
            }).Register();
        }

        private void DodgeEffects(Player self)
        {
            var room = self.room;
            var pos = self.mainBodyChunk.pos;
            var color = self.ShortCutColor();
            room.AddObject(new ExplosionSpikes(room, pos, 8, 29f, 7f, 7f, 170f, color));
            room.AddObject(new ShockWave(pos, 229f, 0.029f, 7, false));

            room.PlaySound(SoundID.Slugcat_Flip_Jump, self.mainBodyChunk);
            room.InGameNoise(new Noise.InGameNoise(pos, 9000f, self, 1f));
        }

        private void ThrowEffects(Player self)
        {
            if (self.GetCustomData().dizziness > 400)
            {
                self.Stun(60);
                self.GetCustomData().throwBoostCool = 160;
            }
            else
            {
                self.GetCustomData().throwBoostCool = 80;
            }
            self.GetCustomData().dizziness = Math.Min(self.GetCustomData().dizziness + 40, 500);
            self.GetCustomData().timeUntilUndizzy = Math.Max(self.GetCustomData().timeUntilUndizzy, 80);
        }

        //public static readonly ConditionalWeakTable<AbstractCreature, GeneralCWT> CWT = new();
        //public static GeneralCWT GetGeneral(this Player crit) => CWT.GetValue(crit.abstractCreature, _ => new GeneralCWT(crit.abstractCreature));

        private static void LoadResources(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);

            AmbulantConfig.RegisterOI();

            Futile.atlasManager.LoadImage("Kill_Slugcat_ambulant");
            Futile.atlasManager.LoadImage("Multiplayer_Death_ambulant");

            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSingle;
            On.HUD.HUD.InitMultiplayerHud += HUD_InitMulti;
            //if (ModManager.ActiveMods.Any(x => x.id == "Antoneeee.PupBase"))
            //{
            //    modsOn[2] = true;
                //PupBaseInit();
            //}
        }

        // the fren :3
        private bool _postModsInit;
        private void ShePostOnMyInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            if (_postModsInit) return;

            try
            {
                _postModsInit = true;
                On.Player.CanBeSwallowed += CanSwallowThis;

                if (ModManager.ActiveMods.Any(x => x.id == "willowwisp.bellyplus"))
                {
                    modsOn[0] = true;
                }
                if (ModManager.ActiveMods.Any(x => x.id == "com.rainworldgame.topicular.lancer.plugin"))
                {
                    modsOn[1] = true;
                }

                if (ModManager.ActiveMods.Any(x => x.id == "TM.PupOnHibernation"))
                {
                    modsOn[3] = true;
                    //PupBaseInitTwoElectricBoogaloo();
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private static void PupBaseInit()
        {
            //_ = PupManager.Register(new PupType(MOD_ID, AmbulantValuesNShit.CombustivePup, spawnWeight: 1) { adultModule = new PupType.AdultModule(new SlugcatStats.Name("SlugpupAdult", register: true), 1) });
        }

        private static void PupBaseInitTwoElectricBoogaloo()
        {
            string[] pupTypes = new string[12];
            for (var a = 0; a < 4; a++)
            {
                pupTypes[0+a] = "Regular";
                pupTypes[4+a] = "Variant<cC>Hunterpup<cB>";
                pupTypes[8+a] = "Variant<cC>Rotundpup<cB>";
            }
            pupTypes[11] = "Type<cC>Combustivepup<cB>Food<cC>3<cB>";
            //PupOnHibernationMain.register_many_pups("SlugcatName3", pups);
        }

        private void StopRollingLmao(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            int myDir = ((self.input[0].x == 0) ? 0 : ((self.input[0].x > 0) ? 1 : -1));

            bool[] myAbilities = ScugHasAbility(self);
            bool fakeIsTrue3 = myAbilities[14] || myAbilities[0] || myAbilities[1] || myAbilities[2]
            || myAbilities[3] || myAbilities[4] || myAbilities[5] || myAbilities[6];

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
            var isTrue7 = false;
            if (DizzyMechanicActive.TryGet(self, out var getdizzy))
            {
                if (getdizzy == true)
                {
                    isTrue7 = true;
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
            if (fakeIsTrue3)
            {
                // MARKER: Crawl Turn Roll
                if (myAbilities[14])
                {
                    self.allowRoll = 15;
                }
                if (isTrue3 && self.slideCounter >= 10 && (self.input[0].y > 0 || self.slideCounter == 10))
                {
                    // UNUSED MARKER: Dash-turn Stopper
                    self.slideDirection = myDir;
                }
                if (myAbilities[0] && self.bodyMode == Player.BodyModeIndex.Crawl && (self.slideCounter == 0 || self.slideCounter >= 10))
                {
                    // MARKER: Crawl Flip
                    self.slideCounter = 1;
                }
                if (isTrue3 && self.input[0].downDiagonal != 0)
                {
                    // UNMARKER
                    self.rollCounter = 11;
                }
                if (myAbilities[1] && self.animation == Player.AnimationIndex.BeamTip)
                {
                    // MARKER: Beam Tip Flip
                    self.slideDirection = -myDir;
                    if (self.input[0].y > 0 && myDir != 0)
                    {
                        self.slideCounter = 9;
                    }
                    else
                    {
                        self.slideCounter = 11;
                    }
                }
                if (isTrue3 && self.bodyMode != Player.BodyModeIndex.Crawl)
                {
                    // UNUSED MARKER: Flip Stopper
                    if (self.input[0].y < 0)
                    {
                        self.slideCounter = 0;
                    }
                }
                if (isTrue3 && self.animation == Player.AnimationIndex.BellySlide && self.consistentDownDiagonal > 20)
                {
                    // UNUSED MARKER: Slide Stopper
                    self.animation = Player.AnimationIndex.None;
                    self.bodyMode = Player.BodyModeIndex.Crawl;
                }
                if (myAbilities[2] && self.superLaunchJump == 20 && self.input[0].y > 0)
                {
                    // MARKER: Vertical Charge-Pounce
                    self.superLaunchJump = 0;
                    self.room.PlaySound(SoundID.Slugcat_Super_Jump, self.mainBodyChunk, false, 1f, 0.6f);
                    var mult = 2f;
                    self.bodyChunks[0].vel.y = 9f * mult;
                    self.bodyChunks[1].vel.y = 6f * mult;
                    self.bodyChunks[1].vel.x = 4f * mult * self.slideDirection;
                }
                if (myAbilities[3] && self.slowMovementStun > 10)
                {
                    // MARKER: Lower Movement Stun
                    if (myAbilities[4] && myAbilities[5])
                    {
                        self.slowMovementStun = 10;
                    }
                    else if (self.slowMovementStun > 15)
                    {
                        self.slowMovementStun = 15;
                    }
                    if (self.horizontalCorridorSlideCounter < 10 && self.verticalCorridorSlideCounter < 10)
                    {
                        if (myAbilities[4] && self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam)
                        {
                            // MARKER: Fast Pole-Climb
                            self.slowMovementStun = 0;
                            if (self.slideUpPole != 0 && self.slideUpPole < 10)
                            {
                                self.slideUpPole = 0;
                            }
                        }
                        if (myAbilities[5] && self.bodyMode == Player.BodyModeIndex.CorridorClimb)
                        {
                            // MARKER: Fast Corridor-Climb
                            self.slowMovementStun = 0;
                        }
                    }
                }
                if (myAbilities[6] && self.animation == Player.AnimationIndex.ClimbOnBeam || self.animation == Player.AnimationIndex.HangFromBeam || self.animation == Player.AnimationIndex.StandOnBeam)
                {
                    // MARKER: Funny Flip Off Of Horizontal Pole
                    self.slideDirection = myDir;
                    self.initSlideCounter = 29;
                    if (self.slideCounter == 0 || self.slideCounter > 11)
                    {
                        self.slideCounter = 11;
                    }
                }
                /*
                if (self.input[0].y>0 && (self.bodyChunks[0].vel[0]==0 || self.animation == Player.AnimationIndex.StandUp) && self.input[1].jmp)
                {
                    self.input[1].jmp = false;
                    self.animation = Player.AnimationIndex.Flip;
                    self.slideCounter = 0;
                    self.rollCounter = 1;
                    self.room.PlaySound(SoundID.Slugcat_Flip_Jump, self.mainBodyChunk, false, 1f, 1f);
                    self.rollDirection = myDir;
                    self.slideDirection = self.rollDirection;
                    self.rollCounter = 11;
                    self.bodyChunks[0].vel.y = 15f;
                    //self.bodyChunks[1].vel.y = 7f;
                    //self.bodyChunks[0].vel.x = self.bodyChunks[0].vel.x * 0.5f + 10f * (float)myDir;
                    //self.bodyChunks[1].vel.x *= 0.5f;
                    self.standing = false;
                }*/
                if (isTrue3)
                {
                    // UNUSED MARKER: Throwless Boosting
                    if (self.GetCustomData().throwBoostCool <= 0)
                    {
                        if (self.input[0].thrw && self.grasps[0] == null && self.grasps[1] == null && self.bodyMode == Player.BodyModeIndex.Default)
                        {
                            ThrowEffects(self);
                            Vector2 dirr = new Vector2(self.ThrowDirection, 0);
                            if (self.input[0].y < 0.8)
                            {
                                if (self.input[0].y > 0)
                                {
                                    dirr.x *= 0.7f;
                                    dirr.y = 0.25f;
                                }
                            }
                            else
                            {
                                dirr = new Vector2(0, 0.5f);
                            }
                            self.bodyChunks[0].vel += dirr * 8f;
                            self.bodyChunks[1].vel -= dirr * 4f;
                            self.bodyChunks[0].vel.y = (self.bodyChunks[0].vel.y + dirr.y * 8f) / 2;
                            self.bodyChunks[1].vel.y = (self.bodyChunks[1].vel.y + dirr.y * 8f) / 2;
                            self.Blink(10);
                            PlayerGraphics playgraph = (self.graphicsModule as PlayerGraphics);
                            playgraph.hands[0].vel += dirr * 8f;
                            playgraph.hands[0].mode = Limb.Mode.Dangle;
                        }
                    }
                    else
                    {
                        if (self.bodyMode != Player.BodyModeIndex.Default && self.bodyMode != Player.BodyModeIndex.Swimming && self.stun <= 0)
                        {
                            self.GetCustomData().throwBoostCool = Math.Min(20, self.GetCustomData().throwBoostCool);
                        }
                        self.GetCustomData().throwBoostCool--;
                    }
                }
                else
                {
                    self.GetCustomData().throwBoostCool = 0;
                }
            }
            if (isTrue4 && self.mushroomCounter > 40)
            {
                self.mushroomCounter = 40;
                if (isTrue7) 
                {
                    self.GetCustomData().dizziness = Math.Min(self.GetCustomData().dizziness+100, 500);
                    self.GetCustomData().timeUntilUndizzy = Math.Max(self.GetCustomData().timeUntilUndizzy, 40);
                }
            }
            orig(self, eu);
        }

        private void StopRollingLmao2(On.Player.orig_Update orig, Player self, bool eu)
        {
            var isTrue7 = false;
            if (DizzyMechanicActive.TryGet(self, out var getdizzy))
            {
                if (getdizzy == true)
                {
                    isTrue7 = true;
                }
            }
            if (ScugHasAbility(self)[7] && !self.dead)
            {
                if (self.dangerGrasp != null && self.dangerGrasp.grabber != null)
                {
                    if ((self.dangerGraspTime == 5 && UnityEngine.Random.value < 0.375) || (self.dangerGraspTime == 29 && UnityEngine.Random.value < 0.2))
                    // haha 1-((1-x)(1-y))=z go brr where x is the first random value y is the second random value and z is the desired chance
                    {
                        self.dangerGrasp.grabber.LoseAllGrasps();
                        DodgeEffects(self);
                        if (isTrue7)
                        {
                            self.stun = 0;
                            self.GetCustomData().dizziness = Math.Min(self.GetCustomData().dizziness + 60, 500);
                        }
                        else
                        {
                            self.stun = 20;
                        }
                    }
                    if (isTrue7)
                    {
                        self.GetCustomData().timeUntilUndizzy = Math.Max(self.GetCustomData().dizziness, 60);
                    }
                }
            }
            if (isTrue7)
            {
                bool woundedOrStarving = self.Wounded || self.Malnourished;
                if (self.animation == Player.AnimationIndex.Roll || self.animation == Player.AnimationIndex.Flip || self.animation == Player.AnimationIndex.RocketJump)
                {
                    if (self.GetCustomData().dizziness < 400)
                    {
                        self.GetCustomData().dizziness = Math.Min(self.GetCustomData().dizziness + 1, 500);
                        self.GetCustomData().timeUntilUndizzy = Math.Max(self.GetCustomData().timeUntilUndizzy, 40);
                    }
                    else
                    {
                        self.GetCustomData().dizziness = Math.Min(self.GetCustomData().dizziness + 50, 500);
                        self.GetCustomData().timeUntilUndizzy = Math.Max(self.GetCustomData().timeUntilUndizzy, 80);
                        self.animation = Player.AnimationIndex.None;
                        self.stopRollingCounter = 20;
                        self.Stun(40);
                    }
                }
                else if ((self.bodyChunks[1].pos.y - self.bodyChunks[0].pos.y) >= 11 && self.room.gravity != 0 && self.stun == 0 && self.GetCustomData().dizziness < 500 && self.GetCustomData().stopFuckingDying == 0)
                {
                    self.GetCustomData().dizziness = Math.Min(self.GetCustomData().dizziness + 1, 500);
                    self.GetCustomData().timeUntilUndizzy = Math.Max(self.GetCustomData().timeUntilUndizzy, 20);
                }
                else if (self.GetCustomData().stopFuckingDying > 0)
                {
                    if (self.GetCustomData().stopFuckingDying < 50) // if this number was 41, it'd be frame-perfect to escape a softlock when tired/malnourished.
                    {
                        self.stun = 0;
                    }
                    self.GetCustomData().stopFuckingDying -= 1;
                }

                if (self.GetCustomData().dizziness >= 500)
                {
                    if (self.GetCustomData().timeUntilUndizzy >= 30000)
                    {
                        self.Die();
                        DodgeEffects(self);
                        self.room.PlaySound(SoundID.Firecracker_Bang, self.bodyChunks[1].pos, 1f, 0.75f + UnityEngine.Random.value);
                    }
                    self.Stun(120);
                    self.GetCustomData().dizziness -= 1;
                    self.GetCustomData().timeUntilUndizzy = Math.Max(self.GetCustomData().timeUntilUndizzy, 200);
                    self.GetCustomData().stopFuckingDying = 170;
                }
                else if (self.stun > 0)
                {
                    if (woundedOrStarving && self.GetCustomData().dizziness <= 400)
                    {
                        self.GetCustomData().dizziness = Math.Min(self.GetCustomData().dizziness + 1, 500);
                    }
                    self.GetCustomData().timeUntilUndizzy = Math.Max(self.GetCustomData().timeUntilUndizzy, 40);
                }
                else if (self.animation == Player.AnimationIndex.GetUpOnBeam)
                {
                    self.GetCustomData().dizziness = Math.Min(self.GetCustomData().dizziness + 1, 500);
                    self.GetCustomData().timeUntilUndizzy = Math.Max(self.GetCustomData().timeUntilUndizzy, 40);
                }
                else if (woundedOrStarving && self.GetCustomData().timeUntilUndizzy < 199 && UnityEngine.Random.value < 0.001)
                {
                    self.GetCustomData().dizziness = 499;
                    self.GetCustomData().timeUntilUndizzy = Math.Max(self.GetCustomData().timeUntilUndizzy, 200);
                }

                if (self.GetCustomData().timeUntilUndizzy < 29000 && (!(woundedOrStarving && UnityEngine.Random.value < 0.25) || self.stun > 0))
                {
                    if (self.GetCustomData().timeUntilUndizzy > 0)
                    {
                        self.GetCustomData().timeUntilUndizzy -= 1;
                    }
                    else if (self.GetCustomData().dizziness > 0)
                    {
                        self.GetCustomData().dizziness -= 1;
                    }
                }
                //Logger.Log(LogLevel.Info, self.GetCustomData().dizziness+" | "+ self.GetCustomData().timeUntilUndizzy);
            }
            else
            {
                self.GetCustomData().dizziness = 0;
                self.GetCustomData().timeUntilUndizzy = 0;
            }
            orig(self, eu);
        }

        private void StopRollingLmao3(On.Player.orig_UpdateAnimation orig, Player self)
        {
            if (ScugHasAbility(self)[9])
            {
                // MARKER: Fast Corridor Turn
                if (self.bodyMode != null && self.animation != null && self.bodyMode == Player.BodyModeIndex.CorridorClimb && self.animation == Player.AnimationIndex.CorridorTurn && !self.Wounded && !self.Malnourished)
                {
                    if (ScugHasAbility(self)[10] && self.corridorTurnCounter < 20)
                    {
                        self.corridorTurnCounter = 20;
                    }
                    else if (self.corridorTurnCounter < 30 )
                    {
                        self.corridorTurnCounter = 30;
                    }
                }
            }
            orig(self);
        }
        private static string Ambulant_Jolly_Name(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_GetPupButtonOffName orig, JollyPlayerSelector self)
        {
            SlugcatStats.Name playerClass = self.JollyOptions(self.index).playerClass;
            if (playerClass != null && playerClass.value == "ambulant")
            {
                if (!self.JollyOptions(self.index).isPup)
                {
                    return "ambulant_pup_off";
                }
                else
                {
                    return "ambulant_pup_on";
                }
            }
            return orig(self);
        }

        private bool CanSwallowThis(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
        {
            if (SwallowOnlyPearls.TryGet(self, out bool swallowporl) && swallowporl == true)
            {
                return (testObj is DataPearl || testObj is Lantern);
            }
            return orig(self, testObj);
        }

        // thank u olaycolay for this code i would've never figured it out without it
        private static bool Ambulant_Jolly_Sprite(On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.orig_HasUniqueSprite orig, SymbolButtonTogglePupButton self)
        {
            // TODO: Make pup sprite
            if (self.symbolNameOff.Contains("ambulant") && !self.isToggled) return true;
            return orig(self);
        }
        private float DeathByBite(On.Player.orig_DeathByBiteMultiplier orig, Player self)
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

        // (code from nightstriker somewhat used) get rid of them MAGGOTS when rolling
        private void DartMaggot_Update(On.DartMaggot.orig_Update orig, DartMaggot self, bool eu)
        {
            if (self.mode == DartMaggot.Mode.StuckInChunk)
            {
                Player player = self.stuckInChunk.owner as Player;
                if (player != null)
                {
                    if (ScugHasAbility(player)[12] && player.animation == Player.AnimationIndex.Roll)
                    {
                        self.stuckInChunk = null;
                        self.mode = DartMaggot.Mode.Free;
                    }
                }
            }
            orig(self, eu);
        }

        private void Leech_Update(On.Leech.orig_Update orig, Leech self, bool eu)
        {
            if (self.grasps[0] != null)
            {
                Player player = self.grasps[0].grabbed as Player;
                if (player != null)
                {
                    if (ScugHasAbility(player)[11] && player.animation == Player.AnimationIndex.Roll)
                    {
                        self.Stun(40);
                    }
                }
            }
            orig(self, eu);
        }

        private void MirosBird_JawSlamShut(On.MirosBird.orig_JawSlamShut orig, MirosBird self)
        {
            bool runOrig = true;
            int num2 = 0;
            Vector2 a = RWCustom.Custom.DirVec(self.neck.Tip.pos, self.Head.pos);
            while (num2 < self.room.abstractRoom.creatures.Count && self.grasps[0] == null)
            {
                Creature realCreature = self.room.abstractRoom.creatures[num2].realizedCreature;
                if (realCreature is Player && !realCreature.dead)
                {
                    var isTrue7 = false;
                    if (DizzyMechanicActive.TryGet((Player)realCreature, out var getdizzy))
                    {
                        if (getdizzy == true)
                        {
                            isTrue7 = true;
                        }
                    }
                    if (ScugHasAbility((Player)realCreature)[8])
                    {
                        if (AmbulantConfig.stupidMirosBirds.Value)
                        {
                            runOrig = false;
                        }
                        int num3 = 0;
                        while (num3 < realCreature.bodyChunks.Length && runOrig)
                        {
                            if (RWCustom.Custom.DistLess(self.Head.pos + a * 30f, realCreature.bodyChunks[num3].pos, 30f + realCreature.bodyChunks[num3].rad))
                            {
                                if ((((Player)realCreature).GetCustomData().dizziness < 100 && isTrue7) || (((Player)realCreature).GetCustomData().dizziness < 400 && UnityEngine.Random.value < 0.8))
                                {
                                    runOrig = false;
                                    self.Stun(20);
                                    ((Player)realCreature).bodyChunks[1].vel.x += ((Player)realCreature).input[0].x * 6f;
                                    ((Player)realCreature).bodyChunks[0].vel.y = 3f;
                                    ((Player)realCreature).bodyChunks[1].vel.y = 3f;
                                    DodgeEffects((Player)realCreature);
                                    if (isTrue7)
                                    {
                                        ((Player)realCreature).GetCustomData().dizziness += 50;
                                        ((Player)realCreature).GetCustomData().timeUntilUndizzy = Math.Max(((Player)realCreature).GetCustomData().timeUntilUndizzy, 20);
                                    }
                                    Logger.LogDebug(((Player)realCreature).SlugCatClass.ToString() + " dodged a Miros Bird. They have " + ((Player)realCreature).GetCustomData().dizziness + "dizziness now.");
                                    break;
                                }
                                else
                                {
                                    Logger.LogDebug(((Player)realCreature).SlugCatClass.ToString() + " didn't dodge a Miros Bird. They have " + ((Player)realCreature).GetCustomData().dizziness + "dizziness now.");
                                    break;
                                }
                            }
                            else
                            {
                                //Logger.LogDebug(((Player)realCreature).SlugCatClass.ToString() + " is too far from Miros Bird!");
                            }
                            num3++;
                        }
                    }
                    else {
                            //Logger.LogDebug("what the fuck");
                        
                    }
                }
                num2++;
            }
            bool superStupid = self.abstractCreature.ID.number == 4152 || self.abstractCreature.ID.number == 69;
            if (runOrig && !superStupid)
            {
                orig(self);
            }
            else if (!AmbulantConfig.stupidMirosBirds.Value && !superStupid)
            {
                Vector2 aa = Custom.DirVec(self.neck.Tip.pos, self.Head.pos);
                self.room.PlaySound(SoundID.Miros_Beak_Snap_Miss, self.Head);
                self.neck.Tip.vel -= aa * 10f;
                self.neck.Tip.pos += aa * 20f;
                self.Head.pos += aa * 20f;
            }
            else
            {
                self.jawOpen = 1f;
                self.jawVel = 0f;
            }
        }

        private void Vulture_JawSlamShut(On.Vulture.orig_JawSlamShut orig, Vulture self)
        {
            bool runOrig = true;
            int num2 = 0;
            Vector2 a = RWCustom.Custom.DirVec(self.neck.Tip.pos, self.Head().pos);
            while (num2 < self.room.abstractRoom.creatures.Count && self.grasps[0] == null)
            {
                Creature realCreature = self.room.abstractRoom.creatures[num2].realizedCreature;
                if (realCreature is Player && !realCreature.dead)
                {
                    var isTrue7 = false;
                    if (DizzyMechanicActive.TryGet((Player)realCreature, out var getdizzy))
                    {
                        if (getdizzy == true)
                        {
                            isTrue7 = true;
                        }
                    }
                    if (ScugHasAbility((Player)realCreature)[8])
                    {
                        if (AmbulantConfig.stupidMirosBirds.Value)
                        {
                            runOrig = false;
                        }
                        int num3 = 0;
                        while (num3 < realCreature.bodyChunks.Length && runOrig)
                        {
                            if (RWCustom.Custom.DistLess(self.Head().pos + a * 30f, realCreature.bodyChunks[num3].pos, 30f + realCreature.bodyChunks[num3].rad))
                            {
                                if (((Player)realCreature).GetCustomData().dizziness < 400 && UnityEngine.Random.value < 0.8)
                                {
                                    runOrig = false;
                                    self.Stun(20);
                                    ((Player)realCreature).bodyChunks[1].vel.x += ((Player)realCreature).input[0].x * 6f;
                                    ((Player)realCreature).bodyChunks[0].vel.y = 3f;
                                    ((Player)realCreature).bodyChunks[1].vel.y = 3f;
                                    DodgeEffects((Player)realCreature);
                                    if (isTrue7)
                                    {
                                        ((Player)realCreature).GetCustomData().dizziness += 50;
                                        ((Player)realCreature).GetCustomData().timeUntilUndizzy = Math.Max(((Player)realCreature).GetCustomData().timeUntilUndizzy, 20);
                                    }
                                    Logger.LogDebug(((Player)realCreature).SlugCatClass.ToString() + " dodged a Miros Bird. They have " + ((Player)realCreature).GetCustomData().dizziness + "dizziness now.");
                                    break;
                                }
                                else
                                {
                                    Logger.LogDebug(((Player)realCreature).SlugCatClass.ToString() + " didn't dodge a Miros Bird. They have " + ((Player)realCreature).GetCustomData().dizziness + "dizziness now.");
                                    break;
                                }
                            }
                            else
                            {
                                //Logger.LogDebug(((Player)realCreature).SlugCatClass.ToString() + " is too far from Miros Bird!");
                            }
                            num3++;
                        }
                    }
                    else
                    {
                        //Logger.LogDebug("what the fuck");

                    }
                }
                num2++;
            }
            bool superStupid = self.abstractCreature.ID.number == 4152 || self.abstractCreature.ID.number == 69;
            if (runOrig && !superStupid)
            {
                orig(self);
            }
            else
            {
                (self.State as HealthState).health -= 0.0005f;
                if (!superStupid)
                {
                    self.FireLaser();
                }
                if (!AmbulantConfig.stupidMirosBirds.Value && !superStupid)
                {
                    Vector2 aa = Custom.DirVec(self.neck.Tip.pos, self.Head().pos);
                    self.room.PlaySound(SoundID.Miros_Beak_Snap_Miss, self.Head());
                    self.neck.Tip.vel -= aa * 10f;
                    self.neck.Tip.pos += aa * 20f;
                    self.Head().pos += aa * 20f;
                }
                else
                {
                    self.jawOpen = 1f;
                    self.jawVel = 0f;
                }
            }
        }
        private static void CharacterSelectPage_UpdateSelectedSlugcat(On.Menu.CharacterSelectPage.orig_UpdateSelectedSlugcat orig, CharacterSelectPage self, int num)
        {
            orig(self, num);

            if (ExpeditionGame.playableCharacters[num].ToString() == "ambulant")
            {
                self.slugcatScene = MSCSceneID.Landscape_VS;
            }
            else {
                BepInEx.Logging.Logger.CreateLogSource(ExpeditionGame.playableCharacters[num].ToString());
            }
        }

        // thank you forthfora for allowing me to use this code :D
        private void Weapon_Shoot(On.Weapon.orig_Shoot orig, Weapon self, Creature shotBy, Vector2 thrownPos, UnityEngine.Vector2 throwDir, float force, bool eu)
        {
            // MARKER: Throw Fixed
            orig(self, shotBy, thrownPos, throwDir, force, eu);
            if (shotBy is Player && ScugHasAbility((Player)shotBy)[13])
            {
                self.changeDirCounter = 0;
            }
        }
        private void Weapon_Thrown(On.Weapon.orig_Thrown orig, Weapon self, Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, RWCustom.IntVector2 throwDir, float frc, bool eu)
        {
            orig(self, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
            if (thrownBy is Player)
            {
                if (ScugHasAbility((Player)thrownBy)[13])
                {
                    self.changeDirCounter = 0;
                }
                if (AdditionalTech.TryGet(thrownBy as Player, out var additiontech) && additiontech == true)
                {
                    ThrowEffects((Player)thrownBy);
                }
            }
        }
        private int Player_ThrowDirection(Func<Player, int> orig, Player self)
        {
            var throwFixed3 = false;
            Logger.Log(LogLevel.Debug, "aa");
            if (ThrowFixed.TryGet(self, out var throwfixeed3))
            {
                if (throwfixeed3 == true)
                {
                    throwFixed3 = true;
                }
            }
            if (!throwFixed3)
            {
                return orig(self);
            }
            if (self.input[0].x == 0) return self.flipDirection;

            return self.input[0].x;
        }

        private static void HUD_InitSingle(On.HUD.HUD.orig_InitSinglePlayerHud orig, global::HUD.HUD self, RoomCamera camera)
        {
            orig(self, camera);
            try
            {
                bool value = true;
                if (value)
                {
                    self.AddPart(new DizzyMeter(self, self.fContainers[1]));
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
                throw e;
            }
        }

        private static void HUD_InitMulti(On.HUD.HUD.orig_InitMultiplayerHud orig, global::HUD.HUD self, global::ArenaGameSession session)
        {
            orig(self, session);
            try
            {
                bool value = true;
                if (value)
                {
                    self.AddPart(new DizzyMeter(self, self.fContainers[1]));
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
                throw e;
            }
        }
    }

    public class DizzyMeter : HudPart
    {
        public DizzyMeter(HUD.HUD hud, FContainer fContainer) : base(hud)
        {
            this.Initialize(10, 2, 60f, fContainer);
            Debug.Log("[Ambulant]  Dizzy meter added to HUD");
        }

        public void Initialize(int count, int color, float y, FContainer fContainer)
        {
            this.circles = new HUDCircle[count];
            this.pos = new Vector2(this.hud.rainWorld.options.ScreenSize.x / 2f - (float)count * 21.6f / 2f, y);
            int realColor = 1;
            for (int i = 0; i < count; i++)
            {
                if (i == 2)
                {
                    realColor = color;
                }
                this.circles[i] = new HUDCircle(this.hud, HUDCircle.SnapToGraphic.smallEmptyCircle, fContainer, realColor)
                {
                    fade = 1f,
                    lastFade = 1f,
                    snapRad = 0f,
                    snapThickness = 0f
                };
            }
        }

        public override void Update()
        {
            this.lastPos = this.pos;
            if (this.hudPlayer != null)
            {
                int myPlayerDizz = this.hudPlayer.GetCustomData().dizziness;
                float evilPlayerDizz = 500f - myPlayerDizz;
                float multiAmt = 1f - Math.Min(0.25f, this.hudPlayer.GetCustomData().timeUntilUndizzy / 160f);
                this.pos.x = this.hud.rainWorld.options.ScreenSize.x / 2f - (float)this.circles.Length * 21.6f / 2f;
                for (int i = 0; i < this.circles.Length; i++)
                {
                    this.circles[i].Update();
                    this.circles[i].rad = 6f * multiAmt;
                    this.circles[i].thickness = Math.Min(8, Math.Max(0f, evilPlayerDizz / 50f - (float)i)*8+1);
                    this.circles[i].pos = this.pos + new Vector2((float)i * 21.6f, 0f);//(float)Math.Sin((this.hudPlayer.GetCustomData().timeUntilUndizzy + this.hudPlayer.GetCustomData().dizziness) / 10 + i)
                }
            }
        }

        public Vector2 DrawPos(float timeStacker)
        {
            return Vector2.Lerp(this.lastPos, this.pos, timeStacker);
        }

        public override void Draw(float timeStacker)
        {
            for (int i = 0; i < this.circles.Length; i++)
            {
                this.circles[i].Draw(timeStacker);
            }
        }

        public HUDCircle[] circles;

        public Vector2 pos;

        public Vector2 lastPos;

        public Player hudPlayer
        {
            get
            {
                Player result;
                if (this.hud.owner as Player != null)
                {
                    result = this.hud.owner as Player;
                }
                else
                {
                    ArenaGameSession arenaGameSession = this.hud.owner as ArenaGameSession;
                    if (arenaGameSession != null)
                    {
                        result = ((arenaGameSession.game.Players.Count<AbstractCreature>() > 0) ? (arenaGameSession.game.Players[0].realizedCreature as Player) : null);
                    }
                    else
                    {
                        result = null;
                    }
                }
                return result;
            }
        }
    }

    public class AmbulantConfig : OptionInterface
    {
        public static AmbulantConfig Instance { get; } = new AmbulantConfig();

        public static void RegisterOI()
        {
            if (MachineConnector.GetRegisteredOI(Plugin.MOD_ID) != Instance)
                MachineConnector.SetRegisteredOI(Plugin.MOD_ID, Instance);
        }

        public static Configurable<bool> enabled = Instance.config.Bind("enabled", true,
            new ConfigurableInfo("Whether the mod is enabled.  Default true.")
        );

        public static Configurable<bool> stupidMirosBirds = Instance.config.Bind("stupidMirosBirds", false,
            new ConfigurableInfo("Makes Miros Birds open their mouths and stare at slugcats.\nOnly works for slugcats with the Escapist ability.")
        );

        // Called when the config menu is opened by the player.
        public override void Initialize()
        {
            base.Initialize();
            var Tab1 = new OpTab(this, "General Settings");
            var Tab2 = new OpTab(this, "Ability Settings");
            var Tab3 = new OpTab(this, "Silly Settings");
            this.Tabs = new[] {
                Tab1,
                Tab2,
                Tab3
            };

            OpContainer Tab1Cont = new OpContainer(new Vector2(0, 0));
            Tab1.AddItems(Tab1Cont);
            int theSize = 100;
            UIelement[] Elementes = new UIelement[] // Labels in a fixed box size + alignment
            {
                new OpLabelLong(new Vector2(0,theSize), new Vector2(150, 150),"OpLabels down there with set size allows for better alignment positioning but the size itself is ignored, in this case use OpLabelLong for auto-wrap and newline \n with \\n", true, FLabelAlignment.Left){verticalAlignment = OpLabel.LabelVAlignment.Bottom },
                new OpLabel(new Vector2(0,0), new Vector2(theSize, theSize),"Top L",FLabelAlignment.Left){verticalAlignment = OpLabel.LabelVAlignment.Top },
                new OpCheckBox(enabled, new Vector2(60f, 570f)) { description = enabled.info.description },
            };
            Tab1.AddItems(Elementes);

            OpContainer Tab2Cont = new OpContainer(new Vector2(0, 0));
            Tab2.AddItems(Tab2Cont);
            int theSize2 = 100;
            UIelement[] Elementes2 = new UIelement[] // Labels in a fixed box size + alignment
            {
                new OpLabelLong(new Vector2(0,theSize2), new Vector2(150, 150),"OpLabels down there with set size allows for better alignment positioning but the size itself is ignored, in this case use OpLabelLong for auto-wrap and newline \n with \\n", true, FLabelAlignment.Left){verticalAlignment = OpLabel.LabelVAlignment.Bottom },
                new OpLabel(new Vector2(0,0), new Vector2(theSize2, theSize2),"Top L",FLabelAlignment.Left){verticalAlignment = OpLabel.LabelVAlignment.Top },
            };
            Tab2.AddItems(Elementes2);

            OpContainer Tab3Cont = new OpContainer(new Vector2(0, 0));
            Tab3.AddItems(Tab3Cont);
            UIelement[] Elementes3 = new UIelement[] // Labels in a fixed box size + alignment
            {
                new OpCheckBox(stupidMirosBirds, new Vector2(60f, 300f)) { description = stupidMirosBirds.info.description },
            };
            Tab1.AddItems(Elementes3);
        }
    }
}