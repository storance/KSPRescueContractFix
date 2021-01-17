# KSPRescueContractFix

KSPRescueContractFix fixes some issues with KSP's stock rescue contract when using mods.

## Issues Fixed

### Kerbals spawning in crewed parts without EVA hatches

KSPRescueContractFix will restrict parts Kerbals can spawn in based a user-defined list of allowable parts.  Out of the box, this list contains parts from:

* Stock
* MakingHistory
* Airplane Plus
* Bluedog Design Bureau
* HabTech 2
* Mark IV Spaceplane System
* Nearfuture Spacecraft
* reDIRECT
* SOCK
* Station Parts Expansion Redux
* Tantares
* Tundra Exploration

### Rescue Contract orbits are sometimes spawned inside the atmosphere when using a rescaled planet pack

KSPRescueContractFix will raise the periapsis out of the atmosphere and will raise the apoapsis to keep the same eccentricty as the old orbit.  More specifically, it increases the semi-major axis but leaves all the other orbital parameters alone (inclination, eccentricity, argument of the periapsis, longitude of the ascending node, and mean anomaly at epoch).

## Configuration Settings

```
RESCUE_CONTRACT_FIX_CONFIG
{
	minPeriapsis = 10000
	periapsisMinJitter = 1000
	periapsisMaxJitter = 2000
	maxMassPercentDiff=0.1

	ALLOWED_PARTS
	{
		part = landerCabinSmall
		part = mk2LanderCabin
		part = mk2LanderCabin_v2
		part = mk1pod
		part = mk1pod_v2
		part = Mark1-2Pod
		part = Mark1Cockpit
		part = MK1CrewCabin
		part = Mark2Cockpit
		part = mk2Cockpit_Standard
		part = mk2Cockpit_Inline
		part = cupola
		part = crewCabin
		part = mk1-3pod
		part = mk3Cockpit_Shuttle
	}

	BODY
	{
		name = Kerbin
		periapsisMinJitter = 1500
		periapsisMaxJitter = 2500
		minPeriapsis = 100000
	}
}
```
* **minPeriapsis** - Configures the absolute minimum periapsis in meters for orbits.  If the atmosphere height of the parent body is larger, that will be used instead.  Any value <= 0 will disable the absolute min periapsis but will still use the atmosphere height as the minimum.
* **periapsisMinJitter**, **periapsisMaxJitter** - If the orbit needs to be corrected, add a random amount of meters to the periapasis between these two values.  This prevents the rescue orbit from having a round periapsis.
* **maxMassPercentDiff** - When replacing a part for the recover kerbal and part contracts, attempt to find a part whose mass is within the configured percent difference of the original part's mass.  Should be a value between 0 and 1.
* **ALLOWED_PARTS** - List of allowed crewed parts for rescue contracts involving kerbals.
* **BODY** - Allows overriding **minPeriapsis**, **periapsisMinJitter**, and **periapsisMaxJitter** on a per celestial body basis.
	* **name** - The name of the celestial body
	* **minPeriapsis** - Overrides minPeriapsis for this celestial body.  Any negative value will use the global setting for this instead.
	* **periapsisMinJitter** - Overrides periapsisMinJitter for this celestial body.  Any negative value will use the global setting for this instead.
	* **periapsisMaxJitter** - Overrides periapsisMaxJitter for this celestial body.  Any negative value will use the global setting for this instead.

## Dependencies
* Module Manager

## Acknowledgements
The part restrictions is based off shadowmage45's [KSPRescuePodFix](https://github.com/shadowmage45/KSPRescuePodFix) also licensed under GPLv3.