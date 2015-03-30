using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace PickOfTheWeek
{
    // Use this class for converting CortanaMode values to gif paths from Assets/CortanaAnimations 
    // I am not currently using this class in PickOfTheWeek project
    public sealed class CortanaModeToUriConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return null;

            string resultString = null;

            switch ((CortanaMode)value)
            {
                case CortanaMode.Calm:
                    resultString = "circle_calm";
                    break;
                case CortanaMode.Listening:
                    resultString = "circle_listening";
                    break;
                case CortanaMode.Speaking:
                    resultString = "circle_speaking";
                    break;
                case CortanaMode.Thinking:
                    resultString = "circle_thinking";
                    break;
                case CortanaMode.Reminder:
                    resultString = "circle_reminder";
                    break;
                case CortanaMode.Considerate:
                    resultString = "circle_considerate";
                    break;
                case CortanaMode.Optimistic:
                    resultString = "circle_optimistic";
                    break;
                case CortanaMode.Greeting:
                    resultString = "circle_greeting";
                    break;
                case CortanaMode.Greeting2:
                    resultString = "circle_greeting2";
                    break;
                case CortanaMode.Abashed:
                    resultString = "circle_abashed";
                    break;
                default: // including CortanaMode.None
                    resultString = "circle_calm";
                    break;
            }

            return new Uri(String.Format("ms-appx:///Assets/CortanaAnimations/{0}.gif", resultString)); ;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return CortanaMode.None;
        }

    }
}