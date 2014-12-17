using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XamlAnimatedGif.Extensions
{
    /// <summary>
    /// Helps boxing Booolean values.
    /// </summary>
    internal static class BooleanBoxes
    {
        /// <summary>
        /// Gets a boxed representation for Boolean's "true" value.
        /// </summary>
        public static readonly object TrueBox;

        /// <summary>
        /// Gets a boxed representation for Boolean's "false" value.
        /// </summary>
        public static readonly object FalseBox;

        /// <summary>
        /// Initializes the <see cref="BooleanBoxes"/> class.
        /// </summary>
        static BooleanBoxes()
        {
            TrueBox = true;
            FalseBox = false;
        }

        /// <summary>
        /// Returns a boxed representation for the specified Boolean value.
        /// </summary>
        /// <param name="value">The value to box.</param>
        /// <returns></returns>
        public static object Box(bool value)
        {
            if (value)
            {
                return TrueBox;
            }

            return FalseBox;
        }
    }
}
