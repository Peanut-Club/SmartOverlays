using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartOverlays {
    public class Message {
        public string Content;
        public int PixelSize;
        public MessageAlign Align { get; }

        public float EmSize { get => (float)PixelSize / OverlayManager.PixelsPerEm; }

        public Message(string message = "", int? pixelSize = null, MessageAlign align = MessageAlign.Center) {
            Content = message;
            Align = align;
            PixelSize = pixelSize ?? OverlayManager.PixelsPerEm;
        }
    }

    /*
    class MessageAlignSorter : IComparer<Message> {
        public int Compare(Message left, Message right) {
            return (int)right.align - (int)left.align;
        }
    }
    */
}
