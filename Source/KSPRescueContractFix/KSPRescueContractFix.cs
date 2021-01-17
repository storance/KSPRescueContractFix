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

            uint recoveryType = ParseIntConfigValue(contractData, "recoveryType");
            // 0 = Not present/invalid
            // 1 = Recover Kerbal
            // 2 = Recover Part
            // 3 = Recover Kerbal and Pod
            if (recoveryType != 1 && recoveryType != 3)
            {
                return;
            }

            uint partId = ParseIntConfigValue(contractData, "partID");
            // if part is not zero, then it's already been spawned
            if (partId != 0)
            {
                return;
            }

            string partName = contractData.GetValue("partName");
            if (!string.IsNullOrEmpty(partName) && !config.IsValidCrewedPart(partName))
            {
                string newPartName = recoveryType == 1 
                    ? config.getRandomAllowedCrewedPart(rnd) 
                    : config.getRandomAllowedCrewedPartWithSameMass(rnd, partName);
                Log($"{contract.Title}: Replacing crewed part {partName} with allowed crewed part {newPartName}.");
                contractData.SetValue("partName", newPartName, true);
                // Contract.Load() appears to append contract parameters instead of replace.
                // To prevent the contract parameters from being duplicated, delete them from the contract ConfigNode
                // before loading it back into the contract.
                contractData.RemoveNodes("PARAM");
                Contract.Load(contract, contractData);
            }
        }

        public void OnContractAccepted(Contract contract)
        {
            if (!IsRecoverAssetContract(contract))
            {
                return;
            }

            ConfigNode contractData = new ConfigNode("CONTRACT");
            contract.Save(contractData);

            uint partId = ParseIntConfigValue(contractData, "partID");
            // if part is not zero, then it's already been spawned
            if (partId == 0)
            {
                return;
            }

            Vessel vessel = GetVessel(partId);
            if (vessel == null || (vessel.situation != Vessel.Situations.ORBITING 
                && vessel.situation != Vessel.Situations.SUB_ORBITAL))
            {
                return;
            }

            var body = vessel.mainBody;
            var sma = vessel.orbit.semiMajorAxis;
            var e = vessel.orbit.eccentricity;

            var rpe = sma * (1 - e);
            var pe = rpe - body.Radius;

            double minPeriapsis = config.GetMinPeriapsis(body);
            if (minPeriapsis > 0 && pe < minPeriapsis)
            {
                double newPe = minPeriapsis + getRandomJitter(body);
                double newRpe = newPe + body.Radius;
                double newSma = newRpe / (1 - e);
                double newRap = newSma * (1 + e);
                double newAp = newRap - body.Radius;

                Log($"{contract.Title}: Periapsis ({pe}) of {vessel.name} is below the configured minimum ({minPeriapsis}).  " +
                    $"Adjusting periapasis to {newPe} and apoapsis to {newAp}.");
                vessel.orbit.semiMajorAxis = newSma;
            }
        }

        public static Vessel GetVessel(uint partId)
        {
            ProtoPartSnapshot protoPart = FlightGlobals.FindProtoPartByID(partId);
            if (protoPart == null)
            {
                LogError($"Failed to find part {partId} in any of the {FlightGlobals.Vessels.Count} vessels!");
                return null;
            }

            return protoPart.pVesselRef.vesselRef;
        }

        private int getRandomJitter(CelestialBody body)
        {
            int maxJitter = config.GetJitter(body);
            if (maxJitter == 0)
            {
                return 0;
            }

            return rnd.Next(maxJitter);
        }

        private static uint ParseIntConfigValue(ConfigNode node, String name, uint defaultValue = 0)
        {
            if (!node.HasValue(name))
            {
                return defaultValue;
            }

            uint parsedInt = defaultValue;
            if (!uint.TryParse(node.GetValue(name), out parsedInt))
            {
                return defaultValue;
            }

            return parsedInt;
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
