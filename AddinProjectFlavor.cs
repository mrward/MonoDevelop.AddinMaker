using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;

namespace MonoDevelop.AddinMaker
{
	class AddinProjectFlavor : DotNetProjectExtension
	{
		protected override void Initialize ()
		{
			base.Initialize ();

			//TODO: load the actual addin registry referenced from the project file
			AddinRegistry = AddinManager.Registry;

			AddinReferences = new AddinReferenceCollection (this);
			Project.Items.Bind (AddinReferences);
		}

		protected override DotNetProjectFlags OnGetDotNetProjectFlags ()
		{
			return base.OnGetDotNetProjectFlags () | DotNetProjectFlags.IsLibrary;
		}

		protected override bool OnGetSupportsFramework (TargetFramework framework)
		{
			return framework.Id == TargetFrameworkMoniker.NET_4_5;
		}

		protected override SolutionItemConfiguration OnCreateConfiguration (string name, ConfigurationKind kind)
		{
			var cfg = new AddinProjectConfiguration (name);
			cfg.CopyFrom (base.OnCreateConfiguration (name, kind));
			return cfg;
		}

		protected override void OnItemsAdded (IEnumerable<ProjectItem> objs)
		{
			base.OnItemsAdded (objs);

			var addinRefs = objs.OfType<AddinReference> ().ToList ();
			if (addinRefs.Count == 0)
				return;

			var args = new AddinReferenceEventArgs ();
			foreach (var item in addinRefs) {
				item.OwnerProject = this;
				args.AddInfo (this, item);
			}
			OnAddinReferenceAdded (args);
		}

		protected virtual void OnAddinReferenceAdded (AddinReferenceEventArgs args)
		{
			var evt = AddinReferenceAdded;
			if (evt != null)
				evt (this, args);
		}

		protected override void OnItemsRemoved (IEnumerable<ProjectItem> objs)
		{
			base.OnItemsRemoved (objs);

			var addinRefs = objs.OfType<AddinReference> ().ToList ();
			if (addinRefs.Count == 0)
				return;

			var args = new AddinReferenceEventArgs ();
			foreach (var item in addinRefs) {
				item.OwnerProject = null;
				args.AddInfo (this, item);
			}
			OnAddinReferenceRemoved (args);
		}

		protected virtual void OnAddinReferenceRemoved (AddinReferenceEventArgs args)
		{
			var evt = AddinReferenceRemoved;
			if (evt != null)
				evt (this, args);
		}

		public AddinReferenceCollection AddinReferences { get; private set; }

		public IEnumerable<Addin> GetReferencedAddins ()
		{
			yield return AddinRegistry.GetAddin ("MonoDevelop.Core");
			yield return AddinRegistry.GetAddin ("MonoDevelop.Ide");
			foreach (var ar in AddinReferences) {
				yield return AddinRegistry.GetAddin (ar.Id);
			}
		}

		public event EventHandler<AddinReferenceEventArgs> AddinReferenceAdded;
		public event EventHandler<AddinReferenceEventArgs> AddinReferenceRemoved;

		public AddinRegistry AddinRegistry { get; private set; }

		protected override ExecutionCommand OnCreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration)
		{
			var cmd = (DotNetExecutionCommand) base.OnCreateExecutionCommand (configSel, configuration);
			cmd.Command = Assembly.GetEntryAssembly ().Location;
			cmd.Arguments = "--no-redirect";
			cmd.EnvironmentVariables["MONODEVELOP_DEV_ADDINS"] = Project.GetOutputFileName (configSel).ParentDirectory;
			cmd.EnvironmentVariables ["MONODEVELOP_CONSOLE_LOG_LEVEL"] = "All";
			return cmd;
		}

		protected override bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return true;
		}

		protected override IList<string> OnGetCommonBuildActions ()
		{
			var list = new List<string> (base.OnGetCommonBuildActions ());
			list.Add ("AddinFile");
			return list;
		}
	}

	class AddinReferenceEventArgs : EventArgsChain<AddinReferenceEventInfo>
	{
		public void AddInfo (AddinProjectFlavor project, AddinReference reference)
		{
			Add (new AddinReferenceEventInfo (project, reference));
		}
	}

	class AddinReferenceEventInfo
	{
		public AddinReference Reference { get; private set; }
		public AddinProjectFlavor Project { get; private set; }

		public AddinReferenceEventInfo (AddinProjectFlavor project, AddinReference reference)
		{
			Reference = reference;
			Project = project;
		}
	}
}