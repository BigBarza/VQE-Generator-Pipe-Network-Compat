using System.Collections.Generic;
using System.Linq;
using PipeSystem;
using UnityEngine;
using VanillaQuestsExpandedTheGenerator;
using Verse;

namespace VQEARCFuelLoader
{
    [StaticConstructorOnStartup]
    public class ArcPipeNetConnector
    {
        static ArcPipeNetConnector()
        {
            ThingDef holder = DefDatabase<ThingDef>.GetNamed("PipedARC_DummyHolder");
            
            List<ThingDef> arcs = holder.GetModExtension<ArcListModExtension>().ARCS;
            
            List<PipeNetDef> netDefs = DefDatabase<PipeNetDef>.AllDefsListForReading;
            foreach (var netDef in netDefs)
            {
                if(netDef.linkToRefuelables == null) continue;
                foreach (var link in netDef.linkToRefuelables)
                {
                    foreach (var arc in arcs)
                    {
                        CompProperties_RefuelableWithOverdrive fuelComp =
                            arc.GetCompProperties<CompProperties_RefuelableWithOverdrive>();
                        if (fuelComp.fuelFilter.AllowedThingDefs.Contains(link.thing))
                        {
                            arc.comps.Add(new CompProperties_ArcPipeNetConnector
                            {
                                pipeNet = netDef,
                                ConversionRate = link.ratio
                            });
                        }
                    }
                }
            }
        }
    }

    public class ArcListModExtension : DefModExtension
    {
        public List<ThingDef> ARCS;
    }
    
    //This is a clone of CompProperties_Resource, which points to our own Comp
    public class CompProperties_ArcPipeNetConnector : CompProperties_Resource
    {
        public float ConversionRate = 1;
        public CompProperties_ArcPipeNetConnector()
        {
            this.compClass = typeof(CompArcPipeNetConnector);
        }
		
    }

    //This comp extends CompResource, which handles the actual pipe network connection.
    public class CompArcPipeNetConnector : CompResource
    {
        private CompProperties_ArcPipeNetConnector Properties => props as CompProperties_ArcPipeNetConnector;
		
        public override void CompTick()
        {
            base.CompTick();
            //We check if the parent has a CompRefuelableWithOverdrive, which should only be on ARCs beyond the first few ones.
            if (!parent.Spawned || !parent.HasComp<CompRefuelableWithOverdrive>()) return;
            var resourceNet = base.PipeNet;
            var refuelComp = parent.GetComp<CompRefuelableWithOverdrive>();
            var missingFuelFloat = refuelComp.TargetFuelLevel - refuelComp.Fuel;
            //I don't really want to know what happens if there's a negative number. No withdrawing fuel from the ARC.
            if (missingFuelFloat < 0) return;
            //This is important. Only whole fuel units should be withdrawn from a pipe network, otherwise we'll just create fuel from nothing.
            //THIS MIGHT BE A FRAMEWORK ISSUE!
            var missingFuel = Mathf.Floor(missingFuelFloat) * Properties.ConversionRate;
            //Finally, either withdraw the needed fuel, or however much is left.
            var withdraw = missingFuel <= resourceNet.Stored ? missingFuel : resourceNet.Stored;
            resourceNet.DrawAmongStorage(withdraw, resourceNet.storages);
            refuelComp.Refuel(withdraw);
        }
    }
    
}