using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


namespace PickOfTheWeek
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool _configMode = false;
        private CortanaMode _CortanaCurrentMode = CortanaMode.None;
        private double _actualConfigBlockHeight = -1;

        private MediaElement mediaAudio
        {
            get
            {
                return mediaPlayer;
            }
        }

 
        public CortanaMode CortanaCurrentMode
        {
            get { return _CortanaCurrentMode; }
            private set
            {
                _CortanaCurrentMode = value;

                //I don't use one control with different modes by performance reasons. 
                //It was too slow and blinking when switching between gifs, so I decided to go with multiple controls 

                cortanaThinking.Visibility = Visibility.Collapsed;
                cortanaGreeting.Visibility = Visibility.Collapsed;
                cortanaSpeaking.Visibility = Visibility.Collapsed;

                switch (_CortanaCurrentMode)
                {
                    case CortanaMode.Thinking:
                        cortanaThinking.Visibility = Visibility.Visible;
                        break;
                    case CortanaMode.Speaking:
                        cortanaSpeaking.Visibility = Visibility.Visible;
                        break;
                    case CortanaMode.Greeting:
                        cortanaGreeting.Visibility = Visibility.Visible;
                        break;
                    default:
                        break;
                }
            }
        }

        public MainPage()
        {
            this.InitializeComponent();

            Windows.UI.ViewManagement.StatusBar.GetForCurrentView().HideAsync();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            cortana.Tapped += cortana_Tapped;
            cortana.Holding += cortana_Holding;

            Windows.UI.Color accentColor = (Application.Current.Resources["PhoneAccentBrush"] as SolidColorBrush).Color;

            cortanaThinking.SetValue(XamlAnimatedGif.AnimationBehavior.BaseColorProperty, accentColor);
            cortanaGreeting.SetValue(XamlAnimatedGif.AnimationBehavior.BaseColorProperty, accentColor);
            cortanaSpeaking.SetValue(XamlAnimatedGif.AnimationBehavior.BaseColorProperty, accentColor);

            cortanaThinking.SetValue(XamlAnimatedGif.AnimationBehavior.TransparencyFromColorScaleProperty, true);
            cortanaGreeting.SetValue(XamlAnimatedGif.AnimationBehavior.TransparencyFromColorScaleProperty, true);
            cortanaSpeaking.SetValue(XamlAnimatedGif.AnimationBehavior.TransparencyFromColorScaleProperty, true);

            mediaAudio.MediaEnded += mediaAudio_MediaEnded;

            var inputPane = Windows.UI.ViewManagement.InputPane.GetForCurrentView();
            inputPane.Showing += InputPane_Showing;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                mediaAudio.Source = null;
                CortanaCurrentMode = CortanaMode.Thinking;

                imageSplash.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                textHeader.Focus(FocusState.Pointer);
                LoadConfig();

                if (HasMinimalConfig() &&
                    e != null && e.Parameter != null && e.Parameter.ToString() == "PickOfTheWeek")
                {
                    SetMode(false);
                    StartSpeech();
                }
                else
                {
                    SetMode(true);
                }
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Break();
                }
                else
                {
                    await new Windows.UI.Popups.MessageDialog(ex.Message).ShowAsync();
                }
            }
        }

#region Event Handlers
        private void mediaAudio_MediaEnded(object sender, RoutedEventArgs e)
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                //mediaAudio.MediaEnded -= mediaAudio_MediaEnded;
                CortanaCurrentMode = CortanaMode.Greeting;
            });
        }

        private async void cortana_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SpeechSynthesizer synt = new SpeechSynthesizer();
            SpeechSynthesisStream syntStream = await synt.SynthesizeTextToStreamAsync("You are welcome!");

            CortanaCurrentMode = CortanaMode.Speaking;
            mediaAudio.SetSource(syntStream, syntStream.ContentType);
        }

        private void cortana_Holding(object sender, HoldingRoutedEventArgs e)
        {
            SetMode(true);
        }


        private void InputPane_Showing(Windows.UI.ViewManagement.InputPane sender, Windows.UI.ViewManagement.InputPaneVisibilityEventArgs args)
        {
            if (_actualConfigBlockHeight <= 0)
                _actualConfigBlockHeight = config.ActualHeight;

            double needH = _actualConfigBlockHeight + args.OccludedRect.Height;
            if (scroll.ScrollableHeight < needH)
                scrollContainer.MinHeight = needH;
        }


        private async void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
            await new Windows.UI.Popups.MessageDialog("Saved!", "Configuration").ShowAsync();
        }

        private async void buttonSaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
            SetMode(false);
            await new Windows.UI.Popups.MessageDialog("Saved!", "Configuration").ShowAsync();
            Application.Current.Exit();
        }
#endregion

        private async Task InitializeCortana()
        {
            Uri uri = new Uri("ms-appx:///TWC9.xml", UriKind.Absolute);

            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            string xmlVCD = await FileIO.ReadTextAsync(file);

            xmlVCD = xmlVCD.Replace("%USERNAME%", textUser.Text);

            StorageFile configuredFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("configuredVCD.xml", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(configuredFile, xmlVCD);

            // Install voice commands
            try
            {
                await VoiceCommandManager.InstallCommandSetsFromStorageFileAsync(configuredFile);
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format(
                    "An error was encountered installing the Voice Command Definition file: \r\n {0:x} \r\n {1}",
                    ex.HResult,
                    ex.Message);
                await new MessageDialog(errorMessage).ShowAsync();
            }
        }


        void SetMode(bool isConfig)
        {
            if (isConfig == _configMode)
                return;

            _configMode = isConfig;
            if (_configMode)
            {
                content.Visibility = Visibility.Collapsed;
                cortana.Visibility = Visibility.Collapsed;
                configBlock.Visibility = Windows.UI.Xaml.Visibility.Visible;

                if (!topContainer.Children.Contains(configBlock))
                    topContainer.Children.Add(configBlock);
            }
            else
            {
                content.Visibility = Visibility.Visible;
                configBlock.Visibility = Visibility.Collapsed;
                cortana.Visibility = Visibility.Visible;

                if (topContainer.Children.Contains(configBlock))
                    topContainer.Children.Remove(configBlock);
            }
        }

        private async Task StartSpeech()
        {
            try
            {
                if (textSSML.Text.Trim().Length == 0)
                    return;

                SpeechSynthesizer synt = new SpeechSynthesizer();

                string ssmlOrText = textSSML.Text.Trim();
                SpeechSynthesisStream syntStream = null;
                if (!ssmlOrText.StartsWith("<speak "))
                    syntStream = await synt.SynthesizeTextToStreamAsync(ssmlOrText);
                else
                    syntStream = await synt.SynthesizeSsmlToStreamAsync(ssmlOrText);

                CortanaCurrentMode = CortanaMode.Speaking;
                mediaAudio.SetSource(syntStream, syntStream.ContentType);
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Break();
                }
                else
                {
                    await new Windows.UI.Popups.MessageDialog(ex.Message).ShowAsync();
                }
            }
        }

        private bool HasMinimalConfig()
        {
            IPropertySet settings = ApplicationData.Current.LocalSettings.Values;
            return (settings.ContainsKey("Username") && settings.ContainsKey("SSML"));
        }

        private void LoadConfig()
        {
            IPropertySet settings = ApplicationData.Current.LocalSettings.Values;
            
            if (settings.ContainsKey("Username"))
                textUser.Text = settings["Username"].ToString();
            else
                textUser.Text = "my friend";
            
            if (settings.ContainsKey("Header"))
                textHeader.Text = settings["Header"].ToString();
            else
                textHeader.Text = string.Empty;

            if (settings.ContainsKey("Title"))
                textTitle.Text = settings["Title"].ToString();
            else
                textTitle.Text = string.Empty;

            if (settings.ContainsKey("URL"))
                textURL.Text = settings["URL"].ToString();
            else
                textURL.Text = string.Empty;

            if (settings.ContainsKey("SSML"))
                textSSML.Text = settings["SSML"].ToString();
            else
                textSSML.Text = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">\n" +
                                "<prosody rate=\"1.0\"> \n\n" +
                                "\n</prosody>\n</speak>";

            if (settings.ContainsKey("Pic1"))
                textPic1.Text = settings["Pic1"].ToString();
            else
                textPic1.Text = string.Empty;

            if (settings.ContainsKey("Pic2"))
                textPic2.Text = settings["Pic2"].ToString();
            else
                textPic2.Text = string.Empty;

            header.Text = textHeader.Text;
            title.Text = textTitle.Text;
            url.Text = textURL.Text;
            image1.Source = string.IsNullOrEmpty(textPic1.Text) ? null : new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(textPic1.Text));
            image2.Source = string.IsNullOrEmpty(textPic2.Text) ? null : new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(textPic2.Text));
        }

        private void SaveConfig()
        {
            IPropertySet settings = ApplicationData.Current.LocalSettings.Values;
            settings["Username"] = textUser.Text;
            settings["Header"] = textHeader.Text;
            settings["Title"] = textTitle.Text;
            settings["URL"] = textURL.Text;
            settings["SSML"] = textSSML.Text;
            settings["Pic1"] = textPic1.Text;
            settings["Pic2"] = textPic2.Text;

            InitializeCortana();
        }

    }
}
