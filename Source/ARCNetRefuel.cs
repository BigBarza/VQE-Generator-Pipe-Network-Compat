using System;
using PipeSystem;
using UnityEngine;
using VanillaQuestsExpandedTheGenerator;
using Verse;

namespace VQEARCFuelLoader {
	//This is a clone of CompProperties_Resource, which points to our own Comp
	public class CompPropertiesArcPipeNetHook : CompProperties_Resource
	{
		public float ConversionRate = 1;
		public CompPropertiesArcPipeNetHook()
		{
			this.compClass = typeof(ArcPipeNetHook);
		}
	}

	//This comp extends CompResource, which handles the actual pipe network connection.
	public class ArcPipeNetHook : CompResource
	{
		private CompPropertiesArcPipeNetHook Properties => props as CompPropertiesArcPipeNetHook;
		
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
