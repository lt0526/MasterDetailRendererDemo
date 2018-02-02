﻿using System;
using System.ComponentModel;
using System.Linq;
using UIKit;
using PointF = CoreGraphics.CGPoint;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using MdpRight.iOS;
using MasterDetailDemo;

[assembly: ExportRenderer(typeof(MasterDetailPage), typeof(MyPhoneMasterDetailRenderer), UIUserInterfaceIdiom.Phone)]
namespace MdpRight.iOS
{
	public class MyPhoneMasterDetailRenderer : UIViewController, IVisualElementRenderer, IEffectControlProvider
	{
		UIView _clickOffView;
		UIViewController _detailController;

		bool _disposed;
		EventTracker _events;

		UIViewController _masterController;

		UIPanGestureRecognizer _panGesture;

		bool _presented;
		UIGestureRecognizer _tapGesture;

		VisualElementTracker _tracker;

		IPageController PageController => Element as IPageController;

        float widthRatio = (float)0.2;

		public MyPhoneMasterDetailRenderer()
		{
			
		}

		IMasterDetailPageController MasterDetailPageController => Element as IMasterDetailPageController;

		bool Presented
		{
			get { return _presented; }
			set
			{
				if(_presented == value)
					return;
				_presented = value;
				LayoutChildren(true);
				if(value)
					AddClickOffView();
				else
					RemoveClickOffView();

				((IElementController)Element).SetValueFromRenderer(MasterDetailPage.IsPresentedProperty, value);
			}
		}

		public VisualElement Element { get; private set; }

		public event EventHandler<VisualElementChangedEventArgs> ElementChanged;

		public SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			return NativeView.GetSizeRequest(widthConstraint, heightConstraint);
		}

		public UIView NativeView
		{
			get { return View; }
		}

		public void SetElement(VisualElement element)
		{
			var oldElement = Element;
			Element = element;
			Element.SizeChanged += PageOnSizeChanged;

			_masterController = new ChildViewController();
			_detailController = new ChildViewController();

			_clickOffView = new UIView();
			_clickOffView.BackgroundColor = new Color(0, 0, 0, 0).ToUIColor();

			Presented = ((MasterDetailPage)Element).IsPresented;

			OnElementChanged(new VisualElementChangedEventArgs(oldElement, element));

            widthRatio = ((MyMasterDetailPage)element).WidthRatio;

			
			var tEffectUtilities = typeof(PlatformEffect).Assembly.GetTypes().FirstOrDefault(x => x.Name.EndsWith("EffectUtilities"));
			tEffectUtilities.InvokeMember("RegisterEffectControlProvider", System.Reflection.BindingFlags.InvokeMethod,
										  null, null, new object[] { this, oldElement, element });

			
			var sendViewInitialized = typeof(Forms).GetMethod("SendViewInitialized", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			sendViewInitialized.Invoke(null, new object[] { element, NativeView});
		}

		public void SetElementSize(Size size)
		{
            Element.Layout(new Rectangle(Element.X, Element.Y, size.Width, size.Height));
        }

        public UIViewController ViewController
		{
			get { return this; }
		}

		public override void ViewDidAppear(bool animated)
		{
			base.ViewDidAppear(animated);
			PageController.SendAppearing();
		}

		public override void ViewDidDisappear(bool animated)
		{
			base.ViewDidDisappear(animated);
			PageController.SendDisappearing();
		}

		public override void ViewDidLayoutSubviews()
		{
			base.ViewDidLayoutSubviews();

			LayoutChildren(false);
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			_tracker = new VisualElementTracker(this);
			_events = new EventTracker(this);
			_events.LoadEvents(View);

			((MasterDetailPage)Element).PropertyChanged += HandlePropertyChanged;

			_tapGesture = new UITapGestureRecognizer(() =>
			{
				if(Presented)
					Presented = false;
			});
			_clickOffView.AddGestureRecognizer(_tapGesture);

			PackContainers();
			UpdateMasterDetailContainers();

			UpdateBackground();

			UpdatePanGesture();
		}

		public override void WillRotate(UIInterfaceOrientation toInterfaceOrientation, double duration)
		{
			if(!MasterDetailPageController.ShouldShowSplitMode && _presented)
				Presented = false;

			base.WillRotate(toInterfaceOrientation, duration);
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing && !_disposed)
			{
				Element.SizeChanged -= PageOnSizeChanged;
				Element.PropertyChanged -= HandlePropertyChanged;

				if(_tracker != null)
				{
					_tracker.Dispose();
					_tracker = null;
				}

				if(_events != null)
				{
					_events.Dispose();
					_events = null;
				}

				if(_tapGesture != null)
				{
					if(_clickOffView != null && _clickOffView.GestureRecognizers.Contains(_panGesture))
					{
						_clickOffView.GestureRecognizers.Remove(_tapGesture);
						_clickOffView.Dispose();
					}
					_tapGesture.Dispose();
				}
				if(_panGesture != null)
				{
					if(View != null && View.GestureRecognizers.Contains(_panGesture))
						View.GestureRecognizers.Remove(_panGesture);
					_panGesture.Dispose();
				}

				EmptyContainers();

				PageController.SendDisappearing();

				_disposed = true;
			}

			base.Dispose(disposing);
		}

		protected virtual void OnElementChanged(VisualElementChangedEventArgs e)
		{
			var changed = ElementChanged;
			if(changed != null)
				changed(this, e);
		}

		void AddClickOffView()
		{
			View.Add(_clickOffView);
			_clickOffView.Frame = _detailController.View.Frame;
		}

		void EmptyContainers()
		{
			foreach(var child in _detailController.View.Subviews.Concat(_masterController.View.Subviews))
				child.RemoveFromSuperview();

			foreach(var vc in _detailController.ChildViewControllers.Concat(_masterController.ChildViewControllers))
				vc.RemoveFromParentViewController();
		}

		void HandleMasterPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if(e.PropertyName == Page.IconProperty.PropertyName || e.PropertyName == Page.TitleProperty.PropertyName)
				MessagingCenter.Send<IVisualElementRenderer>(this, "Xamarin.UpdateToolbarButtons");
		}

void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
{
	if(e.PropertyName == "Master" || e.PropertyName == "Detail")
		UpdateMasterDetailContainers();
	else if(e.PropertyName == MasterDetailPage.IsPresentedProperty.PropertyName)
		Presented = ((MasterDetailPage)Element).IsPresented;
	else if(e.PropertyName == MasterDetailPage.IsGestureEnabledProperty.PropertyName)
		UpdatePanGesture();
	else if(e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
		UpdateBackground();
	else if(e.PropertyName == Page.BackgroundImageProperty.PropertyName)
		UpdateBackground();
    else if(e.PropertyName == "WidthRatio")
    {
        widthRatio = ((MyMasterDetailPage)Element).WidthRatio;
    }
}

		void LayoutChildren(bool animated)
		{
			var frame = Element.Bounds.ToRectangleF();
			var masterFrame = frame;
            masterFrame.Width = (int)(Math.Min(masterFrame.Width, masterFrame.Height) * widthRatio);


			_masterController.View.Frame = masterFrame;

			var target = frame;

            if (Presented)
                target.X += masterFrame.Width;


            if (animated)
			{
				UIView.BeginAnimations("Flyout");
				var view = _detailController.View;
				view.Frame = target;
				UIView.SetAnimationCurve(UIViewAnimationCurve.EaseOut);
				UIView.SetAnimationDuration(250);
				UIView.CommitAnimations();
			}
			else
				_detailController.View.Frame = target;

            MasterDetailPageController.MasterBounds = new Rectangle(0, 0, masterFrame.Width, masterFrame.Height);
            MasterDetailPageController.DetailBounds = new Rectangle(0, 0, frame.Width, frame.Height);

			if(Presented)
				_clickOffView.Frame = _detailController.View.Frame;
		}

		void PackContainers()
		{
			_detailController.View.BackgroundColor = new UIColor(1, 1, 1, 1);
			View.AddSubview(_masterController.View);
			View.AddSubview(_detailController.View);

			AddChildViewController(_masterController);
			AddChildViewController(_detailController);
		}

		void PageOnSizeChanged(object sender, EventArgs eventArgs)
		{
			LayoutChildren(false);
		}

		void RemoveClickOffView()
		{
			_clickOffView.RemoveFromSuperview();
		}

		void UpdateBackground()
		{
			if(!string.IsNullOrEmpty(((Page)Element).BackgroundImage))
				View.BackgroundColor = UIColor.FromPatternImage(UIImage.FromBundle(((Page)Element).BackgroundImage));
			else if(Element.BackgroundColor == Color.Default)
				View.BackgroundColor = UIColor.White;
			else
				View.BackgroundColor = Element.BackgroundColor.ToUIColor();
		}

		void UpdateMasterDetailContainers()
		{
			((MasterDetailPage)Element).Master.PropertyChanged -= HandleMasterPropertyChanged;

			EmptyContainers();

			if(Platform.GetRenderer(((MasterDetailPage)Element).Master) == null)
				Platform.SetRenderer(((MasterDetailPage)Element).Master, Platform.CreateRenderer(((MasterDetailPage)Element).Master));
			if(Platform.GetRenderer(((MasterDetailPage)Element).Detail) == null)
				Platform.SetRenderer(((MasterDetailPage)Element).Detail, Platform.CreateRenderer(((MasterDetailPage)Element).Detail));

			var masterRenderer = Platform.GetRenderer(((MasterDetailPage)Element).Master);
			var detailRenderer = Platform.GetRenderer(((MasterDetailPage)Element).Detail);

			((MasterDetailPage)Element).Master.PropertyChanged += HandleMasterPropertyChanged;

			_masterController.View.AddSubview(masterRenderer.NativeView);
			_masterController.AddChildViewController(masterRenderer.ViewController);

			_detailController.View.AddSubview(detailRenderer.NativeView);
			_detailController.AddChildViewController(detailRenderer.ViewController);
		}

		void UpdatePanGesture()
		{
			var model = (MasterDetailPage)Element;
			if(!model.IsGestureEnabled)
			{
				if(_panGesture != null)
					View.RemoveGestureRecognizer(_panGesture);
				return;
			}

			if(_panGesture != null)
			{
				View.AddGestureRecognizer(_panGesture);
				return;
			}

			UITouchEventArgs shouldRecieve = (g, t) => !(t.View is UISlider);
			var center = new PointF();
			_panGesture = new UIPanGestureRecognizer(g =>
			{
				switch(g.State)
				{
				case UIGestureRecognizerState.Began:
					center = g.LocationInView(g.View);
					break;
				case UIGestureRecognizerState.Changed:
					var currentPosition = g.LocationInView(g.View);
					var motion = currentPosition.X - center.X;
					var detailView = _detailController.View;
					var targetFrame = detailView.Frame;

                        if (Presented)
                            targetFrame.X = (nfloat)Math.Max(0, _masterController.View.Frame.Width + Math.Min(0, motion));
                        else
                            targetFrame.X = (nfloat)Math.Min(_masterController.View.Frame.Width, Math.Max(0, motion));

                        detailView.Frame = targetFrame;
					break;
				case UIGestureRecognizerState.Ended:
					var detailFrame = _detailController.View.Frame;
					var masterFrame = _masterController.View.Frame;
					if(Presented)
					{

                        if (detailFrame.X < masterFrame.Width * .75)
                            Presented = false;
                        else
                            LayoutChildren(true);
                    }
                    else
					{
						
						if(detailFrame.X > masterFrame.Width * .25)
							Presented = true;
						else
							LayoutChildren(true);
					}
					break;
				}
			});
			_panGesture.ShouldReceiveTouch = shouldRecieve;
			_panGesture.MaximumNumberOfTouches = 2;
			View.AddGestureRecognizer(_panGesture);
		}

		class ChildViewController : UIViewController
		{
			public override void ViewDidLayoutSubviews()
			{
				foreach(var vc in ChildViewControllers)
					vc.View.Frame = View.Bounds;
			}
		}

		void IEffectControlProvider.RegisterEffect(Effect effect)
		{
			var platformEffect = effect as PlatformEffect;
			var propInfo = typeof(PlatformEffect).GetProperty("Container", System.Reflection.BindingFlags.NonPublic);
			propInfo.SetValue(platformEffect, View);
		}
	}

    #region ArrayExtensions
    internal static class ArrayExtensions
    {
        public static T[] Insert<T>(this T[] self, int index, T item)
        {
            var result = new T[self.Length + 1];
            if (index > 0)
                Array.Copy(self, result, index);

            result[index] = item;

            if (index < self.Length)
                Array.Copy(self, index, result, index + 1, result.Length - index - 1);

            return result;
        }

        public static T[] Remove<T>(this T[] self, T item)
        {
            return self.RemoveAt(Array.IndexOf(self, item));
        }

        public static T[] RemoveAt<T>(this T[] self, int index)
        {
            var result = new T[self.Length - 1];
            if (index > 0)
                Array.Copy(self, result, index);

            if (index < self.Length - 1)
                Array.Copy(self, index + 1, result, index, self.Length - index - 1);

            return result;
        }
    }
    #endregion

}

