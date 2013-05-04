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
using Sitecore;

namespace adaptiveImages
{
    public class AdaptiveImagesMediaProvider : MediaProvider
    {

        /// <summary>
        /// The resolution break-points to use (screen widths, in pixels)
        /// </summary>
        private readonly int[] _resolutions = Sitecore.Configuration.Settings.GetSetting("resolutions").Split(',').Select(int.Parse).ToArray();

        /// <summary>
        /// The maximum width that images will be rendered at (Optional)
        /// </summary>
        private readonly string _maxWidth = Sitecore.Configuration.Settings.GetSetting("maxWidth");

        /// <summary>
        /// If there's no cookie FALSE sends the largest var resolutions version (TRUE sends smallest)
        /// </summary>
        private readonly bool _mobileFirst = String.Compare(Sitecore.Configuration.Settings.GetSetting("mobileFirst"), "true", System.StringComparison.OrdinalIgnoreCase) == 0;

        /// <summary>
        /// The name of the cookie containing the resolution value
        /// </summary>
        private readonly string _cookieName = Sitecore.Configuration.Settings.GetSetting("cookieName");

        /// <summary>
        /// The name of the live database (ie: web)
        /// </summary>
        private readonly string _database = Sitecore.Configuration.Settings.GetSetting("database");

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

            //If media item is not an image or the page context is not normal, then return
            if (!IsImage(item) || !Sitecore.Context.PageMode.IsNormal)
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

            //If media item is not an image or the page context is not normal, then return
            if (!IsImage(item) || !Context.PageMode.IsNormal || Context.Database == null || Context.Database.Name != _database)
                return base.GetMediaUrl(item, mediaUrlOptions);

            //If resolution cookie is not set
            if (!IsResolutionCookieSet())
            {
                //If mobileFirst is set to FALSE or user agent is identifying as a desktop, return with largest break-point resolution
                if (!_mobileFirst || IsDesktopBrowser())
                {
                    mediaUrlOptions.MaxWidth = GetLargestBreakpoint();
                    return base.GetMediaUrl(item, mediaUrlOptions);
                }
                //Return with mobile-first breakpoint (Smallest)
                mediaUrlOptions.MaxWidth = GetMobileFirstBreakpoint();
                return base.GetMediaUrl(item, mediaUrlOptions);
            }

            // If Max-width is not set or Max-width is greater than the selected break-point, then set the Max-width to the break-point
            if (mediaUrlOptions.MaxWidth == 0 || mediaUrlOptions.MaxWidth > GetScreenResolution())
                mediaUrlOptions.MaxWidth = GetScreenResolution();

            // If Max-width is not set and the 'maxWidth' setting is not empty, then set the Max-width property to the maxWidth
            if (mediaUrlOptions.MaxWidth == 0 && !string.IsNullOrEmpty(_maxWidth))
            {
                int maxWidth = 0;
                if (int.TryParse(_maxWidth, out maxWidth))
                {
                    // If pixel ratio is normal
                    if (GetCookiePixelDensity() == 1)
                        mediaUrlOptions.MaxWidth = maxWidth;
                    else
                        mediaUrlOptions.MaxWidth = maxWidth * GetCookiePixelDensity();
                }

            }

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
            return HttpContext.Current.Request.Cookies[_cookieName] != null;
        }

        /// <summary>
        /// Gets the mobile first resolution.
        /// </summary>
        /// <returns></returns>
        public int GetMobileFirstBreakpoint()
        {
            return _resolutions.Min();
        }

        /// <summary>
        /// Gets the largest resolution.
        /// </summary>
        /// <returns></returns>
        public int GetLargestBreakpoint()
        {
            return _resolutions.Max();
        }

        /// <summary>
        /// Gets the break point based on the screen resolution stored in a cookie.
        /// </summary>
        /// <returns></returns>
        public int GetScreenResolution()
        {
            int resolution = 0;

            // Double check that the cookie identifying screen resolution is set
            if (!IsResolutionCookieSet())
                return resolution;

            // Get the screen resolution cookie value
            int clientWidth = GetCookieResolution();

            if (clientWidth != 0)
            {
                // Get the screen pixel density ratio cookie value
                int clientPixelDensity = GetCookiePixelDensity();

                // if pixel density is 1 (normal), then return the appropriate resolution break point, else we need some more logic
                if (clientPixelDensity == 1)
                {
                    resolution = _resolutions.OrderBy(i => i).FirstOrDefault(breakPoint => clientWidth <= breakPoint);
                }
                else
                {
                    int totalWidth = clientWidth * clientPixelDensity; // Required physical pixel width of the image
                    // Try to fit into a breakpoint while ignoring the multiplier
                    foreach (int breakpoint in _resolutions.Where(breakpoint => totalWidth <= breakpoint))
                    {
                        resolution = breakpoint;
                    }
                    // Check if the required image width (including multiplier) is bigger than any existing breakpoint value
                    if (totalWidth > GetLargestBreakpoint())
                    {
                        resolution = resolution * clientPixelDensity;
                    }
                }
            }
            else
            {
                // Delete the mangled cookie
                var httpCookie = HttpContext.Current.Response.Cookies[_cookieName];
                if (httpCookie != null)
                    httpCookie.Value = string.Empty;
                var cookie = HttpContext.Current.Response.Cookies[_cookieName];
                if (cookie != null)
                    cookie.Expires = DateTime.Now;
            }

            return resolution;
        }

        /// <summary>
        /// Gets the cookie resolution.
        /// </summary>
        /// <returns></returns>
        public int GetCookieResolution()
        {
            // Double check that the cookie identifying screen resolution is set
            if (!IsResolutionCookieSet())
                return 0;

            // Split the cookie into resolution and pixel density ratio
            string[] cookieResolution = HttpContext.Current.Request.Cookies[_cookieName].Value.Split(',');
            // If we were able to get the cookie resolution
            if (cookieResolution.Length > 0)
            {
                int clientWidth = 0;
                if (int.TryParse(cookieResolution[0], out clientWidth))
                    return clientWidth;
            }

            return 0;
        }

        /// <summary>
        /// Gets the cookie pixel density.
        /// </summary>
        /// <returns></returns>
        public int GetCookiePixelDensity()
        {
            // Double check that the cookie identifying screen resolution is set
            if (!IsResolutionCookieSet())
                return 1;

            // Split the cookie into resolution and pixel density ratio
            string[] cookieResolution = HttpContext.Current.Request.Cookies[_cookieName].Value.Split(',');

            // If we were able to get the cookie pixel density ratio
            if (cookieResolution.Length > 1)
            {
                int clientPixelDensity = 0;
                if (int.TryParse(cookieResolution[1], out clientPixelDensity))
                    return clientPixelDensity;
            }

            return 1;
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