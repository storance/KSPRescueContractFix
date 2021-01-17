using System;
using System.Collections.Generic;
using System.Linq;

namespace KSPRescueContractFix
{
    [Serializable]
    public class RescueContractConfig : IConfigNode
    {
        [Persistent]
        public int periapsisMinJitter = 1000;
        [Persistent]
        public int periapsisMaxJitter = 2000;
        [Persistent]
        public int minPeriapsis = 0;
        [Persistent]
        public float maxMassPercentDiff = 0.1f;

        public HashSet<string> allowedCrewedPartsSet;
        public List<string> allowedCrewedPartsList;
        public Dictionary<string, BodyOverride> bodyOverrides;

        public void Load(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(this, node);

            allowedCrewedPartsSet = new HashSet<string>();
            foreach (ConfigNode partsNode in node.GetNodes("ALLOWED_PARTS"))
            {
                foreach (string partName in partsNode.GetValues("part"))
                {
                    if (string.IsNullOrEmpty(partName))
                    {
                        continue;
                    }
                    allowedCrewedPartsSet.Add(FixPartName(partName));
                }
            }
            allowedCrewedPartsList = new List<string>(allowedCrewedPartsSet);

            bodyOverrides = new Dictionary<string, BodyOverride>();
            foreach (ConfigNode bodyNode in node.GetNodes("BODY"))
            {
                BodyOverride body = new BodyOverride();
                body.Load(bodyNode);
                bodyOverrides.Add(body.name, body);
            }
        }

        public bool IsValidCrewedPart(string partName)
        {
            return allowedCrewedPartsSet.Contains(partName);
        }

        public string getRandomAllowedCrewedPart(System.Random rnd)
        {
            if (allowedCrewedPartsList.Count == 0)
            {
                return "landerCabinSmall";
            }

            allowedCrewedPartsList.FindAll(partName =>
            {
                var partInfo = PartLoader.getPartInfoByName(partName);
                if (partInfo == null)
                {
                    return false;
                }

                return true;
            });

            int index = rnd.Next(allowedCrewedPartsList.Count);
            return allowedCrewedPartsList[index];
        }

        public string getRandomAllowedCrewedPartWithSameMass(System.Random rnd, string originalPartName)
        {
            AvailablePart originalPart = PartLoader.getPartInfoByName(originalPartName);
            // we couldn't load the part so just return any random part
            if (originalPart == null)
            {
                return getRandomAllowedCrewedPart(rnd);
            }

            List<AvailablePart> filteredParts = allowedCrewedPartsList
                .Select(name => PartLoader.getPartInfoByName(name))
                .Where(part => part != null && CalcPercentDifference(originalPart.MinimumMass, part.MinimumMass) <= maxMassPercentDiff)
                .ToList();

            if (filteredParts.Count == 0)
            {
                RescueContractFix.Log($"Could not find part similar in mass to {originalPartName}. " +
                    $"Using any random allowed part instead.");
                return getRandomAllowedCrewedPart(rnd);
            }

            int index = rnd.Next(filteredParts.Count);
            return filteredParts[index].name;
        }

        public void Save(ConfigNode node)
        {
            ConfigNode.CreateConfigFromObject(this, node);

            ConfigNode partsNode = new ConfigNode("ALLOWED_PARTS");
            foreach (string part in allowedCrewedPartsSet)
            {
                partsNode.AddValue("part", part);
            }
            node.AddNode(partsNode);

            foreach (BodyOverride body in bodyOverrides.Values)
            {
                ConfigNode bodyNode = new ConfigNode("BODY");
                body.Save(bodyNode);
                node.AddNode(bodyNode);
            }
        }

        public double GetMinPeriapsis(CelestialBody body)
        {
            int minJitter = periapsisMinJitter;
            double minPeriapsis = this.minPeriapsis;

            if (bodyOverrides.ContainsKey(body.name))
            {
                BodyOverride bodyOverride = bodyOverrides[body.name];
                if (bodyOverride.periapsisMinJitter >= 0)
                {
                    minJitter = bodyOverride.periapsisMinJitter;
                }

                if (bodyOverride.minPeriapsis >= 0)
                {
                    minPeriapsis = bodyOverride.minPeriapsis;
                }
            }

            if (body.atmosphere)
            {
                minPeriapsis = Math.Max(body.atmosphereDepth, minPeriapsis);
            }

            return minPeriapsis + minJitter;
        }

        public int GetJitter(CelestialBody body)
        {
            int minJitter = periapsisMinJitter;
            int maxJitter = periapsisMaxJitter;

            if (bodyOverrides.ContainsKey(body.name))
            {
                BodyOverride bodyOverride = bodyOverrides[body.name];
                if (bodyOverride.periapsisMinJitter >= 0)
                {
                    minJitter = bodyOverride.periapsisMinJitter;
                }

                if (bodyOverride.periapsisMaxJitter >= 0)
                {
                    maxJitter = bodyOverride.periapsisMaxJitter;
                }
            }

            return Math.Max(0, maxJitter - minJitter);
        }

        // For some reason, KSP replaces _ with . in the part names
        private static string FixPartName(string partName) => partName.Replace('_', '.');

        private static float CalcPercentDifference(float mass1, float mass2) => Math.Abs(mass1 - mass2) / (mass1 + mass2);

        public override string ToString()
        {
            return "RescueContractConfig{"
                + ",minPeriapsis=" + minPeriapsis
                + ",periapsisMinJitter=" + periapsisMinJitter
                + ",periapsisMaxJitter=" + periapsisMaxJitter 
                + ",allowedParts=" + string.Join(", ", allowedCrewedPartsSet)
                + ",bodyOverrides=" + string.Join(", ", bodyOverrides.Values)
                + "}";
        }
    }

    [Serializable]
    public class BodyOverride : IConfigNode
    {
        [Persistent]
        public string name;

        [Persistent]
        public int minPeriapsis = -1;

        [Persistent]
        public int periapsisMinJitter = -1;

        [Persistent]
        public int periapsisMaxJitter = -1;

        public void Load(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(this, node);
        }

        public void Save(ConfigNode node)
        {
            ConfigNode.CreateConfigFromObject(this, node);
        }

        
        public override string ToString()
        {
            return "BodyOverride{"
                + "name=" + name 
                + ",minPeriapsis=" + minPeriapsis 
                + ",periapsisMinJitter=" + periapsisMinJitter 
                + ",periapsisMaxJitter=" + periapsisMaxJitter 
                + "}";
        }
    }
}
