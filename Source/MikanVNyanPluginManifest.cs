using System.Reflection;
using VNyanInterface;

namespace MikanXR.VNyan
{
	// What VNyan looks for in the assembly: it constructs this and calls InitializePlugin once
	// the plugin assemblies are loaded, so no loader asset bundle is needed
	public class MikanVNyanPluginManifest : IVNyanPluginManifest
	{
		public string PluginName { get; } = "MikanXR_VNyan";
		// The <Version> of MikanXR_VNyan.csproj, which the build writes into the assembly
		public string Version { get; } = typeof(MikanVNyanPluginManifest).Assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
		public string Title { get; } = "MikanXR";
		public string Author { get; } = "MikanXR";
		public string Website { get; } = "https://github.com/MikanXR/MikanXR_VNyan";

		public void InitializePlugin()
		{
			MikanVNyanPlugin.Start();
		}
	}
}
