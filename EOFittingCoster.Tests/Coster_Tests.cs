using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EOFittingCosterLib;

namespace EOFittingCoster.Tests
{
    [TestClass]
    public class Coster_Tests
    {
        [TestMethod]
        public void Costify_Success_OneItemNotFound()
        {
            // first missile launcher has bad module name
            string rawEFT = @"  [Cerberus, small gang anti-frig]

                                Nanofiber Internal Structure II
                                Ballistic Control System II
                                Ballistic Control System II
                                Reactor Control Unit II
                                
                                X-Large Ancillary Shield Booster, Cap Booster 400
                                50MN Cold-Gas Enduring Microwarpdrive
                                Warp Disruptor II
                                X5 Prototype Engine Enervator
                                EM Ward Amplifier II
                                
                                Rapid Light Missil Launcher II, Caldari Navy Scourge Light Missile
                                Rapid Light Missile Launcher II, Caldari Navy Scourge Light Missile
                                Rapid Light Missile Launcher II, Caldari Navy Scourge Light Missile
                                Rapid Light Missile Launcher II, Caldari Navy Scourge Light Missile
                                Rapid Light Missile Launcher II, Caldari Navy Scourge Light Missile
                                Rapid Light Missile Launcher II, Caldari Navy Scourge Light Missile
                                
                                Medium Core Defense Field Extender II
                                Medium Core Defense Field Extender II
                                
                                
                                Acolyte II x3";

            var coster = new Coster();
            var result = coster.Costify(rawEFT);

            Assert.IsFalse(result.AllItemsAppraised);
            Assert.IsTrue(result.Sum > (decimal)250000000);
            Assert.IsTrue(result.Message.Contains("could not be appraised"));
        }
        [TestMethod]
        public void Costify_Success3()
        {
            string rawEFT = @"[Rattlesnake, Vanguard]
Damage Control II
Drone Damage Amplifier II
Drone Damage Amplifier II
Drone Damage Amplifier II
Omnidirectional Tracking Enhancer II
Omnidirectional Tracking Enhancer II

Large Shield Extender II
Adaptive Invulnerability Field II
EM Ward Field II
Large Shield Extender II
Pith B-Type Explosive Deflection Field
Adaptive Invulnerability Field II
Large Micro Jump Drive

Cruise Missile Launcher II, Scourge Fury Cruise Missile
Cruise Missile Launcher II, Scourge Fury Cruise Missile
Cruise Missile Launcher II, Scourge Fury Cruise Missile
Cruise Missile Launcher II, Scourge Fury Cruise Missile
Drone Link Augmentor II
Drone Link Augmentor II

Large Core Defense Field Extender I
Large Core Defense Field Extender I
Large Core Defense Field Extender I

Bouncer II x2
Berserker II x2
Garde II x2
Warrior II x5
Bouncer II x3
Garde II x2
Ogre II x2
Optimal Range Script x2
Tracking Speed Script x2
Targeting Range Dampening Script x1
Scan Resolution Dampening Script x1
Targeting Range Script x1
Scan Resolution Script x1
Optimal Range Disruption Script x1
Tracking Speed Disruption Script x1
Caldari Navy Inferno Cruise Missile x1000
Caldari Navy Scourge Cruise Missile x1000
Inferno Fury Cruise Missile x1000
Scourge Fury Cruise Missile x1000
Missile Range Script x2
Nanite Repair Paste x300";

            var coster = new Coster();
            var result = coster.Costify(rawEFT);

            Assert.IsTrue(result.AllItemsAppraised);
            Assert.IsTrue(result.Sum > (decimal)250000000);
            Assert.IsTrue(result.Message == "All items were successfully appraised.");
        }
        [TestMethod]
        public void MyTestMethod()
        {
            var raw = @"[Armageddon, NC. TFI]
Armor Thermic Hardener II
True Sansha Armor Kinetic Hardener
True Sansha Armor Explosive Hardener
1600mm Reinforced Steel Plates II
1600mm Reinforced Steel Plates II
Internal Force Field Array I
True Sansha Energized Adaptive Nano Membrane

Experimental 100MN Afterburner I
Large Micro Jump Drive
Heavy Capacitor Booster II
Omnidirectional Tracking Link II

Heavy Unstable Power Fluctuator I
Heavy Unstable Power Fluctuator I
Heavy Unstable Power Fluctuator I
Drone Link Augmentor II
Cruise Missile Launcher II
Cruise Missile Launcher II
Cruise Missile Launcher II


Large Trimark Armor Pump I
Large Trimark Armor Pump I
Large Trimark Armor Pump I

Bouncer II x5";
            var coster = new Coster();
            var result = coster.Costify(raw);
        }
    }
}
