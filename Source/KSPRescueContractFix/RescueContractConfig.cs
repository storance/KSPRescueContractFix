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

        public HashSet<String> allowedCrewedParts;
        public String[] allowedCrewedPartsArray;
        public Dictionary<string, BodyOverride> bodyOverrides;

        public void Load(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(this, node);

            allowedCrewedParts = new HashSet<string>();
            foreach (ConfigNode partsNode in node.GetNodes("ALLOWED_PARTS"))
            {
                foreach (string partName in partsNode.GetValues("part"))
                {
                    // For some reason, KSP replaces _ with . in the part names
                    allowedCrewedParts.Add(partName.Replace('_', '.'));
                }
            }
            allowedCrewedPartsArray = Enumerable.ToArray(allowedCrewedParts);

            bodyOverrides = new Dictionary<string, BodyOverride>();
            foreach (ConfigNode bodyNode in node.GetNodes("BODY"))
            {
                BodyOverride body = new BodyOverride();
                body.Load(bodyNode);
                bodyOverrides.Add(body.name, body);
            }
        }

        public void Save(ConfigNode node)
        {
            ConfigNode.CreateConfigFromObject(this, node);

            ConfigNode partsNode = new ConfigNode("ALLOWED_PARTS");
            foreach (string part in allowedCrewedParts)
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
    }

    [Serializable]
    public class BodyOverride : IConfigNode
    {
        [Persistent]
        public string name;

        [Persistent]
        public int? minPeriapsis;

        [Persistent]
        public int? periapsisMaxJitter;

        public void Load(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(this, node);
        }

        public void Save(ConfigNode node)
        {
            ConfigNode.CreateConfigFromObject(this, node);
        }
    }
}
