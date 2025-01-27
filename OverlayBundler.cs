using Compendium;
using System;
using System.Net;
using System.Collections.Generic;
using static SmartOverlays.OverlayManager;

namespace SmartOverlays {
    public class OverlayBundler { // 1 OverlayBundler for 1 player
        public static int TemporaryStartLine = -2;
        //public SortedDictionary<Tuple<float, MessageAlign>, Message> AllMessages { get => pack.AllMessages; }
        public int FullLeftPos { get => _fullLeftPos; }
        public Dictionary<Tuple<float, MessageAlign>, Message> AllMessages {
            get {
                //if (!(primaryHint is null)) Plugin.Info(0);
                updateFullLeftPos();
                UpdatePrimaryHint(OverlayManager.refreshInterval);
                temporaryMessages.RemoveWhere(
                    delegate (TempMessage tmsg) {
                        bool shouldExpire = tmsg.Duration <= 0;
                        if (shouldExpire) {
                            tmsg.SetExpired();
                            //if (OverlayManager.debugInfo)
                            //    Plugin.Info($"({hub.UserId()}) {hub.Nick()} deleted message id: {tmsg.Id}");
                        } /*else if (OverlayManager.debugInfo) {
                        Plugin.Info($"({hub.UserId()}) {hub.Nick()} Id: {tmsg.Id}, duration: {tmsg.Duration}, of: {tmsg.HorizontalOffset}");
                    }*/
                        tmsg.Duration -= OverlayManager.refreshInterval;
                        return shouldExpire;
                    }
                );

                //if (!(primaryHint is null)) Plugin.Info(1);


                if (IsTempEmpty && primaryHint is null) {
                    if (IsPackEmpty) {
                        OverlayManager.HubsToRemove.Add(hub);
                        //hub.UnregisterAllOverlays();
                    }
                    //Plugin.Info("empty");
                    return pack.AllMessages;
                }
                //if (!(primaryHint is null)) Plugin.Info(2);

                if (lastChanges != changes || packLastChanges != pack.LastSumChanges) {
                    allMessagesWithTemporary = new Dictionary<Tuple<float, MessageAlign>, Message>(pack.AllMessages);
                    SortedSet<TempMessage> tempMessagesWithPrimary = new SortedSet<TempMessage>(temporaryMessages, new TempMessageSorter()); 
                    if (!(primaryHint is null)) {
                        if (OverlayManager.debugInfo) {
                            Plugin.Info($"Primary Hint for: {hub.Nick()} at offset: {primaryHint.HorizontalOffset}");
                            foreach (var msg in primaryHint.Messages) {
                                Plugin.Info(WebUtility.HtmlEncode(msg.Content));
                            }
                        }
                        tempMessagesWithPrimary.Add(primaryHint);
                    }
                    float line = 15;
                    foreach (var tempMesage in tempMessagesWithPrimary) {
                        //Plugin.Info($"count: {tempMesage.Messages.Count}");
                        line = Math.Min(line, tempMesage.HorizontalOffset);
                        foreach (var message in tempMesage.Messages) {
                            if (OverlayManager.debugInfo) {
                                Plugin.Info($"line: {line}:");
                                Plugin.Info("<noparse>" + message.Content + "</noparse>");
                                message.Content = line + ": " + message.Content;
                            }
                            allMessagesWithTemporary.Add(new Tuple<float, MessageAlign>(line, message.Align), message);
                            line -= (float)(message.PixelSize) / OverlayManager.PixelsPerEm;
                            //Plugin.Info($"n. {line}: {message.message}");
                        }
                    }
                    lastChanges = changes;
                    packLastChanges = pack.LastSumChanges;
                }
                //if (!(primaryHint is null)) Plugin.Info(3);
                return allMessagesWithTemporary;
            }
        }

        public void UpdateMessages() {

        }

        private ReferenceHub hub;
        private OverlayPack pack;
        private TempMessage primaryHint = null;

        private Dictionary<Tuple<float, MessageAlign>, Message> allMessagesWithTemporary;
        private SortedSet<TempMessage> temporaryMessages = new SortedSet<TempMessage>(new TempMessageSorter());
        private uint changes = 0;
        private uint lastChanges = 0;
        private uint packLastChanges = 0;
        private int _fullLeftPos = 0;


        public OverlayBundler(ReferenceHub hub) {
            this.hub = hub;
            this.pack = new OverlayPack();
            updateFullLeftPos();
        }

        public OverlayBundler(ReferenceHub hub, OverlayPack pack) {
            //if (!pack.Shared) throw new Exception("OverlayPack must be Shared to pass it to OverlayBundler constructor");
            if (pack is null) throw new ArgumentNullException(nameof(pack));
            this.hub = hub;
            this.pack = pack;
        }

        public void SetPrimaryHint(string message, float duration = 3f, bool instantShow = true) {
            primaryHint = new TempMessage(this, message, duration, align: MessageAlign.Center);
            //Plugin.Info("set primary");
            Changed();
            if (instantShow) {
                Calls.NextFrame(() => hub.ForceRefreshHints());
            }
        }

        private void UpdatePrimaryHint(float interval) {
            if (primaryHint is null) return;
            //Plugin.Info(primaryHint.Duration);
            if (primaryHint.Duration > 0) {
                primaryHint.Duration -= interval;
                return;
            }
            //Plugin.Info("deleted primary");
            primaryHint.SetExpired();
            primaryHint = null;
            Changed();
        }

        public TempMessage AddTemp(string message, float duration = 3f, int? voffset = null, bool instantShow = false, MessageAlign align = MessageAlign.Center) {
            var tempMessage = new TempMessage(this, message, duration, voffset, instantShow, align);
            //Plugin.Info($"added message id: {tempMessage.Id} {message.TrimStart().Substring(0, 12)}");
            temporaryMessages.Add(tempMessage);
            return tempMessage;
        }

        public bool RemoveTemp(TempMessage tempMessage) {
            return temporaryMessages.Remove(tempMessage);
        }

        public bool ContainsTemp(TempMessage tempMessage) {
            return temporaryMessages.Contains(tempMessage);
        }

        public int TempCount => temporaryMessages.Count;

        public ReferenceHub Hub { get => hub; }

        public bool IsTempEmpty => TempCount == 0 && primaryHint is null;

        //public Contain, Remove, SetDuration

        public bool AddOverlay(Overlay overlay) {
            if (pack.Shared) {
                Plugin.Info("unimplemented");
                return false;
            } else {
                return pack.AddOverlay(overlay);
            }
        }

        public bool RemoveOverlay(Overlay overlay) {
            if (pack.Shared) {
                Plugin.Info("unimplemented");
                return false;
            } else {
                return pack.RemoveOverlay(overlay);
            }
        }

        public bool IsRegistered(Overlay overlay) {
            return pack.Contains(overlay);
        }

        public bool IsPackEmpty => pack.IsEmpty();

        public void Changed() {
            changes++;
        }

        private void updateFullLeftPos() {
            var sync = hub.aspectRatioSync;
            _fullLeftPos = Convert.ToInt32(Math.Round(45.3448f * sync.AspectRatio - 51.527f));
        }
    }
}
