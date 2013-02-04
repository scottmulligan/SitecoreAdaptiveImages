namespace adaptiveImages
{
    public class AdaptiveImagesMediaProviderHook : Sitecore.Events.Hooks.IHook
    {
        public void Initialize()
        {
            //Sitecore.Diagnostics.Log.Info("Initalizing AdaptiveImagesMediaProviderHook", this);
            Sitecore.Resources.Media.MediaManager.Provider = new AdaptiveImagesMediaProvider();
            //Sitecore.Diagnostics.Log.Info("AdaptiveImagesMediaProviderHook initialized", this);
        }
    }
}