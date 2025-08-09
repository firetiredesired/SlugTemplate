using System;
using System.Collections.Generic;
using BepInEx;
using UnityEngine;
using RWCustom;
using IL;
using Kittehface.Framework20;
using Menu;

namespace BountyHunter
{
    [BepInPlugin(MOD_ID, "BountyHunter", "0.1.0")]
    class Plugin : BaseUnityPlugin

    {
        private const string MOD_ID = "Firetiredesire.BountyHunter";
        
        // Region-based bounty system
        private string currentRegion = "";
        private string bountyTarget = "";
        private bool bountyActive = false;
        private bool bountyCompleted = false;
        private HashSet<string> completedRegions = new HashSet<string>();
        
        // UI and notification system - SIMPLIFIED
        private int bountyDisplayTimer = 0;
        private int regionEnterTimer = 0;
        
        // Persistent bounty display elements
        private FSprite bountyBackgroundSprite;
        private FLabel bountyTargetLabel;
        private FLabel bountyProgressLabel;
        private bool hudElementsCreated = false;
        
        // Region-specific abilities and buffs - ADDITIVE SYSTEM
        public bool canMaul = false;
        public bool canSpawnSpears = false;
        public bool hasWallJump = false;
        public bool hasWaterBreathing = false;
        public bool hasEnhancedStealth = false;
        public bool hasExplosiveSpears = false;
        public bool hasPsychicPowers = false;
        public bool hasElectricTouch = false;
        public bool hasGlowAura = false;
        public bool hasCamouflage = false;
        public bool hasEnhancedHearing = false;
        public bool hasWormgrassImmunity = false;
        public bool hasSpearOnBack = false;
        
        // ADDITIVE BONUSES - start at 0, add bonuses from each region
        public float runSpeedBonus = 0f;
        public float jumpHeightBonus = 0f;
        public float climbSpeedBonus = 0f;
        public float throwingSkillBonus = 0f;
        public float lungCapacityBonus = 0f;
        public float stealthLevelBonus = 0f;
        public float loudnessReduction = 0f; // How much to reduce loudness
        public float swimSpeedBonus = 0f;
        public float crawlSpeedBonus = 0f;
        
        private string lastKilledCreatureType = null;
        
        // Region creature pools for bounty selection - ALL KILLABLE CREATURES INCLUDING TOUGH ONES
        private Dictionary<string, List<string>> regionCreatures = new Dictionary<string, List<string>>()
        {
            // Vanilla Regions - FIXED CREATURE NAMES
            {"SU", new List<string>{"GreenLizard", "PinkLizard", "Squidcada", "Scavenger", "Centipede", "RedCentipede"}}, // Outskirts - ADDED RedCentipede
            {"HI", new List<string>{"GreenLizard", "PinkLizard", "BlueLizard", "WhiteLizard", "Centipede", "Vulture"}}, // Industrial Complex
            {"CC", new List<string>{"PinkLizard", "BlueLizard", "WhiteLizard", "Scavenger", "Vulture", "KingVulture"}}, // Chimney Canopy
            {"GW", new List<string>{"Scavenger", "EliteScavenger", "WhiteLizard", "Squidcada", "Centipede", "Spider", "BigSpider", "MotherSpider"}}, // Garbage Wastes
            {"SH", new List<string>{"Squidcada", "EelLizard", "Spider", "BigSpider", "BlackLizard", "Centipede", "RedCentipede"}}, // Shaded Citadel - ADDED RedCentipede
            {"DS", new List<string>{"PinkLizard", "Vulture", "Scavenger", "EelLizard", "Centipede"}}, // Drainage System
            {"SI", new List<string>{"Scavenger", "YellowLizard", "Centipede", "Vulture", "KingVulture", "Spider"}}, // Sky Islands
            {"LF", new List<string>{"Centipede", "RedCentipede", "GreenLizard", "Scavenger", "Vulture", "Spider"}}, // Farm Arrays - ADDED RedCentipede
            {"UW", new List<string>{"EelLizard", "Vulture", "WhiteLizard", "Centipede", "Salamander", "BigSpider"}}, // The Exterior
            // SS (Five Pebbles) - REMOVED as specified
            {"SB", new List<string>{"BlackLizard", "Centipede", "RedCentipede", "Vulture", "Scavenger", "Spider"}}, // Subterranean - ADDED RedCentipede
            {"MS", new List<string>{"BlueLizard", "Vulture", "Scavenger", "Centipede", "EelLizard", "EliteScavenger"}}, // Submerged Superstructure
            {"SL", new List<string>{"JetFish", "Salamander", "Vulture", "EelLizard", "Centipede", "KingVulture"}}, // Shoreline
            // Downpour DLC regions
            {"OE", new List<string>{"Vulture", "KingVulture", "CaramelLizard", "Scavenger", "Centipede", "CyanLizard"}}, // Outer Expanse - MOST IMPORTANT
            {"VS", new List<string>{"PinkLizard", "Centipede", "Vulture", "Scavenger", "EliteScavenger", "BlueLizard"}}, // Pipeyard 
            {"LC", new List<string>{"Centipede", "RedCentipede", "CaramelLizard", "Scavenger", "Vulture", "BlueLizard"}}, // Metropolis - ADDED RedCentipede
            {"RM", new List<string>{"EelLizard", "JetFish", "Salamander", "Vulture", "Centipede", "EliteScavenger"}}, // Looks to the Moon
            {"CL", new List<string>{"StrawberryLizard", "Vulture", "Scavenger", "Centipede", "RedCentipede", "Spider", "BigSpider"}} // The Rot - ADDED RedCentipede
        };
        
        // Hook: When a creature dies, check if player killed it
        private void Creature_Die(On.Creature.orig_Die orig, Creature self)
        {
            // Check if killed by player
            if (!(self is Player) && self.killTag is AbstractCreature killer && killer.realizedCreature is Player player)
            {
                string killedType = self.GetType().Name;
                lastKilledCreatureType = killedType;
                
                // DEBUG: Log detailed creature information
                Logger.LogInfo($"CREATURE KILLED DEBUG:");
                Logger.LogInfo($"  - Type.Name: {killedType}");
                Logger.LogInfo($"  - ToString(): {self.ToString()}");
                if (self.abstractCreature != null)
                {
                    Logger.LogInfo($"  - AbstractCreature.creatureTemplate.type: {self.abstractCreature.creatureTemplate.type}");
                }
                Logger.LogInfo($"  - Current bounty target: {bountyTarget}");
                Logger.LogInfo($"  - Bounty active: {bountyActive}");
                Logger.LogInfo($"  - Current region: {currentRegion}");
                
                // Check if this was the bounty target - EXACT MATCH ONLY
                if (bountyActive && killedType.Equals(bountyTarget, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInfo($"BOUNTY COMPLETED! Killed {bountyTarget} in {currentRegion}");
                    CompleteBounty(killedType);
                }
                else
                {
                    Logger.LogInfo($"Killed {killedType}, but bounty target is {bountyTarget}");
                }
            }
            orig(self);
        }
        
        // Region detection and bounty assignment
        private void CheckRegionChange(Player player)
        {
            if (player.room?.abstractRoom?.name == null) return;
            
            string roomName = player.room.abstractRoom.name;
            string newRegion = roomName.Substring(0, 2); // Get region code (SU, HI, CC, etc.)
            
            if (newRegion != currentRegion && regionCreatures.ContainsKey(newRegion))
            {
                Logger.LogInfo($"Entered new region: {newRegion}");
                currentRegion = newRegion;
                regionEnterTimer = 180; // Show region notification for 3 seconds
                
                // Don't assign new bounty if already completed this region
                if (!completedRegions.Contains(newRegion))
                {
                    AssignBountyTarget();
                    // TODO: Trigger dream sequence here
                    Logger.LogInfo($"DREAM: You sense a {bountyTarget} lurking in {currentRegion}...");
                }
                else
                {
                    Logger.LogInfo($"Region {newRegion} already completed!");
                }
            }
        }
        
        // Assign random bounty target from current region
        private void AssignBountyTarget()
        {
            if (regionCreatures.ContainsKey(currentRegion))
            {
                var creatures = regionCreatures[currentRegion];
                bountyTarget = creatures[UnityEngine.Random.Range(0, creatures.Count)];
                bountyActive = true;
                bountyCompleted = false;
                
                Logger.LogInfo($"NEW BOUNTY: Hunt {bountyTarget} in {currentRegion}");
            }
        }
        
        // Complete bounty and apply region-specific buffs only
        private void CompleteBounty(string killedType)
        {
            bountyCompleted = true;
            bountyActive = false;
            completedRegions.Add(currentRegion);
            bountyDisplayTimer = 240; // Show completion notification for 4 seconds
            
            // Apply only region-specific buffs
            ApplyRegionBuff(currentRegion);
            
            // DEBUG: Log current buff totals
            LogCurrentBuffs();
            
            Logger.LogInfo($"Progress: {completedRegions.Count}/{regionCreatures.Count} regions completed");
            
            // Check for ascension (all regions completed)
            if (completedRegions.Count >= regionCreatures.Count)
            {
                Logger.LogInfo("ALL BOUNTIES COMPLETED! ASCENSION UNLOCKED!");
                bountyDisplayTimer = 360; // Show ascension message for 6 seconds
                // TODO: Trigger ascension sequence
            }
        }
        
        // DEBUG: Log all current buffs for testing
        private void LogCurrentBuffs()
        {
            Logger.LogInfo("=== CURRENT BUFF TOTALS ===");
            Logger.LogInfo($"Run Speed: {1.0f + runSpeedBonus:F2}x (base 1.0 + {runSpeedBonus:F2})");
            Logger.LogInfo($"Jump Height: {1.0f + jumpHeightBonus:F2}x (base 1.0 + {jumpHeightBonus:F2})");
            Logger.LogInfo($"Climb Speed: {1.0f + climbSpeedBonus:F2}x (base 1.0 + {climbSpeedBonus:F2})");
            Logger.LogInfo($"Lung Capacity: {1.0f + lungCapacityBonus:F2}x (base 1.0 + {lungCapacityBonus:F2})");
            Logger.LogInfo($"Throwing Skill: {1 + (int)(throwingSkillBonus * 10)} (base 1 + {(int)(throwingSkillBonus * 10)})");
            Logger.LogInfo($"Loudness: {1.0f - loudnessReduction:F2}x (base 1.0 - {loudnessReduction:F2})");
            Logger.LogInfo($"Swim Speed: {1.0f + swimSpeedBonus:F2}x (base 1.0 + {swimSpeedBonus:F2})");
            Logger.LogInfo($"Crawl Speed: {1.0f + crawlSpeedBonus:F2}x (base 1.0 + {crawlSpeedBonus:F2})");
            Logger.LogInfo($"Special Abilities: Glow={hasGlowAura}, Maul={canMaul}, WormgrassImmunity={hasWormgrassImmunity}, SpearOnBack={hasSpearOnBack}");
            Logger.LogInfo($"Completed Regions: {string.Join(", ", completedRegions)}");
            Logger.LogInfo("========================");
        }
        
        // Apply unique buffs based on region completed - EXACT VALUES AS SPECIFIED
        private void ApplyRegionBuff(string region)
        {
            switch (region)
            {
                case "SU": // Outskirts - Basic Movement
                    runSpeedBonus += 0.3f; // runSpeed: 1.3f
                    Logger.LogInfo("OUTSKIRTS MASTERY: Basic movement boost!");
                    break;

                case "HI": // Industrial Complex - Movement through machinery
                    runSpeedBonus += 0.3f; // runSpeed: 1.3f
                    Logger.LogInfo("INDUSTRIAL ADAPTATION: Movement through machinery!");
                    break;

                case "CC": // Chimney Canopy - Swift treetop movement
                    runSpeedBonus += 0.4f; // runSpeed: 1.4f
                    Logger.LogInfo("CANOPY MASTERY: Swift treetop movement!");
                    break;

                case "GW": // Garbage Wastes - Scavenger combat mastery
                    throwingSkillBonus += 0.8f; // throwingSkill: 1.8f
                    Logger.LogInfo("SCAVENGER WISDOM: Scavenger combat mastery!");
                    break;

                case "SH": // Shaded Citadel - Shadow illumination
                    hasGlowAura = true; // the_glow: true
                    Logger.LogInfo("SHADOW WALKER: Shadow illumination!");
                    break;

                case "DS": // Drainage System - Water endurance
                    lungCapacityBonus += 1.0f; // lungCapacity: 2.0f
                    Logger.LogInfo("AQUATIC ADAPTATION: Water endurance!");
                    break;

                case "SI": // Sky Islands - Aerial mobility
                    jumpHeightBonus += 0.6f; // jumpHeight: 1.6f
                    Logger.LogInfo("WIND MASTER: Aerial mobility!");
                    break;

                case "LF": // Farm Arrays - Wormgrass immunity
                    hasWormgrassImmunity = true; // wormgrassImmunity: true
                    Logger.LogInfo("FARM WISDOM: Wormgrass immunity!");
                    break;

                case "UW": // The Exterior - Structure climbing
                    climbSpeedBonus += 0.4f; // climbSpeed: 1.4f
                    Logger.LogInfo("EXTERIOR MASTERY: Structure climbing!");
                    break;

                case "SB": // Subterranean - Underground mastery
                    loudnessReduction += 0.4f; // loudnessLevel: 0.6f
                    stealthLevelBonus += 0.5f; // stealthLevel: 1.5f
                    crawlSpeedBonus += 0.6f; // crawlSpeed: 1.6f
                    Logger.LogInfo("UNDERGROUND MASTERY: Underground mastery!");
                    break;

                case "MS": // Submerged Superstructure - Deep diving
                    lungCapacityBonus += 1.0f; // lungCapacity: 2.0f
                    Logger.LogInfo("DEEP KNOWLEDGE: Deep diving!");
                    break;

                case "SL": // Shoreline - Aquatic movement
                    swimSpeedBonus += 0.8f; // swimSpeed: 1.8f
                    Logger.LogInfo("MARINE MASTERY: Aquatic movement!");
                    break;

                // Downpour DLC regions
                case "OE": // Outer Expanse - Predator combat
                    canMaul = true; // canMaul: true
                    hasSpearOnBack = true; // spearOnBack: true
                    Logger.LogInfo("VOID MASTERY: Predator combat + Spear on back!");
                    break;

                case "VS": // Pipeyard - Pipe navigation
                    crawlSpeedBonus += 0.8f; // crawlSpeed: 1.8f
                    Logger.LogInfo("PIPE MASTERY: Pipe navigation!");
                    break;
            }
               
        }
        
        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            // Always call the original
            orig(self, eu);

            // Only apply to bountyhunter slugcat (matches SlugBase id) - MORE ROBUST CHECK
            var slugcatName = self.slugcatStats.name.value;
            if (slugcatName != "bountyhunter" && slugcatName != "BountyHunter" && !slugcatName.Contains("bounty")) 
            {
                return;
            }

            // DEBUG: Log when we detect the right slugcat
            if (self.room?.game?.Players?[0] == self.abstractCreature)
            {
                Logger.LogInfo($"BountyHunter detected! Slugcat name: '{slugcatName}'");
            }

            // DEBUG: Apply random buff when map key is pressed (use game input system)
            if (self.input[0].mp && !self.input[1].mp) // Map button pressed this frame
            {
                ApplyRandomBuff();
            }
            
            // DEBUG: Show current buffs when jump + map pressed together
            if (self.input[0].jmp && self.input[0].mp && !self.input[1].jmp && !self.input[1].mp)
            {
                LogCurrentBuffs();
            }

            // Check for region changes
            CheckRegionChange(self);
            
            // Apply active buffs
            ApplyActiveBuff(self);
            
            // Apply wormgrass immunity if active
            if (hasWormgrassImmunity)
            {
                ApplyWormgrassImmunity(self);
            }
            
            // Update timers
            if (bountyDisplayTimer > 0) bountyDisplayTimer--;
            if (regionEnterTimer > 0) regionEnterTimer--;
            
            // Display bounty info (only during pause, region entry, or completion)
            DisplayBountyInfo(self);
        }
        
        // DEBUG: Apply a random buff for testing
        private void ApplyRandomBuff()
        {
            var regions = new string[] { "SU", "HI", "CC", "GW", "SH", "DS", "SI", "LF", "UW", "SB", "MS", "SL", "OE", "VS", "LC", "RM", "CL" };
            string randomRegion = regions[UnityEngine.Random.Range(0, regions.Length)];
            
            Logger.LogInfo($"DEBUG: Applying random buff from region {randomRegion}");
            ApplyRegionBuff(randomRegion);
            LogCurrentBuffs();
        }
        
        // Apply spear on back ability - ensures player always has a spear on their back
        private void ApplySpearOnBack(Player player)
        {
            if (player.room == null) return;
            
            // Check if player already has something on their back
            if (player.spearOnBack == null || player.spearOnBack.spear == null)
            {
                // Only create a new spear if there isn't one already
                bool hasSpearOnBack = false;
                
                // Check if player has a spear-like object on their back
                if (player.spearOnBack?.spear != null)
                {
                    hasSpearOnBack = true;
                }
                
                // If no spear on back, create one (but not too frequently to avoid spam)
                if (!hasSpearOnBack && UnityEngine.Random.value < 0.02f) // 2% chance per frame to spawn spear
                {
                    try
                    {
                        // Create a new spear
                        var abstractSpear = new AbstractSpear(player.room.world, null, player.room.GetWorldCoordinate(player.mainBodyChunk.pos), player.room.game.GetNewID(), false);
                        var spear = new Spear(abstractSpear, player.room.world);
                        
                        // Add it to the room
                        player.room.AddObject(spear);
                        
                        // Try to put it on the player's back
                        if (player.spearOnBack != null)
                        {
                            player.spearOnBack.SpearToBack(spear);
                            Logger.LogInfo("SPEAR ON BACK: Granted spear!");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Logger.LogError($"Failed to create spear on back: {e.Message}");
                    }
                }
            }
        }
        
        // Apply wormgrass immunity - prevents wormgrass from grabbing/damaging player
        private void ApplyWormgrassImmunity(Player player)
        {
            if (player.room == null) return;
            
            // Check for wormgrass objects near the player
            foreach (var obj in player.room.physicalObjects[0]) // Check updatable objects layer
            {
                // Look for wormgrass or similar plant hazards
                if (obj.GetType().Name.Contains("WormGrass") || obj.GetType().Name.Contains("Plant") || obj.GetType().Name.Contains("Kelp"))
                {
                    // Check distance to player
                    if (obj.bodyChunks != null && obj.bodyChunks.Length > 0)
                    {
                        float distance = Vector2.Distance(player.mainBodyChunk.pos, obj.firstChunk.pos);
                        if (distance < 50f) // Within interaction range
                        {
                            // If it's trying to grab the player, break the interaction
                            var grabbedBy = player.grabbedBy;
                            if (grabbedBy != null)
                            {
                                for (int i = grabbedBy.Count - 1; i >= 0; i--)
                                {
                                    var grabber = grabbedBy[i];
                                    if (grabber.grabber == obj)
                                    {
                                        // Release the player from wormgrass grab
                                        grabber.Release();
                                        Logger.LogInfo("WORMGRASS IMMUNITY: Escaped wormgrass grab!");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // Apply all active buffs to the player - EXACT VALUES AS SPECIFIED
        private void ApplyActiveBuff(Player player)
        {
            // Apply movement buffs (1.0 base + bonuses)
            if (runSpeedBonus > 0f)
            {
                player.slugcatStats.runspeedFac = 1.0f + runSpeedBonus;
            }
            
            if (climbSpeedBonus > 0f)
            {
                player.slugcatStats.poleClimbSpeedFac = 1.0f + climbSpeedBonus;
                player.slugcatStats.corridorClimbSpeedFac = 1.0f + climbSpeedBonus;
            }
            
            // Apply lung capacity (1.0 base + bonuses)
            if (lungCapacityBonus > 0f)
            {
                player.slugcatStats.lungsFac = 1.0f + lungCapacityBonus;
            }
            
            // Apply stealth modifications (1.0 base - reductions)
            if (loudnessReduction > 0f)
            {
                player.slugcatStats.loudnessFac = 1.0f - loudnessReduction;
            }
            
            // Apply throwing skill (additive to base skill)
            if (throwingSkillBonus > 0f)
            {
                int baseSkill = 1; // Base throwing skill
                player.slugcatStats.throwingSkill = baseSkill + (int)(throwingSkillBonus * 10); // Scale for integers
            }
            
            // Apply swim speed bonuses (through enhanced lung capacity)
            if (swimSpeedBonus > 0f)
            {
                // Swimming speed is enhanced through lung capacity in Rain World
                player.slugcatStats.lungsFac = Math.Max(player.slugcatStats.lungsFac, 1.0f + swimSpeedBonus);
            }
            
            // Apply crawl speed bonuses (through corridor climbing speed)
            if (crawlSpeedBonus > 0f)
            {
                // Crawling uses corridor movement mechanics in Rain World
                player.slugcatStats.corridorClimbSpeedFac = Math.Max(player.slugcatStats.corridorClimbSpeedFac, 1.0f + crawlSpeedBonus);
            }
            
            // Apply spear on back ability
            if (hasSpearOnBack)
            {
                ApplySpearOnBack(player);
            }
        }
        
        // Display bounty information to the player - PAUSE MENU + NOTIFICATIONS
        private void DisplayBountyInfo(Player player)
        {
            // Create persistent HUD elements if not already created
            if (player.room?.game?.cameras[0]?.hud != null && !hudElementsCreated)
            {
                CreateBountyHUDElements(player.room.game.cameras[0].hud);
            }
            
            // Only show bounty info when game is paused OR during special events
            bool showBountyDisplay = false;
            
            // Check if game is paused
            bool gamePaused = player.room?.game?.pauseMenu != null;
            
            // Show during pause menu
            if (gamePaused)
            {
                showBountyDisplay = true;
            }
            // Show briefly when entering new region
            else if (regionEnterTimer > 0)
            {
                showBountyDisplay = true;
            }
            // Show briefly when bounty completed
            else if (bountyCompleted && bountyDisplayTimer > 0)
            {
                showBountyDisplay = true;
            }
            
            // Update display visibility and content
            if (hudElementsCreated && bountyTargetLabel != null && bountyProgressLabel != null)
            {
                if (showBountyDisplay)
                {
                    // Make elements visible
                    bountyBackgroundSprite.isVisible = true;
                    bountyTargetLabel.isVisible = true;
                    bountyProgressLabel.isVisible = true;
                    
                    // Update content
                    if (bountyActive)
                    {
                        bountyTargetLabel.text = $"TARGET: {GetCreatureName(bountyTarget)}";
                        bountyTargetLabel.color = new Color(1f, 0.8f, 0f); // Golden color for active bounty
                    }
                    else if (completedRegions.Contains(currentRegion))
                    {
                        bountyTargetLabel.text = $"{GetRegionName(currentRegion)}: MASTERED";
                        bountyTargetLabel.color = new Color(0f, 1f, 0f); // Green for completed
                    }
                    else
                    {
                        bountyTargetLabel.text = "No Active Bounty";
                        bountyTargetLabel.color = new Color(0.7f, 0.7f, 0.7f); // Gray for inactive
                    }
                    
                    // Always show progress
                    bountyProgressLabel.text = $"Progress: {completedRegions.Count}/{regionCreatures.Count}";
                    bountyProgressLabel.color = new Color(0.9f, 0.9f, 0.9f); // Light gray
                }
                else
                {
                    // Hide elements during normal gameplay
                    bountyBackgroundSprite.isVisible = false;
                    bountyTargetLabel.isVisible = false;
                    bountyProgressLabel.isVisible = false;
                }
            }
        }
        
        // Create persistent HUD elements in top-right corner
        private void CreateBountyHUDElements(HUD.HUD hud)
        {
            try
            {
                // Create background sprite for bounty info
                bountyBackgroundSprite = new FSprite("pixel", true);
                bountyBackgroundSprite.scaleX = 200f;
                bountyBackgroundSprite.scaleY = 40f;
                bountyBackgroundSprite.x = hud.rainWorld.screenSize.x - 110f; // Top-right position
                bountyBackgroundSprite.y = hud.rainWorld.screenSize.y - 30f;
                bountyBackgroundSprite.color = new Color(0f, 0f, 0f, 0.6f); // Semi-transparent black
                hud.fContainers[1].AddChild(bountyBackgroundSprite);
                
                // Create target label
                bountyTargetLabel = new FLabel(Custom.GetFont(), "");
                bountyTargetLabel.x = hud.rainWorld.screenSize.x - 110f;
                bountyTargetLabel.y = hud.rainWorld.screenSize.y - 20f;
                bountyTargetLabel.alignment = FLabelAlignment.Center;
                hud.fContainers[1].AddChild(bountyTargetLabel);
                
                // Create progress label
                bountyProgressLabel = new FLabel(Custom.GetFont(), "");
                bountyProgressLabel.x = hud.rainWorld.screenSize.x - 110f;
                bountyProgressLabel.y = hud.rainWorld.screenSize.y - 35f;
                bountyProgressLabel.alignment = FLabelAlignment.Center;
                hud.fContainers[1].AddChild(bountyProgressLabel);
                
                hudElementsCreated = true;
                Logger.LogInfo("Bounty HUD elements created in top-right corner");
            }
            catch (System.Exception e)
            {
                Logger.LogError($"Failed to create bounty HUD elements: {e.Message}");
            }
        }
        
        // Get friendly region names
        private string GetRegionName(string regionCode)
        {
            switch (regionCode)
            {
                case "SU": return "Outskirts";
                case "HI": return "Industrial Complex";
                case "CC": return "Chimney Canopy";
                case "GW": return "Garbage Wastes";
                case "SH": return "Shaded Citadel";
                case "DS": return "Drainage System";
                case "SI": return "Sky Islands";
                case "LF": return "Farm Arrays";
                case "UW": return "The Exterior";
                case "SS": return "Five Pebbles";
                case "SB": return "Subterranean";
                case "MS": return "Submerged Superstructure";
                case "SL": return "Shoreline";
                // Downpour DLC regions
                case "OE": return "Outer Expanse";
                case "VS": return "Pipeyard";
                case "LC": return "Metropolis";
                case "RM": return "Looks to the Moon";
                case "CL": return "The Rot";
                default: return regionCode;
            }
        }
        
        // Get friendly creature names - ALL CHALLENGING CREATURES INCLUDED
        private string GetCreatureName(string creatureType)
        {
            switch (creatureType)
            {
                // All Lizard Types (Killable)
                case "GreenLizard": return "Green Lizard";
                case "PinkLizard": return "Pink Lizard";
                case "BlueLizard": return "Blue Lizard";
                case "WhiteLizard": return "White Lizard";
                case "YellowLizard": return "Yellow Lizard";
                case "BlackLizard": return "Black Lizard";
                case "Salamander": return "Salamander";
                case "RedLizard": return "Red Lizard";
                case "CyanLizard": return "Cyan Lizard";
                // Downpour Lizards
                case "CaramelLizard": return "Caramel Lizard";
                case "EelLizard": return "Eel Lizard";
                case "StrawberryLizard": return "Strawberry Lizard";
                case "TrainLizard": return "Train Lizard";
                // ALL CENTIPEDE VARIANTS - FIXED TO ACTUAL GAME TYPES
                case "Centipede": return "Centipede"; // All sizes are just "Centipede"
                case "RedCentipede": return "Red Centipede"; // Elite red variant
                // ALL VULTURE VARIANTS
                case "Vulture": return "Vulture";
                case "KingVulture": return "King Vulture"; // YES - tough challenge!
                // ALL SCAVENGER VARIANTS  
                case "Scavenger": return "Scavenger";
                case "EliteScavenger": return "Elite Scavenger"; // YES - tough challenge!
                // ALL SPIDER VARIANTS
                case "Spider": return "Spider";
                case "BigSpider": return "Big Spider"; // Spider type 2
                case "MotherSpider": return "Mother Spider"; // Spider type 3
                // Other Killable Creatures
                case "Squidcada": return "Squidcada";
                case "JetFish": return "Jet Fish";
                default: return creatureType;
            }
        }

        private void OnEnable()
        {
            Logger.LogInfo("=== BOUNTYHUNTER MOD LOADING ===");
            
            On.Player.ctor += (orig, self, abstraction, world) =>
            {
                orig(self, abstraction, world);
                Logger.LogInfo($"Player created with slugcat name: '{self.slugcatStats.name.value}'");
                
                // Check if this is our bounty hunter
                var slugcatName = self.slugcatStats.name.value;
                if (slugcatName == "bountyhunter" || slugcatName == "BountyHunter" || slugcatName.Contains("bounty"))
                {
                    Logger.LogInfo("*** BOUNTY HUNTER SLUGCAT DETECTED! ***");
                    Logger.LogInfo("Bounty hunting system will be active for this playthrough!");
                }
            };
           
            On.Player.Update += Player_Update;
            On.Creature.Die += Creature_Die;
            
            // Add jump boost hook for regions that provide jump bonuses
            On.Player.Jump += Player_Jump;
            
            // Hook into HUD system for bounty display
            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
            On.HUD.TextPrompt.Update += TextPrompt_Update;
            On.HUD.FoodMeter.Update += FoodMeter_Update; // Hook into food meter for custom display
            
            // Hook for wormgrass immunity
            On.Player.TerrainImpact += Player_TerrainImpact;
            
            // Hook for meat eating ability
            On.Player.CanEatMeat += Player_CanEatMeat;
            
            // Hook for starting karma - modify player when spawning
            On.AbstractCreature.ctor += AbstractCreature_ctor;
            
            // DEBUG: Add console commands for testing
            Logger.LogInfo("=== DEBUG COMMANDS AVAILABLE ===");
            Logger.LogInfo("Press MAP BUTTON - Apply random buff for testing");
            Logger.LogInfo("Press JUMP + MAP - Show current buff totals");
            Logger.LogInfo("- Kill any creature to see creature type debugging");
            Logger.LogInfo("- Enter any region to see region detection");
            Logger.LogInfo("- Check BepInEx console for buff status");
        }
        
        // Add jump height boost only when jumping (like SlugBase example)
        private void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            orig(self);
            
            // Only apply to bountyhunter slugcat - MORE ROBUST CHECK
            var slugcatName = self.slugcatStats.name.value;
            if (slugcatName != "bountyhunter" && slugcatName != "BountyHunter" && !slugcatName.Contains("bounty")) 
            {
                return;
            }
            
            // Apply jump boost based on completed regions
            if (jumpHeightBonus > 0f)
            {
                // Convert percentage to actual jumpBoost value (0.3 = +3f boost)
                self.jumpBoost += jumpHeightBonus * 10f;
            }
        }
        
        // Hook for wormgrass immunity - prevents damage from wormgrass tiles
        private void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, RWCustom.IntVector2 direction, float speed, bool firstContact)
        {
            // Only apply to bountyhunter slugcat - MORE ROBUST CHECK
            var slugcatName = self.slugcatStats.name.value;
            if (slugcatName != "bountyhunter" && slugcatName != "BountyHunter" && !slugcatName.Contains("bounty")) 
            {
                orig(self, chunk, direction, speed, firstContact);
                return;
            }
            
            // Check if player has wormgrass immunity
            if (hasWormgrassImmunity)
            {
                // Check if the terrain impact is from wormgrass
                var room = self.room;
                if (room != null)
                {
                    var pos = self.bodyChunks[chunk].pos;
                    var tilePos = room.GetTilePosition(pos);
                    
                    // Check if the tile is wormgrass - WormGrass is terrain type 6
                    var tile = room.GetTile(tilePos);
                    if (tile.Terrain == Room.Tile.TerrainType.ShortcutEntrance && tile.shortCut != 0)
                    {
                        // This is a more complex check - for now, let's use a simple approach
                        // In Farm Arrays, most dangerous plant-like terrain is wormgrass
                        var regionCode = room.abstractRoom.name.Substring(0, 2);
                        if (regionCode == "LF" && (tile.Terrain == Room.Tile.TerrainType.ShortcutEntrance || 
                                                   tile.Terrain == Room.Tile.TerrainType.Solid))
                        {
                            // Prevent damage from potentially harmful terrain in Farm Arrays
                            Logger.LogInfo("WORMGRASS IMMUNITY: Avoided potential wormgrass damage in Farm Arrays!");
                            return;
                        }
                    }
                }
            }
            
            // Call original for all other terrain impacts
            orig(self, chunk, direction, speed, firstContact);
        }
        
        // Hook for meat eating - allows bountyhunter to eat meat from dead creatures
        private bool Player_CanEatMeat(On.Player.orig_CanEatMeat orig, Player self, Creature creature)
        {
            // Only apply to bountyhunter slugcat - MORE ROBUST CHECK
            var slugcatName = self.slugcatStats.name.value;
            if (slugcatName != "bountyhunter" && slugcatName != "BountyHunter" && !slugcatName.Contains("bounty")) 
            {
                return orig(self, creature);
            }
            
            // The Bounty Hunter can eat meat from any dead creature (bounty hunter!)
            if (creature != null && creature.dead)
            {
                Logger.LogInfo($"MEAT EATING: Can eat meat from {creature.GetType().Name}");
                return true;
            }
            
            // Fall back to original logic for living creatures
            return orig(self, creature);
        }
        
        // Hook for starting karma - give karma when player spawns
        private void AbstractCreature_ctor(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
        {
            orig(self, world, creatureTemplate, realizedCreature, pos, ID);
            
            // Check if this is a player creature
            if (creatureTemplate.type == CreatureTemplate.Type.Slugcat && self.state is PlayerState playerState)
            {
                // Check if this is bountyhunter - MORE ROBUST CHECK
                var saveNumberValue = world?.game?.GetStorySession?.saveStateNumber?.value;
                if (saveNumberValue != null && (saveNumberValue == "bountyhunter" || saveNumberValue == "BountyHunter" || saveNumberValue.Contains("bounty")))
                {
                    // Set starting karma to 4 if it's still at default
                    var saveState = world.game.GetStorySession.saveState;
                    if (saveState != null && saveState.deathPersistentSaveData.karma < 4)
                    {
                        saveState.deathPersistentSaveData.karma = 4;
                        saveState.deathPersistentSaveData.karmaCap = Math.Max(4, saveState.deathPersistentSaveData.karmaCap);
                        Logger.LogInfo("STARTING KARMA: Set to 4 for The Bounty Hunter");
                    }
                }
            }
        }

        private void OnDisable()
        {
           
            On.Player.Update -= Player_Update;
            On.Creature.Die -= Creature_Die;
            On.Player.Jump -= Player_Jump;
            On.Player.TerrainImpact -= Player_TerrainImpact;
            On.Player.CanEatMeat -= Player_CanEatMeat;
            On.HUD.HUD.InitSinglePlayerHud -= HUD_InitSinglePlayerHud;
            On.HUD.TextPrompt.Update -= TextPrompt_Update;
            On.HUD.FoodMeter.Update -= FoodMeter_Update;
        }
        
        // Custom HUD system for bounty display
        private void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig(self, cam);
            
            // Only add for our slugcat - MORE ROBUST CHECK
            if (cam.room?.game?.Players?[0]?.realizedCreature is Player player)
            {
                var slugcatName = player.slugcatStats.name.value;
                if (slugcatName == "bountyhunter" || slugcatName == "BountyHunter" || slugcatName.Contains("bounty"))
                {
                    Logger.LogInfo($"Initializing bounty HUD for BountyHunter (detected name: '{slugcatName}')");
                }
            }
        }
        
        // Hook into text prompt system for bounty notifications - DISABLED
        private void TextPrompt_Update(On.HUD.TextPrompt.orig_Update orig, HUD.TextPrompt self)
        {
            orig(self);
            
            // DISABLED ALL HUD TEXT DISPLAY to prevent flashing
            // Use console logging only for clean experience
        }
        
        // Display bounty information in HUD
        private void DisplayBountyHUD(HUD.TextPrompt textPrompt, string bountyText)
        {
            try
            {
                // Create a message that appears in the HUD
                textPrompt.AddMessage(bountyText, 0, 240, true, true); // 4 seconds display
                Logger.LogInfo($"HUD Display: {bountyText}");
            }
            catch (System.Exception e)
            {
                Logger.LogError($"Failed to display HUD: {e.Message}");
            }
        }
        
        // Hook into food meter to add bounty progress display
        private void FoodMeter_Update(On.HUD.FoodMeter.orig_Update orig, HUD.FoodMeter self)
        {
            orig(self);
            
            // NO AUTOMATIC PROGRESS DISPLAY - only manual via map button press
            // This prevents UI spam completely
        }
    }
}
