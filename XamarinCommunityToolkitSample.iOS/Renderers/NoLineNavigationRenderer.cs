﻿using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Microsoft.Toolkit.Xamarin.Forms.Sample.iOS.Renderers;
using Microsoft.Toolkit.Xamarin.Forms.Sample.Pages;

[assembly: ExportRenderer(typeof(BaseNavigationPage), typeof(NoLineNavigationPageRenderer))]
namespace Microsoft.Toolkit.Xamarin.Forms.Sample.iOS.Renderers
{
    public class NoLineNavigationPageRenderer : NavigationRenderer
    {
        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            if (!(Element is NavigationPage))
                return;

            //iOS older version fix
            NavigationBar.SetBackgroundImage(new UIKit.UIImage(), UIKit.UIBarMetrics.Default);
            NavigationBar.ShadowImage = new UIKit.UIImage();
            NavigationBar.ClipsToBounds = true;

            //Newest iOS version fix - trycatch isn't optimal
            try
            {
                NavigationBar.ScrollEdgeAppearance.ShadowImage = new UIKit.UIImage();
                NavigationBar.ScrollEdgeAppearance.ShadowColor = null;
            }
            catch (Exception)
            {
                // Supressed because we don't really mind since it's the sample app.
            }
        }
    }
}
