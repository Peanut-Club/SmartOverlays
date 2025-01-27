using Compendium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmartOverlays {

    public class TempMessage {
        public float Duration { get; set; }
        public bool Expired { get => _expired; }
        public int HorizontalOffset { get => _horizontalOffset; }
        public uint Id { get => id; }
        public List<Message> Messages { get => _messages; }

        private OverlayBundler obundler;
        private bool _expired;
        private List<Message> _messages;
        private int _horizontalOffset;
        private static uint idCounter = 0;
        private uint id;

        public TempMessage(OverlayBundler obundler, string message, float duration, int? voffset = null, bool instantShow = false, MessageAlign align = MessageAlign.Center) {
            this.obundler = obundler;
            id = idCounter++;
            Duration = duration;
            _expired = false;
            _messages = new List<Message>();
            SetMessages(message, voffset == null ? -4 : voffset, instantShow, align);
        }

        public void SetMessages(string message, int? voffset = null, bool instantShow = false, MessageAlign align = MessageAlign.Center) {
            if (Expired) throw new Exception("This TempMessage is expired!");
            _messages.Clear();
            message = message.Replace("\r\n", "\n").Replace("\\n", "\n").Replace("<br>", "\n");
            message = TrimStartCountNewLine(message, out int count);
            if (voffset != null) {
                _horizontalOffset = (int)voffset;
            } else {
                _horizontalOffset = -count;
            }
            if (OverlayManager.debugInfo) {

                Plugin.Info($"new message; offset: {HorizontalOffset}");
            }

            var messages = SplitToMessages(message.TrimEnd(), OverlayManager.MaxCharsOnLine, align);
            //using (StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "latest_hint_output.txt"))) {
            //    outputFile.WriteLine(HorizontalOffset);
            //    messages.ForEach(msg => outputFile.WriteLine(msg.PixelSize + ": " + msg.Content)); }

            _messages = messages;

            obundler.Changed();
            if (instantShow)
                obundler.Hub.ForceRefreshHints();
        }

        public bool SetExpired() {
            Duration = -1;
            _expired = true;
            _messages.Clear();
            obundler.Changed();
            return true;
        }

        public static string TrimStartCountNewLine(string str, out int count) {
            int length = str.Length;
            int i = 0;
            for (; i < length && str[i] == '\n'; i++) { }
            count = i;
            return str.Substring(i);
        }


        public static List<Message> SplitToMessages(string str, int maxLineLength, MessageAlign align) {
            string pattern = @"\n|(<[^>]*>)+|\s*[^<\s\r\n]+[^\S\r\n]*|\s*"; // @"\n|(<[^>]*>)+|\s*[^<\s]+\s*|\s*"; //@"<[^>]+>|[^<>\s]+|[^\S\r\n]+|\n";
            MatchCollection matches = Regex.Matches(str, pattern);

            //using (StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "latest_regex_output.txt")))
            //    (from Match match in matches where match.Success select match.Value).ForEach(outputFile.WriteLine);

            List<Message> messages = new List<Message>();
            string part = "";
            int partLength = 0;
            int? size = null;
            bool isEnded = true;
            foreach (Match match in matches) {
                if (!match.Success) continue;
                string word = match.Value;
                

                if (word.Equals("\n")) {
                    //part = part.Trim();
                    manageSize(ref part, ref size, out isEnded);
                    messages.Add(new(part, size, align));
                    //Plugin.Info("NL-ADD: " + WebUtility.HtmlEncode(messages.Last().Content));
                    if (!isEnded) {
                        part = "<size=" + size + ">";
                        partLength = 0;
                        continue;
                    }
                    part = "";
                    partLength = 0;
                    continue;
                }

                if (word.StartsWith("<")) {
                    part += word;
                    continue;
                }

                // Check if adding the match to the current line exceeds the max line length
                if (partLength + word.Length <= maxLineLength) {
                    part += word;
                    partLength += word.Length;
                } else {
                    // If adding the match exceeds the limit, start a new line
                    part = part.Trim();
                    manageSize(ref part, ref size, out isEnded);
                    messages.Add(new (part, size, align));

                    while (word.Length > maxLineLength) {
                        string long_word_part = word.Substring(0, maxLineLength);
                        manageSize(ref long_word_part, ref size, out isEnded);
                        messages.Add(new(long_word_part, size, align));
                        //Plugin.Info("LONG-WORD-ADD: " + WebUtility.HtmlEncode(messages.Last().Content));
                        word = word.Substring(maxLineLength);
                    }

                    part = word;
                    partLength = word.Length;
                    if (!isEnded) {
                        part = "<size=" + size + ">" + part;
                        continue;
                    }
                }
            }

            // Add the last line
            if (!string.IsNullOrEmpty(part)) {
                manageSize(ref part, ref size, out isEnded);
                messages.Add(new(part, size, align));
                //Plugin.Info("LAST-ADD: " + WebUtility.HtmlEncode(messages.Last().Content));
            }

            return messages;
        }

        private static void manageSize(ref string line, ref int? size, out bool isEnded) {
            if (!tryFindSize(line, out size, out isEnded)) return;
            if (!isEnded) {
                line += "</size>";
            }
            //Plugin.Info($"found size: '{size}'; is ended: {isEnded}");
        }

        private static bool tryFindSize(string line, out int? size, out bool isEnded) {
            string pattern = @"(?<=<size=)([^>]*)(?=>)";
            MatchCollection matches = Regex.Matches(line, pattern);
            for (int i = matches.Count - 1; i >= 0; i--) {
                size = getPixelSize(matches[i].Value);
                isEnded = line.IndexOf("</size>", matches[i].Index, StringComparison.OrdinalIgnoreCase) != -1;
                return true;
            }
            size = null;
            isEnded = true;
            return false;
        }

        private static int? getPixelSize(string size) {
            if (size.EndsWith("%")) {
                if (!float.TryParse(size.Substring(0, size.Length - 1), out float percentage)) goto logUnrecognised;
                return (int)(percentage * OverlayManager.PixelsPerEm / 100);
            } else if (size.EndsWith("em")) {
                if (!float.TryParse(size.Substring(0, size.Length - 2), out float em)) goto logUnrecognised;
                return (int)(em * OverlayManager.PixelsPerEm);
            } else if (char.IsDigit(size.Last())) {
                if (!int.TryParse(size, out int pixels)) goto logUnrecognised;
                return pixels;
            } else if (size.EndsWith("px")) {
                if (!int.TryParse(size.Substring(0, size.Length - 2), out int pixels)) goto logUnrecognised;
                return pixels;
            } else {
                return null;
            }
        logUnrecognised:
            Plugin.Warn($"Tried to print unknown size: '{size}', using default");
            return null;
        }
    }

    class TempMessageSorter : IComparer<TempMessage> {
        public int Compare(TempMessage left, TempMessage right) {
            if (left.HorizontalOffset != right.HorizontalOffset)
                return right.HorizontalOffset - left.HorizontalOffset;
            return (int)(right.Id - left.Id);
        }
        /*
        private static int OffsetPositions = 15;
        private static int OffsetSplit = 286331153; // (int)(((long)int.MaxValue - (long)int.MinValue) / OffsetPositions);

        public bool Equals(TempMessage left, TempMessage right) {
            return ReferenceEquals(left, right);
        }

        public int GetHashCode(TempMessage item) {
            if (item.HorizontalOffset > 15) return int.MaxValue;
            int offset = Math.Max(item.HorizontalOffset, 0);
            return offset * OffsetSplit + (int)(item.Id % OffsetSplit);
        }
        */
    }
}
