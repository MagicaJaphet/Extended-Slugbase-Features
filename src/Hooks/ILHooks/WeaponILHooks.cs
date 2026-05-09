using ExtendedSlugbase.Features.PlayerRelated;
using MagicaHookingLibrary.Interfaces;

namespace ExtendedSlugbase.Hooks.ILHooks;
internal class WeaponILHooks : IOwnHooks
{
	public void PreApply()
	{
		IL.SharedPhysics.TraceProjectileAgainstBodyChunks += CanCreateSpears.Implementation.SharedPhysics_TraceProjectileAgainstBodyChunks;
		IL.Spear.HitSomething += CanCreateSpears.Implementation.Spear_HitSomething;
	}

	public void OnApply()
	{
	}

	public void PostApply()
	{
	}
}
