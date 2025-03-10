using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;

namespace TheClimb
{
    [BepInPlugin(MOD_ID, "TheClimb", "0.1.0")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "Firetiredesire.TheClimb";

        // Feature Configuration
        public static readonly PlayerFeature<float> SuperJump = PlayerFloat("theclimb/super_jump");
        public static readonly PlayerFeature<bool> ExplodeOnDeath = PlayerBool("theclimb/explode_on_death");
        public static readonly GameFeature<float> MeanLizards = GameFloat("theclimb/mean_lizards");
        public static readonly PlayerFeature<bool> PoisonOnBite = PlayerBool("theclimb/poison_on_bite");

        // Feature Implementation
        private bool Creature_Grab(On.Creature.orig_Grab orig, Creature self, PhysicalObject obj,
            int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability,
            float dominance, bool overrideEquallyDominant, bool pacifying)
        {
            bool result = orig(self, obj, graspUsed, chunkGrabbed, shareability, dominance,
                              overrideEquallyDominant, pacifying);

            if (obj is Player player && PoisonOnBite.TryGet(player, out bool poisonEnabled) && poisonEnabled)
            {
                Debug.Log($"Creature {self.Template.type} attempted to bite player!");
                PoisonCreature(self);
                Debug.Log($"Poison feature status: Enabled={poisonEnabled}");
            }

            return result;
        }

       private void PoisonCreature(Creature creature)
{
    // Apply poison effect with visual feedback
   
    creature.stun = Math.Max(creature.stun, 200);
   
    creature.room?.InGameNoise(new Noise.InGameNoise(
        creature.mainBodyChunk.pos,
        5000f,
        creature,
        1f));
    creature.room?.ScreenMovement(creature.mainBodyChunk.pos, default, 0.8f);
    
    // Add 1 second delay before continuing
  
}

        private void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            orig(self);
            if (SuperJump.TryGet(self, out float jumpMultiplier))
            {
                self.jumpBoost *= jumpMultiplier;
                self.mainBodyChunk.vel.y += 8f * jumpMultiplier;
                
                
            }
        }

        private void Player_Die(On.Player.orig_Die orig, Player self)
        {
            bool wasDead = self.dead;
            orig(self);
            
            if (!wasDead && self.dead && ExplodeOnDeath.TryGet(self, out bool explode) && explode)
            {
                var room = self.room;
                var pos = self.mainBodyChunk.pos;
                var color = self.ShortCutColor();
                
                // Spawn 10 grenades in a circle
                float angleStep = 360f / 10f;
                for (int i = 0; i < 10; i++)
                {
                    float angle = i * angleStep;
                    Vector2 spawnPos = pos + new Vector2(
                        Mathf.Cos(angle * Mathf.Deg2Rad) * 2f,
                        Mathf.Sin(angle * Mathf.Deg2Rad) * 2f
                    );
                    // Instantiate grenade behavior here
                }
                
                room.ScreenMovement(pos, default, 1.3f);
                room.PlaySound(SoundID.Bomb_Explode, pos);
                room.InGameNoise(new Noise.InGameNoise(pos, 9000f, self, 1f));
            }
        }

        private void Player_JollyUpdate(On.Player.orig_JollyUpdate orig, Player self, bool something)
        {
            orig(self, something);
            // Call Die() every frame for proof-of-concept
        }

        public void OnEnable()
        {
            // Player initialization
            On.Player.ctor += (orig, self, abstractCreature, world) =>
            {
                orig(self, abstractCreature, world);
                
                Debug.Log("Custom Slugcat is now a pup!");
            };

            // Feature hooks
            On.Creature.Grab += Creature_Grab;
            On.Player.Jump += Player_Jump;
            On.Player.Die += Player_Die;
            On.Player.JollyUpdate += Player_JollyUpdate;
        }

        private void LoadResources(RainWorld rainWorld)
        {
            // Load any resources (sprites, sounds, etc.)
        }
    }
}