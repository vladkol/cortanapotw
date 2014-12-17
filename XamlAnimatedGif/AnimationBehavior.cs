/*

    This is a modified version of XamlAnimatedGif by Thomas Levesque 
    (https://github.com/thomaslevesque/XamlAnimatedGif) 

    I added two features: 
        1. Forcing transparency based on "grayscaled" value of pixels colors 
        2. Recoloring frames with specified color 

    Vladimir Kolesnikov (vladkol) 

    THE CODE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

*/

using System;
using System.IO;
using System.Threading.Tasks;
#if WPF
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
#elif WINRT
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
#endif

namespace XamlAnimatedGif
{
    public static class AnimationBehavior
    {
        #region Public attached properties and events

        #region SourceUri

#if WPF
        [AttachedPropertyBrowsableForType(typeof(Image))]
#endif
        public static Uri GetSourceUri(Image image)
        {
            return (Uri)image.GetValue(SourceUriProperty);
        }

        public static void SetSourceUri(Image image, Uri value)
        {
            image.SetValue(SourceUriProperty, value);
        }

        private static readonly DependencyProperty _SourceUriProperty =
            DependencyProperty.RegisterAttached(
              "SourceUri",
              typeof(Uri),
              typeof(AnimationBehavior),
              new PropertyMetadata(
                null,
                SourceChanged));

        public static DependencyProperty SourceUriProperty
        {
            get
            {
                return _SourceUriProperty;
            }
        }


        #endregion

        #region SourceStream

#if WPF
        [AttachedPropertyBrowsableForType(typeof(Image))]
#endif
        private static Windows.Storage.Streams.IInputStream GetSourceStream(DependencyObject obj)
        {
            return (Windows.Storage.Streams.IInputStream)obj.GetValue(SourceStreamProperty);
        }

        public static void SetSourceStream(DependencyObject obj, Windows.Storage.Streams.IInputStream value)
        {
            obj.SetValue(SourceStreamProperty, value);
        }

        private static readonly DependencyProperty _SourceStreamProperty =
            DependencyProperty.RegisterAttached(
                "SourceStream",
                typeof(Windows.Storage.Streams.IInputStream),
                typeof(AnimationBehavior),
                new PropertyMetadata(
                    null,
                    SourceChanged));

        public static DependencyProperty SourceStreamProperty
        {
            get
            {
                return _SourceStreamProperty;
            }
        }


        #endregion

        #region RepeatBehavior

#if WPF
        [AttachedPropertyBrowsableForType(typeof(Image))]
#endif
        public static RepeatBehavior GetRepeatBehavior(DependencyObject obj)
        {
            return (RepeatBehavior)obj.GetValue(RepeatBehaviorProperty);
        }

        public static void SetRepeatBehavior(DependencyObject obj, RepeatBehavior value)
        {
            obj.SetValue(RepeatBehaviorProperty, value);
        }

        private static readonly DependencyProperty _RepeatBehaviorProperty =
            DependencyProperty.RegisterAttached(
              "RepeatBehavior",
              typeof(RepeatBehavior),
              typeof(AnimationBehavior),
              new PropertyMetadata(
                default(RepeatBehavior),
                SourceChanged));

        public static DependencyProperty RepeatBehaviorProperty
        {
            get
            {
                return _RepeatBehaviorProperty;
            }
        }


        #endregion

        #region AutoStart

#if WPF
        [AttachedPropertyBrowsableForType(typeof(Image))]
#endif
        public static bool GetAutoStart(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoStartProperty);
        }

        public static void SetAutoStart(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoStartProperty, Extensions.BooleanBoxes.Box(value));
        }

        private static readonly DependencyProperty _AutoStartProperty =
            DependencyProperty.RegisterAttached(
                "AutoStart",
                typeof(bool),
                typeof(AnimationBehavior),
                new PropertyMetadata(Extensions.BooleanBoxes.TrueBox));

        public static DependencyProperty AutoStartProperty
        {
            get
            {
                return _AutoStartProperty;
            }
        }


        #endregion

        #region AnimateInDesignMode


        public static bool GetAnimateInDesignMode(DependencyObject obj)
        {
            return (bool)obj.GetValue(AnimateInDesignModeProperty);
        }

        public static void SetAnimateInDesignMode(DependencyObject obj, bool value)
        {
            obj.SetValue(AnimateInDesignModeProperty, Extensions.BooleanBoxes.Box(value));
        }

        private static readonly DependencyProperty _AnimateInDesignModeProperty =
            DependencyProperty.RegisterAttached(
                "AnimateInDesignMode",
                typeof(bool),
                typeof(AnimationBehavior),
                new PropertyMetadata(
                    Extensions.BooleanBoxes.FalseBox,
                    AnimateInDesignModeChanged));

        public static DependencyProperty AnimateInDesignModeProperty
        {
            get
            {
                return _AnimateInDesignModeProperty;
            }
        }


        #endregion

        #region Animator

        internal static Animator GetAnimator(DependencyObject obj)
        {
            return (Animator) obj.GetValue(AnimatorProperty);
        }

        private static void SetAnimator(DependencyObject obj, Animator value)
        {
            obj.SetValue(AnimatorProperty, value);
        }

        private static readonly DependencyProperty _AnimatorProperty =
            DependencyProperty.RegisterAttached(
                "Animator",
                typeof (Animator),
                typeof (AnimationBehavior),
                new PropertyMetadata(null));

        public static DependencyProperty AnimatorProperty
        {
            get
            {
                return _AnimatorProperty;
            }
        }

        #endregion

        #region Error

        public static event EventHandler<AnimationErrorEventArgs> Error;

        internal static void OnError(object sender, Exception exception, AnimationErrorKind kind)
        {
            EventHandler<AnimationErrorEventArgs> handler = Error;
            if (handler != null)
            {
                var e = new AnimationErrorEventArgs(sender, exception, kind);
                handler(sender, e);
            }
        }

        #endregion


        #region BaseColor

        public static Windows.UI.Color GetBaseColor(DependencyObject obj)
        {
            return (Windows.UI.Color)obj.GetValue(BaseColorProperty);
        }

        public static void SetBaseColor(DependencyObject obj, Windows.UI.Color value)
        {
            obj.SetValue(BaseColorProperty, value);
        }

        private static readonly DependencyProperty _BaseColorProperty =
            DependencyProperty.Register(
                "BaseColor",
                typeof(Windows.UI.Color),
                typeof(AnimationBehavior),
                new PropertyMetadata(Windows.UI.Colors.Black, 
                    BaseColorChanged));

        public static DependencyProperty BaseColorProperty
        {
            get
            {
                return _BaseColorProperty;
            }
        }


        #endregion


        #region TransparencyFromColorScale

        public static bool GetTransparencyFromColorScale(DependencyObject obj)
        {
            return (bool)obj.GetValue(TransparencyFromColorScaleProperty);
        }

        public static void SetTransparencyFromColorScale(DependencyObject obj, bool value)
        {
            obj.SetValue(TransparencyFromColorScaleProperty, Extensions.BooleanBoxes.Box(value));
        }

        private static readonly DependencyProperty _TransparencyFromColorScaleProperty =
            DependencyProperty.RegisterAttached(
                "TransparencyFromColorScale",
                typeof(bool),
                typeof(AnimationBehavior),
                new PropertyMetadata(null, ForceTransparencyChanged));

        public static DependencyProperty TransparencyFromColorScaleProperty
        {
            get
            {
                return _TransparencyFromColorScaleProperty;
            }
        }


        #endregion


        #endregion

        private static void SourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var image = o as Image;
            if (image == null)
                return;

            image.Source = null;

            image.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    InitAnimation(image);
                });
        }

        private static void BaseColorChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                var image = o as Image;
                if (image == null)
                    return;

                var animator = GetAnimator(image);
                if (animator == null)
                    return;

                animator.BaseColor = (Windows.UI.Color)e.NewValue;
            }
            catch { }
        }

        private static void ForceTransparencyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                var image = o as Image;
                if (image == null)
                    return;

                var animator = GetAnimator(image);
                if (animator == null)
                    return;

                animator.ForceTransparencyFromColorScale = e.NewValue != null ? (bool)e.NewValue : false;
            }
            catch { }
        }

        private static async void AnimateInDesignModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var image = d as Image;
            if (image == null)
                return;

            if (IsInDesignMode(image))
            {
                bool animateInDesignMode = (bool) e.NewValue;
                if (animateInDesignMode)
                {
                    await InitAnimation(image);
                }
                else
                {
                    ClearAnimatorCore(image);
                }
            }
        }

        private static bool CheckDesignMode(Image image, Uri sourceUri, Stream sourceStream)
        {
            if (IsInDesignMode(image) && !GetAnimateInDesignMode(image))
            {
                var bmp = new BitmapImage();
#if WPF
                bmp.BeginInit();
#endif
                if (sourceStream != null)
                {
#if WPF
                    bmp.StreamSource = sourceStream;
#elif WINRT
                    bmp.SetSource(sourceStream.AsRandomAccessStream());
#endif
                }
                if (sourceUri != null)
                    bmp.UriSource = sourceUri;
#if WPF
                bmp.EndInit();
#endif
                image.Source = bmp;
                return false;
            }
            return true;
        }

        private static async Task InitAnimation(Image image)
        {
            ClearAnimatorCore(image);

            var stream = GetSourceStream(image);
            if (stream != null)
            {
                await InitAnimationAsync(image, stream.AsStreamForRead(), GetRepeatBehavior(image));
            }
            else
            {
                var uri = GetSourceUri(image);
                if (uri != null)
                {
                    if (!uri.IsAbsoluteUri)
                    {
#if WPF
                        var baseUri = ((IUriContext) image).BaseUri;
#elif WINRT
                        var baseUri = image.BaseUri;
#endif
                        if (baseUri != null)
                        {
                            uri = new Uri(baseUri, uri);
                        }
                        else
                        {
                            return;
                        }
                    }
                    await InitAnimationAsync(image, uri, GetRepeatBehavior(image));
                }
            }
        }

        private static async Task InitAnimationAsync(Image image, Uri sourceUri, RepeatBehavior repeatBehavior)
        {
            if (!CheckDesignMode(image, sourceUri, null))
                return;

            try
            {
                var animator = await Animator.CreateAsync(sourceUri, image, repeatBehavior);
                SetAnimatorCore(image, animator);
            }
            catch(Exception ex)
            {
                OnError(image, ex, AnimationErrorKind.Loading);
            }
        }

        private static async Task InitAnimationAsync(Image image, Stream stream, RepeatBehavior repeatBehavior)
        {
            if (!CheckDesignMode(image, null, stream))
                return;

            try
            {
                var animator = await Animator.CreateAsync(stream, image, repeatBehavior);
                SetAnimatorCore(image, animator);
            }
            catch(Exception ex)
            {
                OnError(image, ex, AnimationErrorKind.Loading);
            }
        }

        private static void SetAnimatorCore(Image image, Animator animator)
        {
            animator.BaseColor = GetBaseColor(image);
            animator.ForceTransparencyFromColorScale = GetTransparencyFromColorScale(image);

            SetAnimator(image, animator);
            image.Source = animator.Bitmap;
            if (GetAutoStart(image))
            {
                animator.CurrentFrameIndex = 0;
                animator.Play();
            }
            else
                animator.CurrentFrameIndex = 0;
        }

        private static void ClearAnimatorCore(Image image)
        {
            image.Source = null;

            var animator = GetAnimator(image);
            if (animator == null)
                return;

            animator.Dispose();
            SetAnimator(image, null);
        }

        // ReSharper disable once UnusedParameter.Local (used in WPF)
        private static bool IsInDesignMode(DependencyObject obj)
        {
#if WPF
            return DesignerProperties.GetIsInDesignMode(obj);
#elif WINRT
            return DesignMode.DesignModeEnabled;
#endif

        }
    }
}
