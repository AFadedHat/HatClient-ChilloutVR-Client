using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;


[assembly: AssemblyTitle("HatClient")]
[assembly: AssemblyDescription("HatClient")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("TwoFadedHats")]
[assembly: AssemblyProduct("HatClient")]
[assembly: AssemblyCopyright("HatClient Copyright © 2026")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]


[assembly: ComVisible(false)]

[assembly: Guid("4BA58EB4-181A-4661-86F3-6E45DCB88DAE")]

[assembly: AssemblyVersion(BTKUILib.BuildInfo.Version + ".0")]
[assembly: AssemblyFileVersion(BTKUILib.BuildInfo.Version + ".0")]
[assembly: MelonInfo(typeof(BTKUILib.BTKUILib), BTKUILib.BuildInfo.Name, BTKUILib.BuildInfo.Version, BTKUILib.BuildInfo.Author)]
[assembly: MelonGame("ChilloutVR", "ChilloutVR")]
[assembly: MelonPriority(-10)]
[assembly: HarmonyDontPatchAll]