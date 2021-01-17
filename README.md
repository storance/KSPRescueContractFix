# KSPRescueContractFix

KSPRescueContractFix fixes some issues with KSP's stock rescue contract when using mods.

## Rescue Contract Fixes

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
* Tantares
* Tundra Exploration

### Rescue Contract orbits are sometimes spawned inside the atmosphere when using a rescaled planet pack

KSPRescueContractFix will raise the periapsis out of the atmosphere and will raise the apoapsis to keep the same eccentricty as the old orbit.  More specifically, it increases the semi-major axis but leaves all the other orbital parameters alone (inclination, eccentricity, argument of the periapsis, longitude of the ascending node, and mean anomaly at epoch).


## Acknowledgements
The part restrictions is based off shadowmage45's [KSPRescuePodFix](https://github.com/shadowmage45/KSPRescuePodFix) also licensed under GPLv3.