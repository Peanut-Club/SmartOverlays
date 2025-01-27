using Hints;
using System;
using System.Collections.Generic;

using helpers;
using helpers.Events;
using helpers.Pooling.Pools;
using UnityEngine;
using Compendium;
using Compendium.Updating;
using System.Collections.ObjectModel;
using System.Linq;
using Compendium.Attributes;
using System.IO;
using PluginAPI.Core;

namespace SmartOverlays {
    public static partial class OverlayManager {
        public static readonly EventProvider PreDisplayEvent = new EventProvider();
        public static bool debugInfo = false;

        public static float duration = 3f;
        public const float defaultRefreshInterval = 1f; //in seconds
        public static float refreshInterval = defaultRefreshInterval;
        private static float timeCounter = 0f;

        public static int MaxCharsOnLine = 60;

        public const int PixelsPerEm = 35;

        //private static HashSet<Overlay> _allOverlays = new HashSet<Overlay>();
        //private static Dictionary<Player, SortedSet<Overlay>> _playerOverlays = new Dictionary<Player, SortedSet<Overlay>>();
        public static Dictionary<ReferenceHub, OverlayBundler> HubBundlers = new Dictionary<ReferenceHub, OverlayBundler>();
        public static List<ReferenceHub> HubsToRemove = new List<ReferenceHub>();

        private static readonly HintParameter[]  _hintParameter = new HintParameter[] { new StringHintParameter(string.Empty)};
        private static StringBuilderPool _sbPool = new StringBuilderPool();

        private static bool _registered = false;
        private static bool _endRoundPause = false;

        //private static bool _displaying = false;
        //private static bool _initialized = false;
        //private static Timer _timer;


        public static Overlay CreateOverlay() { return new Overlay(); }

        public static void ResetManager() {
            ClearHints();
            HubBundlers.Clear();
            HubsToRemove.Clear();
            //if (!_initialized) InitTimers();
            //_timer.Stop();
            //_displaying = false;
        }

        public static void ClearHints() {
            ClearHints(HubBundlers.Keys);
        }

        public static void ClearHints(IEnumerable<ReferenceHub> hubs) {
            if (_endRoundPause) return;
            hubs.ForEach(hub => hub.hints.Show(new TextHint(String.Empty, new HintParameter[] { new StringHintParameter(String.Empty) }, null, 1f)));
        }

        public static void ForceRefreshHints(this ReferenceHub hub) {
            return;
            if (HubBundlers.TryGetValue(hub, out OverlayBundler bundler)) {
                refreshInterval = 0f;
                hub.hints.Show(new TextHint(DrawHud(bundler), _hintParameter, null, duration));
                refreshInterval = defaultRefreshInterval;
            }
        }

        /*
        private static void ShowHint(this Player player, Message message, float duration = 3f) {
            AddPlayer(player);
            if (message is null || !_playerOverlays.ContainsKey(player)) return;
            _playerOverlays[player].Add(message, duration);
        }*/

        public static TempMessage AddTempHint(this ReferenceHub hub, string message, float duration = 3f, int? voffset = null, bool instantShow = false, MessageAlign align = MessageAlign.Center) {
            AddPlayer(hub);
            if (message is null || !HubBundlers.ContainsKey(hub))
                throw new NullReferenceException("Player was not added");
            var tempMes = HubBundlers[hub].AddTemp(message, duration, voffset, instantShow, align);
            return tempMes;
        }

        public static void SetPrimaryHint(this ReferenceHub hub, string message, float duration = 3f, bool instantShow = true) {
            //if (debugInfo)
            //    Plugin.Info($"Primary Hint Duration: {duration}");
            AddPlayer(hub);
            if (message is null || !HubBundlers.ContainsKey(hub))
                throw new NullReferenceException("Player was not added");
            HubBundlers[hub].SetPrimaryHint(message, duration, instantShow);
        }

        public static void SetPrimaryHint(Compendium.Messages.HintMessage hintMessage, ReferenceHub hub) {
            if (!hintMessage.IsValid) return;
            //using (StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "latest_hint_input.txt")))
            //    outputFile.WriteLine(hintMessage.Value);
            SetPrimaryHint(hub, hintMessage.Value, (float)hintMessage.Duration);
        }

        private static void AddPlayer(ReferenceHub hub) {
            if (!HubBundlers.ContainsKey(hub)) {
                //_playerOverlays.Add(player, new SortedSet<Overlay>(new OverlaySorter()));
                HubBundlers.Add(hub, new OverlayBundler(hub));
            }
        }

        public static bool UnregisterAllOverlays(this ReferenceHub hub) {
            return HubBundlers.Remove(hub);
        }

        public static bool RegisterOverlay(this ReferenceHub hub, Overlay overlay) {
            if (overlay == null) return false;
            AddPlayer(hub);
            if (overlay is null || !HubBundlers.ContainsKey(hub)) return false;
            bool res = HubBundlers[hub].AddOverlay(overlay);
            //Plugin.Info("added " + player.DisplayNickname);
            //hub.ForceRefreshHints();
            return res;
        }

        public static bool UnregisterOverlay(this ReferenceHub hub, Overlay overlay, bool removeEmptyPlayer = true) {
            if (overlay is null || !HubBundlers.ContainsKey(hub)) return false;
            var res = HubBundlers[hub].RemoveOverlay(overlay);
            if (removeEmptyPlayer && HubBundlers[hub].IsPackEmpty && HubBundlers[hub].IsTempEmpty) {
                HubBundlers.Remove(hub);
            }
            return res;
        }

        public static bool IsOverlayRegistered(this ReferenceHub hub, Overlay overlay) {
            if (!HubBundlers.ContainsKey(hub)) return false;
            return HubBundlers[hub].IsRegistered(overlay);
        }

        private static string DrawHud(OverlayBundler bundler) {
            int fullLeftPos = bundler.FullLeftPos;
            var sb = _sbPool.Get();
            sb.Append("~\n<line-height=1285%>\n<line-height=0>\n");
            foreach (var messagePair in bundler.AllMessages) {
                var pos = messagePair.Key;
                var msg = messagePair.Value;

                sb.Append($"<voffset={pos.Item1.ToString(System.Globalization.CultureInfo.InvariantCulture)}em>");
                if (pos.Item2 is MessageAlign.FullLeft) {
                    sb.Append("<align=left><pos=-" + fullLeftPos + "%>" + msg.Content + "</pos></align>");
                } else if (pos.Item2 is MessageAlign.Left) {
                    sb.Append("<align=left>" + msg.Content + "</align>");
                } else if (pos.Item2 is MessageAlign.Right) {
                    sb.Append("<align=right>" + msg.Content + "</align>");
                } else {
                    sb.Append("" + msg.Content);
                }
                sb.Append("\n");
            }

            sb.Append("<voffset=0><line-height=2100%>\n~");
            return _sbPool.PushReturn(sb);
        }

        //every game tick
        [Update(Delay = (int)(defaultRefreshInterval * 1000), IsUnity = true, PauseWaiting = false, PauseRestarting = true)]
        public static void DisplayOverlays() {
            /*
            timeCounter += Time.deltaTime;
            if (timeCounter <= refreshInterval) return;
            timeCounter = 0f;
            */
            if (_endRoundPause) return;
            float dur = duration;

            if (Round.IsRoundEnded) {
                dur = 3600;
                _endRoundPause = true;
            }

            PreDisplayEvent.Invoke();
            DateTime start = DateTime.Now;

            if (HubBundlers.IsEmpty()) return;
            HubsToRemove.Clear();

            foreach (var playerOverlays in HubBundlers) {
                string message = DrawHud(playerOverlays.Value);
                if (debugInfo) {
                    using (StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "latest_all_output.txt"))) {
                        outputFile.WriteLine(message);
                    }
                }
                playerOverlays.Key.hints.Show(new TextHint(message, _hintParameter, null, dur));
            }

            foreach (var hub in HubsToRemove) {
                HubBundlers.Remove(hub);
            }

            if (debugInfo)
                Plugin.Info($"Display time took: {BetterTime(DateTime.Now - start)}");
        }

        /*
        private static void StartTimers() {
            if (_playerOverlays.IsEmpty()) return;

            _displaying = true;
            if (!_initialized) InitTimers();
            _timer.Start();
        }

        private static void InitTimers() {
            _initialized = true;
            _timer = new Timer();
            _timer.Interval = refreshInterval * 1000f;
            _timer.Elapsed += DisplayOverlaysTimer;
        }
        private static void DisplayOverlaysTimer(System.Object source, ElapsedEventArgs e) {
            if (_playerOverlays.IsEmpty()) {
                _timer.Stop();
                _displaying = false;
                return;
            }
            PreDisplayEvent.Invoke();
            
            DateTime start = DateTime.Now;
            DisplayOverlays();
            if (debugInfo)
                Log.Info($"time took: {BetterTime(DateTime.Now - start)}");
        }
        */

        /*
        [RoundStateChanged(Compendium.Enums.RoundState.Ending)]
        private static void OnRoundEnd() {
            Calls.Delay(4.5f, () => { _endRoundPause = true; });
        }
        */

        [RoundStateChanged(Compendium.Enums.RoundState.WaitingForPlayers)]
        private static void OnWaitingOnPlayers() {
            _endRoundPause = false;
        }

        public static void RegisterEvents() {
            if (_registered) return;
            _registered = true;
            ResetManager();

            UpdateHandler.Register();
            AttributeRegistry<RoundStateChangedAttribute>.Register();
            //EventRegistry.RegisterEvents(assembly);
            //CommandManager.Register(assembly);

            //Reflection.TryAddHandler<Action>(typeof(StaticUnityMethods), "OnFixedUpdate", DisplayOverlays);
            //UpdateHandler.Register(DisplayOverlays, isUnity: true, isWaiting: true, isRestarting: true, delay: (int)(refreshInterval * 1000));
        }

        public static void UnregisterEvents() {
            if (!_registered) return;
            _registered = false;
            ResetManager();

            UpdateHandler.Unregister();
            //Reflection.TryRemoveHandler<Action>(typeof(StaticUnityMethods), "OnFixedUpdate", DisplayOverlays);
        }


        private static string BetterTime(TimeSpan diff) {
            return $"{diff.TotalMilliseconds} ms";
        }


        /*
        public static SortedDictionary<K, V> ToSortedDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> pairs) {
            var dictionary = new SortedDictionary<K, V>();
            foreach (var pair in pairs) {
                dictionary.Add(pair.Key, pair.Value);
            }
            return dictionary;
        }
        */

        /*
        private static bool RegisterOverlay(Overlay overlay) {
            //if (!_overlays.Contains(overlay)) return false;
            //TODO: udělat sémantiku
            //return _allOverlays.Add(overlay);
            return true;
        }

        private static bool UnregisterOverlay(Overlay overlay) {
            bool res = false;
            foreach (var player in _playerOverlays.Keys) {
                res |= UnregisterPlayer(player, overlay, false);
            }
            _playerOverlays = _playerOverlays.Where(p => !p.Value.IsEmpty()).ToDictionary();
            //return _allOverlays.Remove(overlay);
            return res;
        }

        //private static bool RemoveOverlay(Overlay overlay) {  return _allOverlays.Remove(overlay); } //TODO: sémantika,

        //private static bool NameTaken(Overlay overlay) { }
        */
    }
}


/*

        public static void DisplayOverlays() {

            //DateTime start = DateTime.Now;
            foreach (var playerOverlays in _playerOverlays) {
                //List<SortedSet<Message>> lines = new List<SortedSet<Message>>();
                //foreach (var overlay in pair.Value) { }

                SortedDictionary<Tuple<int, MessageAlign>, Message> allMessages = new SortedDictionary<Tuple<int, MessageAlign>, Message>();
                foreach (var overlay in playerOverlays.Value) {
                    allMessages = allMessages.Union(overlay.GetMessages()).ToSortedDictionary();
                }

                //allMessages = playerOverlays.Value.Last().GetMessages();

                //var allMessages = playerOverlays.Value.First().GetMessages();

                int lastLine = -1;
                var sb = new StringBuilder();
                sb.Append("‾");
                bool resetHeight = false;
                foreach (var messagePair in allMessages) {
                    var pos = messagePair.Key;
                    var message = messagePair.Value;

                    if (pos.Item1 < lastLine) continue;

                    if (lastLine != pos.Item1 && resetHeight) {
                        resetHeight = false;
                        sb.Append("<line-height=100%>");
                    }

                    for (int i = lastLine + 1; i < pos.Item1; i++) sb.Append("<br>");

                    if (lastLine == pos.Item1 && !resetHeight) {
                        resetHeight = true;
                        sb.Append("<line-height=0>");
                    }
                    sb.Append("<br>");

                    if (pos.Item2 is MessageAlign.FullLeft) {
                        sb.Append("<align=\"left\"><pos=-28%>" + message.message + "</pos></align>");
                    } else if (pos.Item2 is MessageAlign.Left) {
                        sb.Append("<align=\"left\">" + message.message + "</align>");
                    } else if (pos.Item2 is MessageAlign.Right) {
                        sb.Append("<align=\"right\">" + message.message + "</align>");
                    } else {
                        sb.Append(message.message);
                    }


                    lastLine = pos.Item1;
                }

                if (resetHeight) sb.Append("<line-height=100%>");
                for (int i = lastLine; i < MAXLINE; i++) sb.Append("<br>");
                sb.Append("<br><br><br><br><br><br><br><br>|");
                var wholeMessage = sb.ToString();//Round.Duration.TotalSeconds.ToString();
                playerOverlays.Key.ReferenceHub.hints.Show(new TextHint(wholeMessage, new HintParameter[] { new StringHintParameter(wholeMessage) }, null, _refreshInterval + 1f));
            }
            //Log.Info($"start: {BetterTime(start)}, end: {BetterTime(DateTime.Now)}");
        }
*/
