/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2011  Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using System.Linq;

using Google.GData.Photos;
using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.Extensions.Location;
using Google.Picasa;
using IniFile;

namespace GreenshotPicasaPlugin {
	/// <summary>
	/// Description of ImgurUtils.
	/// </summary>
	public class PicasaUtils {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PicasaUtils));
		public static string Picasa_APPLICATION_NAME = "390bea54b8ef045716dd34680d6e21ba";
		private static PicasaConfiguration config = IniConfig.GetIniSection<PicasaConfiguration>();

		private PicasaUtils() {
		}

		public static void LoadHistory() {
			if (config.runtimePicasaHistory == null) {
				return;
			}
			if (config.PicasaUploadHistory == null)
			{
				return;
			}

			if (config.runtimePicasaHistory.Count == config.PicasaUploadHistory.Count) {
				return;
			}
			// Load the Picasa history
			List<string> hashes = new List<string>();
			foreach (string hash in config.PicasaUploadHistory.Keys)
			{
				hashes.Add(hash);
			}
			
			bool saveNeeded = false;

			foreach(string hash in hashes) {
				if (config.runtimePicasaHistory.ContainsKey(hash)) {
					// Already loaded
					continue;
				}
				try {
					PicasaInfo imgurInfo = PicasaUtils.RetrievePicasaInfo(hash);
					if (imgurInfo != null) {
						PicasaUtils.RetrievePicasaThumbnail(imgurInfo);
						config.runtimePicasaHistory.Add(hash, imgurInfo);
					} else {
						LOG.DebugFormat("Deleting not found Picasa {0} from config.", hash);
						config.PicasaUploadHistory.Remove(hash);
						saveNeeded = true;
					}
				} catch (Exception e) {
					LOG.Error("Problem loading Picasa history for hash " + hash, e);
				}
			}
			if (saveNeeded) {
				// Save needed changes
				IniConfig.Save();
			}
		}

		/// <summary>
		/// Do the actual upload to Picasa
		/// For more details on the available parameters, see: http://code.google.com/apis/picasaweb/docs/1.0/developers_guide_dotnet.html
		/// </summary>
		/// <param name="imageData">byte[] with image data</param>
		/// <returns>PicasaResponse</returns>
		public static PicasaInfo UploadToPicasa(byte[] imageData, string title, string filename, string contentType)
		{
			PicasaService service = new PicasaService(Picasa_APPLICATION_NAME);
			service.setUserCredentials(config.Username, config.Password);

			Uri postUri = new Uri(PicasaQuery.CreatePicasaUri(config.Username));

			// build the data stream
			Stream data = new MemoryStream(imageData);

			PicasaEntry entry = (PicasaEntry)service.Insert(postUri, data, contentType, filename);

			return RetrievePicasaInfo(entry);
		}

		public static Image CreateThumbnail(Image image, int thumbWidth, int thumbHeight) {
			int srcWidth=image.Width;
			int srcHeight=image.Height; 
			Bitmap bmp = new Bitmap(thumbWidth, thumbHeight);  
			using (Graphics gr = System.Drawing.Graphics.FromImage(bmp)) {
				gr.SmoothingMode = SmoothingMode.HighQuality  ; 
				gr.CompositingQuality = CompositingQuality.HighQuality; 
				gr.InterpolationMode = InterpolationMode.High; 
				System.Drawing.Rectangle rectDestination = new System.Drawing.Rectangle(0, 0, thumbWidth, thumbHeight);
				gr.DrawImage(image, rectDestination, 0, 0, srcWidth, srcHeight, GraphicsUnit.Pixel);  
			}
			return bmp;
		}

		public static void RetrievePicasaThumbnail(PicasaInfo imgurInfo) {
			LOG.InfoFormat("Retrieving Picasa image for {0} with url {1}", imgurInfo.ID, imgurInfo);
			HttpWebRequest webRequest = (HttpWebRequest)NetworkHelper.CreatedWebRequest(imgurInfo.SquareThumbnailUrl);
			webRequest.Method = "GET";
			webRequest.ServicePoint.Expect100Continue = false;

			using (WebResponse response = webRequest.GetResponse()) {
				Stream responseStream = response.GetResponseStream();
				imgurInfo.Image = Image.FromStream(responseStream);
			}
			return;
		}

		public static PicasaInfo RetrievePicasaInfo(string id)
		{
			return RetrievePicasaInfo(RetrievePicasaEntry(id));
		}

		public static PicasaInfo RetrievePicasaInfo(PicasaEntry picasaEntry)
		{
			PhotoAccessor photoAccessor = new PhotoAccessor(picasaEntry);

			LOG.InfoFormat("Retrieving Picasa info for {0}", photoAccessor.Id);

			PicasaInfo picasaInfo = new PicasaInfo();

			picasaInfo.ID = photoAccessor.Id;
			picasaInfo.Title = picasaEntry.Title.Text;
			picasaInfo.Timestamp = picasaEntry.Updated;
			picasaInfo.Description = picasaEntry.Summary.Text;
			picasaInfo.SquareThumbnailUrl = picasaEntry.Media.Thumbnails[0].Url;
			picasaInfo.OriginalUrl = picasaEntry.Media.Content.Url;

			List<AtomLink> links = picasaEntry.Links.Where(r => r.Type.Equals("text/html", StringComparison.OrdinalIgnoreCase)).ToList();
			picasaInfo.WebUrl = string.Empty;
			if (links.Count > 0)
			{
				picasaInfo.WebUrl = links.First().HRef.ToString();
			}
			return picasaInfo;
		}

		public static PicasaEntry RetrievePicasaEntry(string id)
		{
			PicasaService service = new PicasaService(Picasa_APPLICATION_NAME);
			service.setUserCredentials(config.Username, config.Password);

			PhotoQuery photoQuery = new PhotoQuery(PicasaQuery.CreatePicasaUri(config.Username, string.Empty, id));
			photoQuery.NumberToRetrieve = 1;
			PicasaFeed picasaFeed = service.Query(photoQuery);
			
			if (picasaFeed.Entries.Count > 0)
			{
				return  (PicasaEntry)picasaFeed.Entries[0];
			}

			return null;
		}

		public static void DeletePicasaImage(PicasaInfo picasaInfo)
		{
			// Make sure we remove it from the history, if no error occured
			config.runtimePicasaHistory.Remove(picasaInfo.ID);
			config.PicasaUploadHistory.Remove(picasaInfo.ID);

			PicasaEntry picasaEntry = RetrievePicasaEntry(picasaInfo.ID);
			if (picasaEntry != null)
			{
				picasaEntry.Delete();
			}

			picasaInfo.Image = null;
		}
	}
}
