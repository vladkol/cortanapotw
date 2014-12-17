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

namespace XamlAnimatedGif
{
    public sealed class AnimationErrorEventArgs 
    {
        private readonly Exception _exception;
        private readonly AnimationErrorKind _kind;
        private readonly object _sender;

        public AnimationErrorEventArgs(object sender, Exception exception, AnimationErrorKind kind) 
        {
            _exception = exception;
            _kind = kind;
            _sender = sender;
        }

        public Exception Exception
        {
            get { return _exception; }
        }

        public AnimationErrorKind Kind
        {
            get { return _kind; }
        }

        public object Sender
        {
            get { return _sender; }
        }
    }

    public enum AnimationErrorKind
    {
        Loading,
        Rendering
    }
}
