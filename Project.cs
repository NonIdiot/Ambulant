using BepInEx;
using UnityEngine;
using Expedition;
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
using UnityEngine.Rendering;
using SceneID = Menu.MenuScene.SceneID;
using MSCSceneID = MoreSlugcats.MoreSlugcatsEnums.MenuSceneID;
using System.Runtime.CompilerServices;
using MonoMod.RuntimeDetour;
using Unity.Mathematics;
using System.Runtime.InteropServices.ComTypes;
using HUD;

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

        public static bool[] modsOn = {
            false, // rotund world
            false // lancer
        };

        private bool weInitializedYet = false;
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

                weInitializedYet = true;
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

        public void DodgeEffects(Player self)
        {
            var room = self.room;
            var pos = self.mainBodyChunk.pos;
            var color = self.ShortCutColor();
            room.AddObject(new ExplosionSpikes(room, pos, 8, 29f, 7f, 7f, 170f, color));
            room.AddObject(new ShockWave(pos, 229f, 0.029f, 7, false));

            room.PlaySound(SoundID.Slugcat_Flip_Jump, self.mainBodyChunk);
            room.InGameNoise(new Noise.InGameNoise(pos, 9000f, self, 1f));
        }

        //public static readonly ConditionalWeakTable<AbstractCreature, GeneralCWT> CWT = new();
        //public static GeneralCWT GetGeneral(this Player crit) => CWT.GetValue(crit.abstractCreature, _ => new GeneralCWT(crit.abstractCreature));

        private static void LoadResources(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);

            Futile.atlasManager.LoadImage("Kill_Slugcat_ambulant");
            Futile.atlasManager.LoadImage("Multiplayer_Death_ambulant");

            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSingle;
            On.HUD.HUD.InitMultiplayerHud += HUD_InitMulti;
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

            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void StopRollingLmao(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            int myDir = ((self.input[0].x == 0) ? 0 : ((self.input[0].x > 0) ? 1 : -1));

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
            if (isTrue3)
            {
                self.allowRoll = 15;
                if (self.slideCounter >= 10)
                {
                    self.slideDirection = myDir;
                }
                if (self.bodyMode == Player.BodyModeIndex.Crawl && (self.slideCounter == 0 || self.slideCounter >= 10))
                {
                    self.slideCounter = 1;
                }
                if (self.input[0].downDiagonal != 0)
                {
                    self.rollCounter = 11;
                }
                if (self.animation == Player.AnimationIndex.BeamTip)
                {
                    if (self.input[0].y > 0 && myDir != 0)
                    {
                        self.slideDirection = -myDir;
                        self.slideCounter = 9;
                    }
                    else
                    {
                        self.slideDirection = 0;
                        self.slideCounter = 0;
                    }
                }
                if (self.input[0].y < 0 && self.bodyMode != Player.BodyModeIndex.Crawl)
                {
                    self.slideCounter = 0;
                }
                if (self.animation == Player.AnimationIndex.BellySlide && self.consistentDownDiagonal > 20)
                {
                    self.animation = Player.AnimationIndex.None;
                    self.bodyMode = Player.BodyModeIndex.Crawl;
                }
                if (self.superLaunchJump == 20 && self.input[0].y > 0)
                {
                    self.superLaunchJump = 0;
                    self.room.PlaySound(SoundID.Slugcat_Super_Jump, self.mainBodyChunk, false, 1f, 0.6f);
                    var mult = 2f;
                    self.bodyChunks[0].vel.y = 9f * mult;
                    self.bodyChunks[1].vel.y = 6f * mult;
                    self.bodyChunks[1].vel.x = 4f * mult * self.slideDirection;
                }
                if (self.slowMovementStun > 10)
                {
                    self.slowMovementStun = 10;
                    if ((self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam || self.bodyMode == Player.BodyModeIndex.CorridorClimb) && self.horizontalCorridorSlideCounter < 10 && self.verticalCorridorSlideCounter < 10)
                    {
                        self.slowMovementStun = 0;
                    }
                }
                if (self.animation == Player.AnimationIndex.ClimbOnBeam || self.animation == Player.AnimationIndex.HangFromBeam || self.animation == Player.AnimationIndex.StandOnBeam)
                {
                    self.slideDirection = myDir;
                    self.initSlideCounter = 29;
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
            if (isTrue7)
            {
                if (self.animation == Player.AnimationIndex.Roll) 
                {
                    if (self.GetCustomData().dizziness < 400)
                    {
                        self.GetCustomData().dizziness = Math.Min(self.GetCustomData().dizziness + 1, 500);
                    }
                    else
                    {
                        self.GetCustomData().dizziness = Math.Min(self.GetCustomData().dizziness + 50, 500);
                        self.animation = Player.AnimationIndex.None;
                        self.stopRollingCounter = 20;
                    }
                    self.GetCustomData().timeUntilUndizzy = Math.Max(self.GetCustomData().timeUntilUndizzy, 40);
                }

                if (self.GetCustomData().dizziness >= 500)
                {
                    self.Stun(120);
                    self.GetCustomData().dizziness -= 1;
                    self.GetCustomData().timeUntilUndizzy = Math.Max(self.GetCustomData().timeUntilUndizzy, 200);
                }
                else if (self.stun > 0)
                {
                    self.GetCustomData().timeUntilUndizzy = Math.Max(self.GetCustomData().timeUntilUndizzy, 40);
                }

                if (self.GetCustomData().timeUntilUndizzy > 0)
                {
                    self.GetCustomData().timeUntilUndizzy -= 1; 
                }
                else if (self.GetCustomData().dizziness > 0)
                {
                    self.GetCustomData().dizziness -= 1;
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
            var isTrue7 = false;
            if (DizzyMechanicActive.TryGet(self, out var getdizzy))
            {
                if (getdizzy == true)
                {
                    isTrue7 = true;
                }
            }
            if (isTrue5 && !self.dead)
            {
                if (self.dangerGrasp != null && self.dangerGrasp.grabber != null)
                {
                    if ((self.dangerGraspTime == 5 && UnityEngine.Random.value < 0.375) || (self.dangerGraspTime == 29 && UnityEngine.Random.value < 0.2))
                    // haha 1-((1-x)(1-y))=z go brr where x is the first random value y is the second random value and z is the desired chance
                    {
                        self.dangerGrasp.grabber.LoseAllGrasps();
                        DodgeEffects(self);
                        self.stun = 0;
                        if (isTrue7)
                        {
                            self.GetCustomData().dizziness = Math.Min(self.GetCustomData().dizziness + 60, 500);
                        }
                    }
                    if (isTrue7)
                    {
                        self.GetCustomData().timeUntilUndizzy = Math.Max(self.GetCustomData().dizziness, 60);
                    }
                }
            }
            orig(self, eu);
        }

        private void StopRollingLmao3(On.Player.orig_UpdateAnimation orig, Player self)
        {
            var isTrue3 = false;
            if (AdditionalTech.TryGet(self, out var additiontech))
            {
                if (additiontech == true)
                {
                    isTrue3 = true;
                }
            }
            if (isTrue3)
            {
                if (self.bodyMode != null && self.animation != null && self.bodyMode == Player.BodyModeIndex.CorridorClimb && self.animation == Player.AnimationIndex.CorridorTurn && self.corridorTurnCounter < 30)
                {
                    self.corridorTurnCounter = 30;
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
                    var isTrue6 = false;
                    if (Escapist.TryGet(player, out var escape))
                    {
                        if (escape == true)
                        {
                            isTrue6 = true;
                        }
                    }
                    if (isTrue6 && player.animation == Player.AnimationIndex.Roll)
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
                    var isTrue6 = false;
                    if (Escapist.TryGet(player, out var escape))
                    {
                        if (escape == true)
                        {
                            isTrue6 = true;
                        }
                    }
                    if (isTrue6 && player.animation == Player.AnimationIndex.Roll)
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
                    var isTrue5 = false;
                    if (Escapist.TryGet((Player)realCreature, out var escape))
                    {
                        if (escape == true)
                        {
                            isTrue5 = true;
                        }
                    }
                    var isTrue7 = false;
                    if (DizzyMechanicActive.TryGet((Player)realCreature, out var getdizzy))
                    {
                        if (getdizzy == true)
                        {
                            isTrue7 = true;
                        }
                    }
                    if (isTrue5)
                    {
                        int num3 = 0;
                        while (num3 < realCreature.bodyChunks.Length)
                        {
                            if (RWCustom.Custom.DistLess(self.Head.pos + a * 30f, realCreature.bodyChunks[num3].pos, 30f + realCreature.bodyChunks[num3].rad))
                            {
                                if (((Player)realCreature).GetCustomData().dizziness < 400)// && UnityEngine.Random.value < 2
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
                                Logger.LogDebug(((Player)realCreature).SlugCatClass.ToString() + " is too far from Miros Bird!");
                            }
                            num3++;
                        }
                    }
                    else {
                            Logger.LogDebug("what the fuck");
                        
                    }
                }
                num2++;
            }
            if (runOrig)
            {
                orig(self);
            }
            else
            {
                self.room.PlaySound(SoundID.Miros_Beak_Snap_Miss, self.Head);
                self.neck.Tip.vel -= a * 10f;
                self.neck.Tip.pos += a * 20f;
                self.Head.pos += a * 20f;
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
            orig(self, shotBy, thrownPos, throwDir, force, eu);
            var throwFixed1 = false;
            if (shotBy is Player && ThrowFixed.TryGet((Player)shotBy, out var throwfixeed1))
            {
                if (throwfixeed1 == true)
                {
                    throwFixed1 = true;
                }
            }
            if (throwFixed1)
            {
                self.changeDirCounter = 0;
            }
        }
        private void Weapon_Thrown(On.Weapon.orig_Thrown orig, Weapon self, Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, RWCustom.IntVector2 throwDir, float frc, bool eu)
        {
            orig(self, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
            var throwFixed2 = false;
            if (thrownBy is Player && ThrowFixed.TryGet((Player)thrownBy, out var throwfixeed2))
            {
                if (throwfixeed2 == true)
                {
                    throwFixed2 = true;
                }
            }
            if (throwFixed2)
            {
                self.changeDirCounter = 0;
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
            for (int i = 0; i < count; i++)
            {
                this.circles[i] = new HUDCircle(this.hud, HUDCircle.SnapToGraphic.smallEmptyCircle, fContainer, color)
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
                this.pos.x = this.hud.rainWorld.options.ScreenSize.x / 2f - (float)this.circles.Length * 21.6f / 2f;
                for (int i = 0; i < this.circles.Length; i++)
                {
                    this.circles[i].Update();
                    this.circles[i].rad = 6f;
                    this.circles[i].thickness = Math.Min(1, Math.Max(0, myPlayerDizz / 40 - i));
                    this.circles[i].pos = this.pos + new Vector2((float)i * 21.6f, 0f);
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
}