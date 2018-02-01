using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreGraphics;
using Foundation;
using MasterDetailDemo;
using MasterDetailDemo.iOS;
using MdpRight.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(MyMasterDetailPage), typeof(MyMasterDetailPageRenderer))]
namespace MasterDetailDemo.iOS
{
    public class MyMasterDetailPageRenderer : MyPhoneMasterDetailRenderer
    {
        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);

            
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();
            
        }
    }
}