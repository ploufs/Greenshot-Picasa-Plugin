
using System.ComponentModel;
using System.Drawing;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using IniFile;

namespace GreenshotPicasaPlugin
{
	public class PicasaDestination :AbstractDestination
	{
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PicasaDestination));
		private static PicasaConfiguration config = IniConfig.GetIniSection<PicasaConfiguration>();
		private ILanguage lang = Language.GetInstance();

		private PicasaPlugin plugin = null;
		public PicasaDestination(PicasaPlugin plugin) {
			this.plugin = plugin;
		}
		
		public override string Designation {
			get {
				return "Picasa";
			}
		}

		public override string Description {
			get {
				return lang.GetString(LangKey.upload_menu_item);
			}
		}

		public override Image DisplayIcon {
			get {
				ComponentResourceManager resources = new ComponentResourceManager(typeof(PicasaPlugin));
				return (Image)resources.GetObject("Picasa");
			}
		}

		public override bool ExportCapture(ISurface surface, ICaptureDetails captureDetails) {
			using (Image image = surface.GetImageForExport()) {
				bool uploaded = plugin.Upload(captureDetails, image);
				if (uploaded) {
					surface.SendMessageEvent(this, SurfaceMessageTyp.Info, "Exported to Picasa");
					surface.Modified = false;
				}
				return uploaded;
			}
		}
	}
}
