using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using KSP;

namespace KSPRescueContractFix
{
    public class RescueContractSettings : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("Rescue Pod Fix Enabled",
            toolTip = "Turn on/off restricting parts for rescue contracts where only the Kerbal needs to be recoverd.")]
        public bool rescueKerbalFixEnabled = true;

        [GameParameters.CustomParameterUI("Rescue and Recover Pod Fix Enabled",
            toolTip = "Turn on/off restricting parts for rescue contracts where both the Kerbal and Pod needs to be recoverd.")]
        public bool recoverKerbalPodFixEnabled = true;

        [GameParameters.CustomParameterUI("Orbit Fix Enabled",
            toolTip = "Turn on/off fixing rescue contracts orbits that are spawned inside the planet's atmosphere.")]
        public bool orbitFixEnabled = true;

        public static RescueContractSettings Instance => HighLogic.CurrentGame.Parameters.CustomParams<RescueContractSettings>();

        public override string Title => "Settings";

        public override string DisplaySection => Section;

        public override string Section => "Rescue Contract Fix";

        public override int SectionOrder => 0;

        public override GameParameters.GameMode GameMode => GameParameters.GameMode.CAREER;

        public override bool HasPresets => false;
    }

    public abstract class AllowedPartsSettings : GameParameters.CustomParameterNode
    {
        public static AllowedPartsSettings Instance => (AllowedPartsSettings)
            HighLogic.CurrentGame.Parameters.CustomParams(SettingsBuilder.allowedPartsSettingsType);

        public AllowedPartsSettings()
        {
            foreach (FieldInfo field in GetType().GetFields())
            {
                if (field.FieldType == typeof(bool))
                {
                    field.SetValue(this, true);
                }
            }
        }

        public List<string> FilterAllowedParts(HashSet<string> allParts)
        {
            return allParts.Where(IsPartEnabled).ToList();
        }

        public bool IsPartEnabled(String partName)
        {
            FieldInfo field = GetType().GetField(SettingsBuilder.Sanitize(partName));
            return field != null && (bool)field.GetValue(this);
        }

        public override string Title => "Allowed Parts";

        public override string DisplaySection => Section;

        public override string Section => "Rescue Contract Fix";

        public override int SectionOrder => 1;

        public override GameParameters.GameMode GameMode => GameParameters.GameMode.CAREER;

        public override bool HasPresets => false;
    }

    public class SettingsBuilder
    {
        public static Type allowedPartsSettingsType = null;

        public static void Create(RescueContractConfig config)
        {
            if (allowedPartsSettingsType != null)
            {
                return;
            }

            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("KSPRescueContractFixDynamic"), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("KSPRescueContractFixDynamicModule");

            ConstructorInfo paramUICons = typeof(GameParameters.CustomParameterUI).GetConstructor(new Type[] { typeof(string) });

            TypeBuilder partsSettingsBuilder = moduleBuilder.DefineType("KSPRescueContractFix.AllowedPartsSettings",
                TypeAttributes.Public | TypeAttributes.Class, typeof(AllowedPartsSettings));

            // Define a field for each contract type
            foreach (AvailablePart part in config.allowedCrewedParts.Select(PartLoader.getPartInfoByName).Where(p => p != null).OrderBy(p => p.title))
            {
                FieldBuilder groupField = partsSettingsBuilder.DefineField(Sanitize(part.name), typeof(bool), FieldAttributes.Public);

                CustomAttributeBuilder attBuilder = new CustomAttributeBuilder(paramUICons, new object[] { part.title });
                groupField.SetCustomAttribute(attBuilder);
            }

            allowedPartsSettingsType = partsSettingsBuilder.CreateType();
            GameParameters.ParameterTypes.Add(allowedPartsSettingsType);
        }
        public static string Sanitize(String partName)
        {
            if (string.IsNullOrEmpty(partName))
            {
                return string.Empty;
            }

            return new string(partName.Select(c => Char.IsLetterOrDigit(c) ? c : '_').ToArray());
        }
    }
}
