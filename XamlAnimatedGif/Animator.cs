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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
#if WPF
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.IO.Packaging;
using System.Runtime.InteropServices;
#elif WINRT
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media.Animation;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources.Core;
#endif

using XamlAnimatedGif.Extensions;
using XamlAnimatedGif.Decoding;
using XamlAnimatedGif.Decompression;
using System.Diagnostics;

namespace XamlAnimatedGif
{
#if WINRT
    [Bindable]
#endif
    public class Animator : DependencyObject, IDisposable
    {
        private readonly Stream _sourceStream;
        private readonly Uri _sourceUri;
        private readonly GifDataStream _metadata;
        private readonly Image _image;
        private readonly Dictionary<int, GifPalette> _palettes;
        private readonly WriteableBitmap _bitmap;
        private readonly WriteableBitmap _bitmapToRender;
        private readonly int _stride;
        private readonly byte[] _previousBackBuffer;
        private readonly RepeatBehavior _repeatBehavior;

        private Storyboard __storyboard = null;
        private Storyboard _storyboard
        {
            get
            {
                if (__storyboard == null)
                    __storyboard = CreateStoryboard(_metadata, _repeatBehavior);
                return __storyboard;
            }
        }

        public Windows.UI.Color BaseColor
        {
            get; set;
        }

        public bool ForceTransparencyFromColorScale
        {
            get; set;
        }


        #region Constructor and factory methods

        private Animator(Stream sourceStream, Uri sourceUri, GifDataStream metadata, RepeatBehavior repeatBehavior, Image image)
        {
            _sourceStream = sourceStream;
            _sourceUri = sourceUri;
            _metadata = metadata;
            _image = image;
            _palettes = CreatePalettes(metadata);

            _bitmap = CreateBitmap(metadata);
            _bitmapToRender = CreateBitmap(metadata);

            var desc = metadata.Header.LogicalScreenDescriptor;
            _stride = 4 * ((desc.Width * 32 + 31) / 32);
            _previousBackBuffer = new byte[metadata.Header.LogicalScreenDescriptor.Height * _stride];
            _repeatBehavior = repeatBehavior;
        }

        internal static async Task<Animator> CreateAsync(Uri sourceUri, Image image, RepeatBehavior repeatBehavior = default(RepeatBehavior))
        {
            var stream = await GetStreamFromUriAsync(sourceUri);
            try
            {
                return await CreateAsync(stream, sourceUri, repeatBehavior, image);
            }
            catch
            {
                if (stream != null)
                    stream.Dispose();
                throw;
            }
        }

        internal static Task<Animator> CreateAsync(Stream sourceStream, Image image, RepeatBehavior repeatBehavior = default(RepeatBehavior))
        {
            return CreateAsync(sourceStream, null, repeatBehavior, image);
        }

        private static async Task<Animator> CreateAsync(Stream sourceStream, Uri sourceUri, RepeatBehavior repeatBehavior, Image image)
        {
            var stream = sourceStream.AsBuffered();
            var metadata = await GifDataStream.ReadAsync(stream);
            return new Animator(stream, sourceUri, metadata, repeatBehavior, image);
        }

        #endregion

        #region Animation

        public int FrameCount
        {
            get { return _metadata.Frames.Count; }
        }

        private bool _isStarted;

        public void Play()
        {
            if (_isStarted)
                _storyboard.Resume();
            else
            {
                _storyboard.Begin();
            }
            _isStarted = true;
#if WINRT
            _isPaused = false;
#endif
        }

#if WINRT
        private bool _isPaused;
#endif
        public void Pause()
        {
            if (_isStarted)
            {
                _storyboard.Pause();
#if WINRT
                _isPaused = true;
#endif
            }
        }

        public bool IsPaused
        {
            get
            {
                if (_isStarted)
                {
#if WPF
                    return _storyboard.GetIsPaused();
#elif WINRT
                    return _isPaused;
#endif
                }
                return true;
            }
        }

        public bool IsComplete
        {
            get
            {
                if (_isStarted)
                    return _storyboard.GetCurrentState() == ClockState.Filling;
                return false;
            }
        }

        public event EventHandler CurrentFrameChanged;

        private void OnCurrentFrameChanged()
        {
            EventHandler handler = CurrentFrameChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public event EventHandler AnimationCompleted;

        private void OnAnimationCompleted()
        {
            EventHandler handler = AnimationCompleted;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public int CurrentFrameIndex
        {
            get { return (int)GetValue(CurrentFrameIndexProperty); }
            internal set { SetValue(CurrentFrameIndexProperty, value); }
        }

        public static readonly DependencyProperty CurrentFrameIndexProperty =
            DependencyProperty.Register("CurrentFrameIndex", typeof(int), typeof(Animator), new PropertyMetadata(-1, CurrentFrameIndexChanged));

        bool bWasInvisible = false;

        private static async void CurrentFrameIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var animator = d as Animator;
            if (animator == null)
                return;
            animator.OnCurrentFrameChanged();

            if (animator._image.Visibility != Visibility.Visible)
                animator.bWasInvisible = true;
            else
            {
                if (animator.bWasInvisible)
                {
                    animator.bWasInvisible = false;
                    animator._storyboard.Stop();
                    animator._storyboard.Begin();
                }
                else
                    await animator.RenderFrameAsync((int)e.NewValue);
            }
        }

        private Storyboard CreateStoryboard(GifDataStream metadata, RepeatBehavior repeatBehavior)
        {
#if WPF
            var animation = new Int32AnimationUsingKeyFrames();
#elif WINRT
            var animation = new ObjectAnimationUsingKeyFrames { EnableDependentAnimation = true };
#endif
            var totalDuration = TimeSpan.Zero;
            for (int i = 0; i < metadata.Frames.Count; i++)
            {
                var frame = metadata.Frames[i];
#if WPF 
                var keyFrame = new DiscreteInt32KeyFrame(i, totalDuration);
#elif WINRT
                var keyFrame = new DiscreteObjectKeyFrame { Value = i, KeyTime = totalDuration };
#endif
                animation.KeyFrames.Add(keyFrame);
                totalDuration += GetFrameDelay(frame);
            }

            animation.RepeatBehavior =
                repeatBehavior == default(RepeatBehavior)
                    ? GetRepeatBehavior(metadata)
                    : repeatBehavior;

            Storyboard.SetTarget(animation, this);
#if WPF
            Storyboard.SetTargetProperty(animation, new PropertyPath(CurrentFrameIndexProperty));
#elif WINRT
            Storyboard.SetTargetProperty(animation, "CurrentFrameIndex");
#else
#error Not implemented
#endif

            var sb = new Storyboard
            {
                Children = { animation }
            };

            sb.Completed += (sender, e) => OnAnimationCompleted();

            return sb;
        }

        #endregion

        #region Rendering

        private WriteableBitmap CreateBitmap(GifDataStream metadata)
        {
            var desc = metadata.Header.LogicalScreenDescriptor;
#if WPF
            var bitmap = new WriteableBitmap(desc.Width, desc.Height, 96, 96, PixelFormats.Bgra32, null);
#elif WINRT
            var bitmap = new WriteableBitmap(desc.Width, desc.Height);
#else
#error Not implemented
#endif
            return bitmap;
        }

        private Dictionary<int, GifPalette> CreatePalettes(GifDataStream metadata)
        {
            var palettes = new Dictionary<int, GifPalette>();
            Color[] globalColorTable = null;
            if (metadata.Header.LogicalScreenDescriptor.HasGlobalColorTable)
            {
                globalColorTable =
                    metadata.GlobalColorTable
                        .Select(gc => Color.FromArgb(0xFF, gc.R, gc.G, gc.B))
                        .ToArray();
            }

            for (int i = 0; i < metadata.Frames.Count; i++)
            {
                var frame = metadata.Frames[i];
                var colorTable = globalColorTable;
                if (frame.Descriptor.HasLocalColorTable)
                {
                    colorTable =
                        frame.LocalColorTable
                            .Select(gc => Color.FromArgb(0xFF, gc.R, gc.G, gc.B))
                            .ToArray();
                }

                int? transparencyIndex = null;
                var gce = frame.GraphicControl;
                if (gce != null && gce.HasTransparency)
                {
                    transparencyIndex = gce.TransparencyIndex;
                }

                palettes[i] = new GifPalette(transparencyIndex, colorTable);
            }

            return palettes;
        }

        internal Task RenderingTask { get; private set; }

        private static readonly Task _completedTask = Task.FromResult(0);
        private async Task RenderFrameAsync(int frameIndex)
        {
            try
            {
                var task = RenderingTask = RenderFrameCoreAsync(frameIndex);
                await task;
            }
            catch (Exception ex)
            {
                object sender = (object)_image ?? this;
                AnimationBehavior.OnError(sender, ex, AnimationErrorKind.Rendering);
            }
            finally
            {
                RenderingTask = _completedTask;
            }
        }

        private GifFrame _previousFrame;
        private bool _isRendering;
        private async Task RenderFrameCoreAsync(int frameIndex)
        {
            if (frameIndex < 0)
                return;

            if (_isRendering)
                return;

            _isRendering = true;

            try
            {
                var frame = _metadata.Frames[frameIndex];
                var desc = frame.Descriptor;
                using (var indexStream = GetIndexStream(frame))
                {
                    DisposePreviousFrame(frame);

                    int bufferLength = 4 * desc.Width;
                    byte[] indexBuffer = new byte[desc.Width];
                    byte[] lineBuffer = new byte[bufferLength];

                    var palette = _palettes[frameIndex];
                    int transparencyIndex = palette.TransparencyIndex ?? -1;

                    var rows = frame.Descriptor.Interlace
                        ? InterlacedRows(frame.Descriptor.Height)
                        : NormalRows(frame.Descriptor.Height);

                    foreach (int y in rows)
                    {
                        int read = await indexStream.ReadAsync(indexBuffer, 0, desc.Width);
                        if (read != desc.Width)
                            throw new EndOfStreamException();

                        int offset = (desc.Top + y) * _stride + desc.Left * 4;

                        if (transparencyIndex > 0)
                        {
                            CopyFromBitmap(lineBuffer, _bitmap, offset, bufferLength);
                        }

                        for (int x = 0; x < desc.Width; x++)
                        {
                            byte index = indexBuffer[x];
                            int i = 4 * x;
                            if (index != transparencyIndex)
                            {
                                WriteColor(lineBuffer, palette[index], i);
                            }
                        }
                        CopyToBitmap(lineBuffer, _bitmap, offset, bufferLength);
                    }

                    RecolorBitmap(_bitmap, _bitmapToRender);
                    _bitmapToRender.Invalidate();

                    _previousFrame = frame;
                }
            }
            finally
            {
                _isRendering = false;
            }
        }


        void RecolorBitmap(WriteableBitmap source, WriteableBitmap dest)
        {
            Debug.Assert(source.PixelHeight == dest.PixelHeight && source.PixelWidth == dest.PixelWidth);

            bool bHasBaseColor = (BaseColor != Windows.UI.Colors.Black);

            int iSize = source.PixelWidth * source.PixelHeight * 4;
            byte[] pixelBytes = new byte[iSize];
            source.PixelBuffer.CopyTo(pixelBytes);

            if (ForceTransparencyFromColorScale || bHasBaseColor)
            {
                for (int i = 0; i < iSize; i += 4)
                {
                    if (ForceTransparencyFromColorScale && pixelBytes[i + 3] > 1)
                    {
                        pixelBytes[i + 3] = pixelBytes[i];
                    }

                    if (bHasBaseColor && pixelBytes[i + 3] > 1)
                    {
                        double bc = ((double)pixelBytes[i]) / 255.0 * BaseColor.B;
                        double gc = ((double)pixelBytes[i + 1]) / 255.0 * BaseColor.G;
                        double rc = ((double)pixelBytes[i + 2]) / 255.0 * BaseColor.R;

                        pixelBytes[i] = (byte)bc;
                        pixelBytes[i + 1] = (byte)gc;
                        pixelBytes[i + 2] = (byte)rc;
                    }
                }
            }

            dest.PixelBuffer.AsStream().Write(pixelBytes, 0, iSize);
        }

        private static IEnumerable<int> NormalRows(int height)
        {
            return Enumerable.Range(0, height);
        }

        private static IEnumerable<int> InterlacedRows(int height)
        {
            /*
             * 4 passes:
             * Pass 1: rows 0, 8, 16, 24...
             * Pass 2: rows 4, 12, 20, 28...
             * Pass 3: rows 2, 6, 10, 14...
             * Pass 4: rows 1, 3, 5, 7...
             * */
            var passes = new[]
            {
                new { Start = 0, Step = 8 },
                new { Start = 4, Step = 8 },
                new { Start = 2, Step = 4 },
                new { Start = 1, Step = 2 }
            };
            foreach (var pass in passes)
            {
                int y = pass.Start;
                while (y < height)
                {
                    yield return y;
                    y += pass.Step;
                }
            }
        }

        private static void CopyToBitmap(byte[] buffer, WriteableBitmap bitmap, int offset, int length)
        {
#if WPF
            Marshal.Copy(buffer, 0, bitmap.BackBuffer + offset, length);
#elif WINRT
            buffer.CopyTo(0, bitmap.PixelBuffer, (uint)offset, length);
#else
            #error Not implemented
#endif
        }

        private static void CopyFromBitmap(byte[] buffer, WriteableBitmap bitmap, int offset, int length)
        {
#if WPF
            Marshal.Copy(bitmap.BackBuffer + offset, buffer, 0, length);
#elif WINRT

            bitmap.PixelBuffer.CopyTo((uint)offset, buffer, 0, length);
#else
            #error Not implemented
#endif
        }

        private void WriteColor(byte[] lineBuffer, Color color, int startIndex)
        {
            lineBuffer[startIndex] = color.B;
            lineBuffer[startIndex + 1] = color.G;
            lineBuffer[startIndex + 2] = color.R;
            lineBuffer[startIndex + 3] = color.A;
        }

        private void DisposePreviousFrame(GifFrame currentFrame)
        {
            if (_previousFrame != null)
            {
                var pgce = _previousFrame.GraphicControl;
                if (pgce != null)
                {
                    switch (pgce.DisposalMethod)
                    {
                        case GifFrameDisposalMethod.None:
                        case GifFrameDisposalMethod.DoNotDispose:
                            {
                                // Leave previous frame in place
                                break;
                            }
                        case GifFrameDisposalMethod.RestoreBackground:
                            {
                                ClearArea(_previousFrame.Descriptor);
                                break;
                            }
                        case GifFrameDisposalMethod.RestorePrevious:
                            {
                                CopyToBitmap(_previousBackBuffer, _bitmap, 0, _previousBackBuffer.Length);
#if WPF
                                var desc = _metadata.Header.LogicalScreenDescriptor;
                                var rect = new Int32Rect(0, 0, desc.Width, desc.Height);
                                _bitmap.AddDirtyRect(rect);
#endif
                                break;
                            }
                        default:
                            {
                                throw new ArgumentOutOfRangeException();
                            }
                    }
                }
            }

            var gce = currentFrame.GraphicControl;
            if (gce != null && gce.DisposalMethod == GifFrameDisposalMethod.RestorePrevious)
            {
                CopyFromBitmap(_previousBackBuffer, _bitmap, 0, _previousBackBuffer.Length);
            }
        }

        private void ClearArea(IGifRect rect)
        {
            int bufferLength = 4 * rect.Width;
            byte[] lineBuffer = new byte[bufferLength];
            for (int y = 0; y < rect.Height; y++)
            {
                int offset = (rect.Top + y) * _stride + 4 * rect.Left;
                CopyToBitmap(lineBuffer, _bitmap, offset, bufferLength);
            }
#if WPF
            _bitmap.AddDirtyRect(new Int32Rect(rect.Left, rect.Top, rect.Width, rect.Height));
#endif
        }

        private Stream GetIndexStream(GifFrame frame)
        {
            var data = frame.ImageData;
            _sourceStream.Seek(data.CompressedDataStartOffset, SeekOrigin.Begin);
            var dataBlockStream = new GifDataBlockStream(_sourceStream, true);
            var lzwStream = new LzwDecompressStream(dataBlockStream, data.LzwMinimumCodeSize);
            return lzwStream;
        }

        internal BitmapSource Bitmap
        {
            get { return _bitmapToRender; }
        }

        #endregion

        #region Helper methods

#if WPF

        private static Task<Stream> GetStreamFromUriAsync(Uri uri)
        {
            if (uri.Scheme == PackUriHelper.UriSchemePack)
            {
                StreamResourceInfo sri;
                if (uri.Authority == "siteoforigin:,,,")
                    sri = Application.GetRemoteStream(uri);
                else
                    sri = Application.GetResourceStream(uri);

                if (sri != null)
                    return Task.FromResult(sri.Stream);

                throw new FileNotFoundException("Cannot find file with the specified URI");
            }
            
            if (uri.Scheme == Uri.UriSchemeFile)
            {
                return Task.FromResult<Stream>(File.OpenRead(uri.LocalPath));
            }

            throw new NotSupportedException("Only pack: and file: URIs are supported");
        }
#elif WINRT
        private static async Task<Stream> GetStreamFromUriAsync(Uri uri)
        {
            if (uri.Scheme == "ms-appx" || uri.Scheme == "ms-appdata")
            {
                var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                return await file.OpenStreamForReadAsync();
            }
            if (uri.Scheme == "ms-resource")
            {
                var rm = ResourceManager.Current;
                var context = ResourceContext.GetForCurrentView();
                var candidate = rm.MainResourceMap.GetValue(uri.LocalPath, context);
                if (candidate != null && candidate.IsMatch)
                {
                    var file = await candidate.GetValueAsFileAsync();
                    return await file.OpenStreamForReadAsync();
                }
                throw new Exception("Resource not found");
            }
            if (uri.Scheme == "file")
            {
                var file = await StorageFile.GetFileFromPathAsync(uri.LocalPath);
                return await file.OpenStreamForReadAsync();
            }
            if(uri.Scheme == "http" || uri.Scheme == "https")
            {
                System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
                var response = await client.GetAsync(uri);
                using (Stream s = await response.Content.ReadAsStreamAsync())
                {
                    MemoryStream ms = new MemoryStream((int)s.Length);
                    s.CopyTo(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    return ms;
                }
            }
            throw new NotSupportedException("Only http:, https:, ms-appx:, ms-appdata:, ms-resource: and file: URIs are supported");
        }
#endif

        private static TimeSpan GetFrameDelay(GifFrame frame)
        {
            var gce = frame.GraphicControl;
            if (gce != null)
            {
                if (gce.Delay != 0)
                    return TimeSpan.FromMilliseconds(gce.Delay);
            }
            return TimeSpan.FromMilliseconds(100);
        }

        private RepeatBehavior GetRepeatBehavior(GifDataStream metadata)
        {
            if (metadata.RepeatCount == 0)
                return RepeatBehavior.Forever;
            return new RepeatBehavior(metadata.RepeatCount);
        }

        #endregion

        #region Finalizer and Dispose

        ~Animator()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;
        public void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _storyboard.Stop();
                    _storyboard.Children.Clear();
                    _sourceStream.Dispose();
                }
                _disposed = true;
            }
        }

        #endregion

        public override sealed string ToString() 
        {
            string s = _sourceUri != null ? _sourceUri.ToString() : _sourceStream.ToString();
            return "GIF: " + s;
        }

        class GifPalette
        {
            private readonly int? _transparencyIndex;
            private readonly Color[] _colors;

            public GifPalette(int? transparencyIndex, Color[] colors)
            {
                _transparencyIndex = transparencyIndex;
                _colors = colors;
            }

            public int? TransparencyIndex
            {
                get { return _transparencyIndex; }
            }

            public Color this[int i]
            {
                get { return _colors[i]; }
            }
        }
    }
}
