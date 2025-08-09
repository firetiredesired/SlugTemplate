// ARCHIVED BOUNTY SYSTEM CODE - July 23, 2025
// This file contains a full copy of the original Plugin.cs with bounty system logic for future reference.
// All code is commented out to prevent compilation errors.

// using System;
// using System.Collections.Generic;
// using BepInEx;
// using UnityEngine;
// using RWCustom;
// using IL;
// using Kittehface.Framework20;

// namespace BountyHunter
// {
//     [BepInPlugin(MOD_ID, "BountyHunter", "0.1.0")]
//     class Plugin : BaseUnityPlugin
//     {
//         private const string MOD_ID = "Firetiredesire.BountyHunter";
//         public bool canspawnspear = false; // Can spawn spear set to true when a lizard is killed
//         public bool can_maul = false; // Can maul set to true when a lizard is killed
//         public float trow_skill = 3f; // Throw skill level, can be set by killing creatures
//         public float tunnel_speed = 30f; // Tunnel speed, can be set by killing creatures
//         private string lastKilledCreatureType = null;
//         // Manual permanent throw skill buff
//         private Dictionary<Player, int> permanentThrowSkill = new Dictionary<Player, int>();
//         // Hook: When a creature dies, check if player killed it
//         private void Creature_Die(On.Creature.orig_Die orig, Creature self)
//         {
//             // Check if killed by player
//             if (!(self is Player) && self.killTag is AbstractCreature killer && killer.realizedCreature is Player player)
//             {
//                 string killedType = self.GetType().Name;
//                 lastKilledCreatureType = killedType;
//                 Debug.Log($"Essence absorbed: {killedType}");
//                 // Add your bounty logic here for each creature type
//                 if (killedType.Contains("Lizard"))
//                 {
//                    can_maul = true; // Set can maul to true when a lizard is killed
//                     //can maul set true
//                 }
//                 if (killedType == "Vulture")
//                 {
//                     Debug.Log("Vulture killed, applying jump boost.");
//                     // jump boost
//                 }
//                 if (killedType.Contains("Centipede"))
//                 {
//                     // increase tunnel speed
//                     tunnel_speed = 30f; // Example value, adjust as needed
//                 }
//                 if (killedType.Contains("Dropwig"))
//                 {
//                     Debug.Log("Dropwig killed, applying effects.");
//                     // make the player sneak better when standing still
//                 }
//                 if (killedType.Contains("Scavenger"))
//                 {
//                     Debug.Log("Scavenger killed, applying effects.");
//                     trow_skill = 30f;
//                     // Set permanent throw skill buff
//                 }
//             }
//             orig(self);
//         }

//         private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
//         {
//             // Always call the original
//             orig(self, eu);
//     
//             if (self.input[0].mp && !self.dead && canspawnspear)
//                 {
//                     if (self.room != null && self.room.abstractRoom != null)
//                     {
//                         Vector2 spearPosition = self.mainBodyChunk.pos;
//                         Vector2 spearVelocity = self.mainBodyChunk.vel;
//                         AbstractSpear abstractSpear = new AbstractSpear(
//                             self.room.world,
//                             null,
//                             self.room.GetWorldCoordinate(spearPosition),
//                             self.room.game.GetNewID(),
//                             false,
//                             false
//                         );
//                         Spear newSpear = new Spear(abstractSpear, self.room.world);
//                         newSpear.firstChunk.pos = spearPosition;
//                         newSpear.firstChunk.vel = spearVelocity;
//                         self.room.AddObject(newSpear);
//                         Debug.Log("Spear created at player's position.");
//                     }
//                     else
//                     {
//                         Debug.LogWarning("Player is not in a valid room to create a spear.");
//                     }
//                 }
//         }

//         private void OnEnable()
//         {
//             On.Player.ctor += (orig, self, abstraction, world) =>
//             {
//                 orig(self, abstraction, world);
//                 Debug.Log("Custom Slugcat initialized.");
//             };
//            
//             On.Player.Update += Player_Update;
//             On.Creature.Die += Creature_Die;
//         }

//         private void OnDisable()
//         {
//            
//             On.Player.Update -= Player_Update;
//             On.Creature.Die -= Creature_Die;
//         }
//     }
// }
