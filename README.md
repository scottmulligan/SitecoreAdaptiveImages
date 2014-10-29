Sitecore Adaptive Images
======================

This is a Sitecore port of the popular Adaptive Images script. This module detects a visitors screen size and uses the built in Sitecore image features to deliver re-scaled versions of images if necessary. It is intended for use with responsive designs.

<b>Setup:</b>

Add this line of JavaScript as high in the head of your main layout as possible, before any other JS: 

```html
<script type="text/javascript">document.cookie = '<%= Sitecore.Configuration.Settings.GetSetting("cookieName") %>=' + Math.max(screen.width, screen.height) + '; path=/';</script>
```

The module itself  can be installed as a package through the installation wizard or the source files can be compiled and the dll and config files can be dropped into your Sitecore solution. You can download the module from the Sitecore Marketplace or GitHub.

<b>Features:</b>

<ul>
<li>Ensures you and your visitors are not wasting bandwidth delivering images at a higher resolution than the visitor needs.</li>
<li>Will work on your existing site, as it requires no changes to your mark-up.</li>
</ul>

The users screen size is detected and added as a cookie with a small line javascipt that is added to the main layout file in your Sitecore solution. Once the screen size is detected then it is possible to deliver appropriately sized images to the user.

There is a C# .NET port (https://github.com/MattWilcox/Adaptive-Images) of the popular PHP based Adaptive Images script, but this module scraps its image caching in favor of Sitecore's built in image caching.
