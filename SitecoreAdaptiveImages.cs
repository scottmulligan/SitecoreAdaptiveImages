/*
 * Version: 0.5
 * 
 * Homepage: http://blog.scottymulligan.com
 * GitHub: https://github.com/scottmulligan/SitecoreAdaptiveImages
 * Twitter: @scottmulligan
 * 
 * LEGAL:
 * Sitecore Adaptive Images by Scott Mulligan is licensed under a Creative Commons Attribution 3.0 Unported License.
 */

using Sitecore.Data.Items;
using Sitecore.Resources.Media;
using System;
using Sitecore.Diagnostics;
using System.Linq;
using System.Web;

namespace adaptiveImages
{
    public class AdaptiveImagesMediaProvider : MediaProvider
    {

        /// <summary>
        /// The resolution break-points to use (screen widths, in pixels)
        /// </summary>
        private readonly int[] resolutions = Sitecore.Configuration.Settings.GetSetting("resolutions").Split(',').Select(int.Parse).ToArray();

        /// <summary>
        /// If there's no cookie FALSE sends the largest var resolutions version (TRUE sends smallest)
        /// </summary>
        private readonly bool mobileFirst = String.Compare(Sitecore.Configuration.Settings.GetSetting("mobileFirst"), "true", System.StringComparison.OrdinalIgnoreCase) == 0;

        /// <summary>
        /// The name of the cookie containing the resolution value
        /// </summary>
        private readonly string cookieName = Sitecore.Configuration.Settings.GetSetting("cookieName");

        /// <summary>
        /// This list is compared to the user agent returned from the browser to determine if mobile first should be turned off because it is a desktop
        /// </summary>
        private static readonly string[] desktopOs = { "macintosh", "x11", "windows nt" };

        /// <summary>
        /// Gets a media URL.
        /// </summary>
        /// <param name="item">The media item.</param>
        /// <returns>
        /// The media URL.
        /// </returns>
        public override string GetMediaUrl(MediaItem item)
        {
            Assert.ArgumentNotNull(item, "item");

            if (!IsImage(item))
                return base.GetMediaUrl(item);

            MediaUrlOptions mediaUrlOptions = new MediaUrlOptions();

            return GetMediaUrl(item, mediaUrlOptions);
        }

        /// <summary>
        /// Gets the media URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="mediaUrlOptions">The media URL options.</param>
        /// <returns></returns>
        public override string GetMediaUrl(MediaItem item, MediaUrlOptions mediaUrlOptions)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(mediaUrlOptions, "mediaUrlOptions");

            //If media item is not image, then return
            if (!IsImage(item))
                return base.GetMediaUrl(item, mediaUrlOptions);

            //If resolution cookie is not set
            if (!IsResolutionCookieSet())
            {
                //If mobileFirst is set to FALSE or user agent is identifying as a desktop, return with largest break-point resolution
                if (!mobileFirst || IsDesktopBrowser())
                {
                    mediaUrlOptions.MaxWidth = GetLargestResolution();
                    return base.GetMediaUrl(item, mediaUrlOptions);
                }
                //Return with mobile-first breakpoint (Smallest)
                mediaUrlOptions.MaxWidth = GetMobileFirstResolution();
                return base.GetMediaUrl(item, mediaUrlOptions);
            }

            //If Max-width is not set or Max-width is greater than the selected break-point, then set the Max-width of images to the break-point
            if (mediaUrlOptions.MaxWidth == 0 || mediaUrlOptions.MaxWidth > GetScreenResolution())
                mediaUrlOptions.MaxWidth = GetScreenResolution();

            return base.GetMediaUrl(item, mediaUrlOptions);
        }

        /// <summary>
        /// Determines whether the specified item is an image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if the specified item is image; otherwise, <c>false</c>.
        /// </returns>
        public bool IsImage(MediaItem item)
        {
            return item.MimeType.ToLower().Contains("image");
        }

        /// <summary>
        /// Resolutions the cookie is set.
        /// </summary>
        /// <returns></returns>
        public bool IsResolutionCookieSet()
        {
            return HttpContext.Current.Request.Cookies[cookieName] != null;
        }

        /// <summary>
        /// Gets the mobile first resolution.
        /// </summary>
        /// <returns></returns>
        public int GetMobileFirstResolution()
        {
            return resolutions.Min();
        }

        /// <summary>
        /// Gets the largest resolution.
        /// </summary>
        /// <returns></returns>
        public int GetLargestResolution()
        {
            return resolutions.Max();
        }

        /// <summary>
        /// Gets the break point based on the screen resolution stored in a cookie.
        /// </summary>
        /// <returns></returns>
        public int GetScreenResolution()
        {
            int resolution = 0;

            //Double check that the cookie identifying screen resolution is set
            if (HttpContext.Current.Request.Cookies[cookieName] == null)
                return resolution;

            //Get the screen resolution cookie and set the "resolution" variable to that value
            int clientWidth = 0;
            if (int.TryParse(HttpContext.Current.Request.Cookies[cookieName].Value, out clientWidth))
            {
                resolution = resolutions.OrderBy(i => i).FirstOrDefault(breakPoint => clientWidth <= breakPoint);
            }
            else
            {
                //Delete the mangled cookie
                var httpCookie = HttpContext.Current.Response.Cookies[cookieName];
                if (httpCookie != null)
                    httpCookie.Value = string.Empty;
                var cookie = HttpContext.Current.Response.Cookies[cookieName];
                if (cookie != null)
                    cookie.Expires = DateTime.Now;
            }

            return resolution;
        }

        /// <summary>
        /// Switch off mobile-first if browser is identifying as desktop
        /// NOTE: only used in the event a cookie isn't available (ex: JS is disabled)
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is desktop browser]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsDesktopBrowser()
        {
            HttpContext context = HttpContext.Current;
            if (context.Request.UserAgent == null)
                return false;

            string userAgent = context.Request.UserAgent.ToLower();
            return desktopOs.Any(userAgent.Contains);
        }

    }
}