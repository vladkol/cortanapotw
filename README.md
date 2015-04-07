Cortana's Pick of the Week 
=====

Hello from Seattle! 

[Cortana's](http://www.windowsphone.com/en-us/how-to/wp8/cortana/meet-cortana) Pick of the Week app made for ["This Week on Channel 9"] (http://channel9.msdn.com/Shows/This+Week+On+Channel+9) (TWC9).

If you want to know how to integrate your app with Cortana, there is a great talk on that - ["Integrating Your App into the Windows Phone Speech Experience"](http://channel9.msdn.com/Events/Build/2014/2-530) by Rob Chambers, F Avery Bishop and Monica South. 

There is also a good collection of links to samples: http://channel9.msdn.com/coding4fun/blog/Cortana-whats-new-There-are-new-code-samples 


We are using this app at "This Week on Channel 9" for making some fun around technologies. 
This app reqires some configuration. By default it starts in the configuration mode where you can specify header, title, 2 images, URL for this week's Pick of the Week **and** an actual [SSML](http://www.w3.org/TR/speech-synthesis/) or text for Cortana talking through. 

![Configuration mode](https://monosnap.com/image/9QxhPHNKAvy4doNlBlSK9ELl7X9Bw6.png)

Then, with launching the app via Cortana (ask *"Cortana, what is your pick of the week?"*) you will get something like on a picture below and Cortana talking. 

![Working app](https://monosnap.com/image/XgXdLgMlZcaEy02PoJX6nFUpoG3HfR.png)

Cortana works in Windows Phone 8.1 and later. The project requires Visual Studio 2013 or later. 

In addition to the resources above, there is a very good sample of Cortana integration - [MSDN Voice Search](https://code.msdn.microsoft.com/windowsapps/MSDN-Voice-Search-for-95c16d92). It includes both basic stuff like we used, and some advanced techiques, e.g. PhraseTopic with natural language commands. 
Good luck! 

P.S. 
We used a modified version of [XamlAnimatedGif](https://github.com/thomaslevesque/XamlAnimatedGif) by Thomas Levesque. 
Thank you, Thomas!
<br />
<br />   
***
**DISCLAIMER**: THE CODE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
