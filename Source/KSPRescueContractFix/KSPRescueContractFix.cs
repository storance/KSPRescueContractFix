using System;
using UnityEngine;
using Contracts;

namespace KSPRescueContractFix
{
    [KSPAddon(KSPAddon.Startup.Instantly | KSPAddon.Startup.EveryScene, true)]
    public class RescueContractFix : MonoBehaviour
    {
        private RescueContractConfig config;
        private static System.Random rnd;

        public void Start()
        {
            DontDestroyOnLoad(this);
            GameEvents.Contract.onOffered.Add(OnContractOffered);
            GameEvents.Contract.onAccepted.Add(OnContractAccepted);
            
            rnd = new System.Random();
        }

        public void OnDestroy()
        {
            GameEvents.Contract.onOffered.Remove(OnContractOffered);
            GameEvents.Contract.onAccepted.Remove(OnContractAccepted);
        }

        public void ModuleManagerPostLoad()
        {
            config = new RescueContractConfig();
            ConfigNode[] configNodes = GameDatabase.Instance.GetConfigNodes("RESCUE_CONTRACT_FIX_CONFIG");
            if (configNodes.Length > 0)
            {
                config.Load(configNodes[0]);
            }
        }


        public void OnContractOffered(Contract contract)
        {
            if (!IsRecoverAssetContract(contract))
            {
                return;
            }

            ConfigNode contractData = new ConfigNode("CONTRACT");
            contract.Save(contractData);

            int partId = ParseIntConfigValue(contractData, "partID");
            // if part is not zero, then it's already been spawned
            if (partId != 0)
            {
                return;
            }

            int recoveryType = ParseIntConfigValue(contractData, "recoveryType");
            // 0 = Not present/invalid
            // 1 = Recover Kerbal
            // 2 = Recover Part
            // 3 = Recover Kerbal and Pod
            if (recoveryType != 1 && recoveryType != 3)
            {
                return;
            }

            string partName = contractData.GetValue("partName");
            if (!string.IsNullOrEmpty(partName) && !isValidCrewedPart(partName))
            {
                string newPartName = getRandomAllowedCrewedPart();
                Log($"{contract.Title}: Replacing crewed part {partName} with allowed crewed part {newPartName}.");
                contractData.SetValue("partName", newPartName, true);
                // Contract.Load() appears to append contract parameters instead of replace.
                // To prevent the contract parameters from being duplicated, delete them from the contract ConfigNode
                // before loading it back into the contract.
                contractData.RemoveNodes("PARAM");
                Contract.Load(contract, contractData);
            }
        }

        private int ParseIntConfigValue(ConfigNode node, String name, int defaultValue = 0)
        {
            if (!node.HasValue(name))
            {
                return defaultValue;
            }

            int parsedInt = defaultValue;
            if (!int.TryParse(node.GetValue(name), out parsedInt))
            {
                return defaultValue;
            }

            return parsedInt;
        }

        private Boolean isValidCrewedPart(string partName)
        {
            return config.allowedCrewedParts.Contains(partName);
        }

        private string getRandomAllowedCrewedPart()
        {
            if (config.allowedCrewedPartsArray.Length <= 0) {
                return "landerCabinSmall";
            }

            int index = rnd.Next(config.allowedCrewedPartsArray.Length);
            return config.allowedCrewedPartsArray[index];
        }

        public void OnContractAccepted(Contract contract)
        {
            if (!IsRecoverAssetContract(contract))
            {
                return;
            }

            ConfigNode contractData = new ConfigNode("CONTRACT");
            contract.Save(contractData);

            string kerbalName = contractData.GetValue("kerbalName");
            if (string.IsNullOrEmpty(kerbalName) || !HighLogic.CurrentGame.CrewRoster.Exists(kerbalName))
            {
                return;
            }

            Vessel vessel = GetVessel(HighLogic.CurrentGame.CrewRoster[kerbalName]);
            if (vessel == null)
            {
                return;
            }

            if (vessel.situation != Vessel.Situations.ORBITING && vessel.situation != Vessel.Situations.SUB_ORBITAL)
            {
                return;
            }

            var body = vessel.mainBody;
            var sma = vessel.orbit.semiMajorAxis;
            var e = vessel.orbit.eccentricity;

            var rpe = sma * (1 - e);
            var pe = rpe - body.Radius;

            double minPeriapsis = GetMinPeriapsis(body);
            if (minPeriapsis > 0 && pe < minPeriapsis)
            {
                double newPe = minPeriapsis + getRandomJitter(body);
                double newRpe = newPe + body.Radius;
                double newSma = newRpe / (1 - e);

                Log($"{contract.Title}: Periapsis ({pe}) of {vessel.name} is below the configured minimum ({minPeriapsis}).  " +
                    $"Adjusting periapasis {newPe} and semi-major axis to {newSma}.");
                vessel.orbit.semiMajorAxis = newSma;
            }
        }

        public static Vessel GetVessel(ProtoCrewMember crew)
        {
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                // workaround for MKS shenanigans with deleting then recreating the kerbal
                // Since MKS deletes the kerbals after the vessel has spawned, the ProtoCrewMember in 
                // vessel.GetVesselCrew() has the same name but is a different instance from
                // the one in the CrewRoster
                foreach (ProtoCrewMember vesselCrew in vessel.GetVesselCrew())
                {
                    if (vesselCrew.name.Equals(crew.name))
                    {
                        return vessel;
                    }
                }
            }

            LogError($"Failed to find {crew.name} in any of the {FlightGlobals.Vessels.Count} vessels!");
            return null;
        }

        private double GetMinPeriapsis(CelestialBody body)
        {
            if (config.bodyOverrides.ContainsKey(body.name))
            {
                BodyOverride bodyOverride = config.bodyOverrides[body.name];

                if (bodyOverride.minPeriapsis != null && bodyOverride.minPeriapsis > 0)
                {
                    return bodyOverride.minPeriapsis.GetValueOrDefault() + config.periapsisMinJitter;
                }
            }

            if (body.atmosphere)
            {
                return Math.Max(body.atmosphereDepth, config.minPeriapsis);
            }

            return config.minPeriapsis;
        }

        private int getRandomJitter(CelestialBody body)
        {
            if (config.bodyOverrides.ContainsKey(body.name))
            {
                BodyOverride bodyOverride = config.bodyOverrides[body.name];
                if (bodyOverride.periapsisMaxJitter != null)
                {
                    if (bodyOverride.minPeriapsis <= 0)
                    {
                        return 0;
                    }
                    else
                    {
                        return rnd.Next(bodyOverride.periapsisMaxJitter.GetValueOrDefault()+1);
                    }
                }
            }

            if (config.periapsisMinJitter == config.periapsisMaxJitter)
            {
                return config.periapsisMaxJitter;
            }

            return rnd.Next(config.periapsisMinJitter, config.periapsisMaxJitter+1);
        }

        private static bool IsRecoverAssetContract(Contract contract)
        {
            return "Contracts.Templates.RecoverAsset".Equals(contract.GetType().FullName);
        }

        internal static void Log(String message)
        {
            MonoBehaviour.print($"[KSPRescueContractFix] {message}");
        }

        internal static void LogError(String message)
        {
            MonoBehaviour.print($"[KSPRescueContractFix] ERROR: {message}");
        }
    }
}
