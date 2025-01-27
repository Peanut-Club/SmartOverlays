using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartOverlays {

    public static partial class OverlayManager {
        public class Overlay {
            public string Name { get; }
            //public string Description { get; }
            //public int Priority { get; }
            public uint Changes { get => _changes; }
            public readonly SortedDictionary<Tuple<float, MessageAlign>, Message> Messages;

            private uint _changes;

            //private List<SortedSet<Message>> _lines;
            //public List<SortedSet<Message>> GetLines() { return _lines; }

            //public SortedDictionary<Tuple<float, MessageAlign>, Message> GetMessages() { return Messages; } //TODO: copy or not

            public Overlay(string name = "unspecified"/*, int priority = 0*/) { //, int priority
                if (name == null || name.Length <= 0) throw new ArgumentNullException("name");
                Name = name;
                //Priority = priority;
                _changes = 0;
                Messages = new SortedDictionary<Tuple<float, MessageAlign>, Message>();
                //OverlayManager.RegisterOverlay(this);
            }

            public bool IsOccupied(float line, MessageAlign align) {
                //if (!_lines[line].IsEmpty()) return false;
                //return _lines[line].Any(message => message.align == align);
                return Messages.ContainsKey(new Tuple<float, MessageAlign>(line, align));
            }

            /// <summary>
            /// Adds message to Overlay
            /// </summary>
            /// <returns>
            /// <c>true</c> if message was succesfully added, <c>false</c> if the line with <c>message.align</c> position was occupied
            /// </returns>
            /// <param><c>message</c> is the relative x-offset.</param>
            /// <param><c>line</c> is the relative y-offset.</param>
            public bool AddMessage(Message message, float line = 0, MessageAlign align = MessageAlign.Center) { //TODO: automaticke line
                //if (line < 0) line = FindFreeLine(align);
                //if (line < 0 || IsOccupied(line, align)) return false;
                Messages.Add(new Tuple<float, MessageAlign>(line, align), message);
                _changes++;
                return true;
            }

            public bool RemoveMessage(Message message) {
                bool found = false;
                //foreach (var line in _lines) { found |= line.Remove(message); }
                
                foreach(var item in Messages.Where(kvp => object.ReferenceEquals(kvp.Value, message)).ToList()) {
                    found |= Messages.Remove(item.Key);
                }
                _changes++;
                return found;
            }

            /* TODO: dodelat, i pro sloupce
            public bool RemoveMessage(Message message, int line) {
                return _lines[line].Remove(message);
            }*/

            public void MadeChange() {
                _changes++;
            }

            public virtual void UpdateMessages() { }

            /*
            public bool RegisterPlayer(Player player) {

                return OverlayManager.RegisterPlayer(player, this);
            }

            public bool UnregisterPlayer(Player player) {
                return OverlayManager.RegisterPlayer(player, this);
            }
            */

            /*
            public bool UnregisterOverlay() {
                return OverlayManager.UnregisterOverlay(this);
            }
            */

            /*
            private int FindFreeLine(MessageAlign align) {
                for (int line = 0; line < MAXLINE; line++) {
                    if (!Messages.ContainsKey(new Tuple<float, MessageAlign>(line, align)))
                        return line;
                }
                return -1;
            }*/
        }

        /*
        public class OverlaySorter : IComparer<Overlay> {
            public int Compare(Overlay left, Overlay right) {
                if (left.Priority != right.Priority) return right.Priority - left.Priority;
                return left.Name.CompareTo(right.Name);
            }
        }*/
    }
}
