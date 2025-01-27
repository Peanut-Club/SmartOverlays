using helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using static SmartOverlays.OverlayManager;

namespace SmartOverlays {

    public class OverlayPack { //now every Player has one Pack, should multiple players should have one pack, if save overlays are registered
        public bool Shared { get; }

        public uint LastSumChanges { get => _lastSumChanges; }

        //public SortedDictionary<Tuple<float, MessageAlign>, Message> AllMessages {
        public Dictionary<Tuple<float, MessageAlign>, Message> AllMessages {
            get {
                /*if (!Shared)*/
                CheckForUpdate();
                return _allMessages;
            }
        }

        //private SortedDictionary<Tuple<float, MessageAlign>, Message> _allMessages;
        private Dictionary<Tuple<float, MessageAlign>, Message> _allMessages;

        //private SortedSet<Overlay> _overlays;
        private HashSet<Overlay> _overlays;
        private uint _lastSumChanges;

        public OverlayPack(bool shared = false) {
            //_overlays = new SortedSet<Overlay>(new OverlaySorter());
            _overlays = new HashSet<Overlay>();
            _lastSumChanges = 0;
            Shared = shared;
            //_allMessages = new SortedDictionary<Tuple<float, MessageAlign>, Message>();
            _allMessages = new Dictionary<Tuple<float, MessageAlign>, Message>();
        }

        public bool AddOverlay(Overlay overlay) {
            if (overlay is null) throw new ArgumentNullException(nameof(overlay));
            bool res = _overlays.Add(overlay);
            CheckForUpdate();
            return res;
        }

        public bool RemoveOverlay(Overlay overlay) {
            if (overlay is null) throw new ArgumentNullException(nameof(overlay));
            bool res = _overlays.Remove(overlay);
            CheckForUpdate();
            return res;
        }

        public bool Contains(Overlay overlay) {
            if (overlay is null) throw new ArgumentNullException(nameof(overlay));
            bool res = _overlays.Contains(overlay);
            return res;
        }

        public bool IsEmpty() => _overlays.IsEmpty();

        private void CheckForUpdate() {
            uint sum = 0;
            foreach (var overlay in _overlays) {
                sum += overlay.Changes;
            }
            if (_lastSumChanges == sum) return;

            _lastSumChanges = sum;
            _allMessages.Clear();
            Update();
        }


        private void Update() {
            //IEnumerable<KeyValuePair<Tuple<float, MessageAlign>, Message>> newMessages = new SortedDictionary<Tuple<float, MessageAlign>, Message>();
            IEnumerable<KeyValuePair<Tuple<float, MessageAlign>, Message>> newMessages = new Dictionary<Tuple<float, MessageAlign>, Message>();
            foreach (var overlay in _overlays) {
                newMessages = newMessages.Union(overlay.Messages);
            }
            _allMessages = newMessages.ToDictionary();
            //_allMessages = newMessages.ToSortedDictionary();
        }
    }
}
