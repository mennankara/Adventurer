using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Adventurer.Game.Combat;
using Adventurer.Game.Events;
//using Adventurer.Game.Grid;
using Adventurer.Settings;
using Adventurer.UI;
using Adventurer.Util;
using Zeta.Bot;
using Zeta.Bot.Navigation;
using Zeta.Common.Plugins;
using Zeta.Game.Internals;

namespace Adventurer
{
    public class Adventurer : ICommunicationEnabledPlugin
    {
        internal static Adventurer CurrentInstance { get; set; }

        public string Name { get; private set; }
        public Version Version { get; private set; }
        public string Author { get; private set; }
        public string Description { get; private set; }
        public static bool Enabled { get; set; }

        public Window DisplayWindow { get { return ConfigWindow.Instance; } }

        public PluginCommunicationResponse Receive(IPlugin sender, string command, params object[] args)
        {
            return PluginCommunicator.Receive(sender, command, args);
        }

        internal const string PluginName = "Adventurer";
        internal const string LogName = "Adventurer";
        internal static Version PluginVersion = new Version(1, 3, 4, 47);
        public static bool IsBotStarted = false;



        public void OnPulse()
        {
           
        }

        public static bool IsAdventurerTagRunning()
        {
            const string tagsNameSpace = "Adventurer.Tags";
            if (ProfileManager.OrderManager == null || ProfileManager.OrderManager.CurrentBehavior == null)
            {
                return false;
            }
            return ProfileManager.OrderManager.CurrentBehavior.GetType().Namespace == tagsNameSpace;
        }

        public static string GetCurrentTag()
        {
            const string tagsNameSpace = "Adventurer.Tags";
            if (ProfileManager.OrderManager == null || ProfileManager.OrderManager.CurrentBehavior == null)
            {
                return string.Empty;
            }
            var type = ProfileManager.OrderManager.CurrentBehavior.GetType();
            if (type.Namespace == tagsNameSpace)
            {
                return type.Name;
            }
            return string.Empty;
        }

        public void OnInitialize()
        {
            // YAR Login Support (YARKickstart Ibot will call this from the proper thread)
            if (!Application.Current.CheckAccess()) return;

            Task.Factory.StartNew(VersionCheck);
            Logger.Info(string.Format("({0}) initialized.", Version));
            BotMain.OnStart += PluginEvents.OnBotStart;
            BotMain.OnStop += PluginEvents.OnBotStop;
            //GridProvider.Initialize();            
        }



        public void OnShutdown()
        {

        }

        private INavigationProvider _previousNavigationProvider;
        public void OnEnabled()
        {
            // YAR Login Support (YARKickstart Ibot will call this from the proper thread)
            if (!Application.Current.CheckAccess()) return;

            Enabled = true;
            Logger.Info(string.Format("({0}) enabled.", Version));
            DeveloperUI.InstallTab();
            //OverlayUI.InstallOverlayComponents();
            BotEvents.WireUp();
            //MainUI.InstallButtons();

            SafeZerg.Instance.DisableZerg();
        }



        public void OnDisabled()
        {
            Enabled = false;
            Logger.Info((string.Format("({0}) disabled.", Version)));
            DeveloperUI.RemoveTab();
            //OverlayUI.RemoveOverlayComponents();
            BotEvents.UnWire();
            //MainUI.InstallButtons();
        }


        private Adventurer()
        {
            Name = PluginName;
            Version = PluginVersion;
            Author = "TarasBulba";
            Description = "Bounty & Rift Runner";
            CurrentInstance = this;
        }

        public bool Equals(IPlugin other)
        {
            return false;
        }

        internal static bool IsGameOrBotPaused
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(BotMain.StatusText))
                {
                    return BotMain.StatusText.IndexOf("paused", StringComparison.InvariantCultureIgnoreCase) > 0;
                }
                return true;
            }
        }

        private static void VersionCheck()
        {
            const string fileUrl = "https://subversion.assembla.com/svn/bulba-s-adventurer.2/trunk/Plugins/Adventurer/Adventurer.cs";
            var regex = new Regex("Version\\((\\d+), *(\\d+), *(\\d+), *(\\d+)\\)", RegexOptions.Compiled);
            var webClient = new WebClient();
            try
            {
                var adventurerCs = webClient.DownloadString(fileUrl);
                if (!string.IsNullOrWhiteSpace(adventurerCs))
                {
                    var match = regex.Match(adventurerCs);
                    if (match.Success && match.Groups.Count == 5)
                    {
                        var major = Convert.ToInt32(match.Groups[1].Captures[0].Value);
                        var minor = Convert.ToInt32(match.Groups[2].Captures[0].Value);
                        var build = Convert.ToInt32(match.Groups[3].Captures[0].Value);
                        var revision = Convert.ToInt32(match.Groups[4].Captures[0].Value);
                        var netVersion = new Version(major, minor, build, revision);
                        Console.WriteLine("Net Version {0}", netVersion);
                        var compareResult = PluginVersion.CompareTo(netVersion);
                        if (compareResult < 0)
                        {
                            Logger.RawWarning("==============================================================");
                            Logger.RawWarning(" You are running an outdated version of Adventurer.");
                            Logger.RawWarning(" Please download the latest version ({0})", netVersion);
                            Logger.RawWarning(" https://www.thebuddyforum.com/demonbuddy-forum/plugins/adventurer/225946-plugin-adventurer.html");
                            Logger.RawWarning("==============================================================");
                        }
                        else
                        {
                            Logger.Info("Plugin is up-to-date.");
                        }

                    }

                }
            }
            catch (Exception)
            {
            }

        }




    }

}
