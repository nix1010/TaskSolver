using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditorApplication.Utility
{
    public class CompletionData : ICompletionData
    {
        public int Offset { get; set; }
        public int Length { get; set; }

        public CompletionData(string text, int offset, int length)
        {
            this.Text = text;
            this.Offset = offset;
            this.Length = length;
        }

        public System.Windows.Media.ImageSource Image
        {
            get { return null; }
        }

        public string Text { get; private set; }

        public double Priority
        {
            get
            {
                return 1;
            }
        }

        // Use this property if you want to show a fancy UIElement in the list.
        public object Content
        {
            get { return this.Text; }
        }

        public object Description
        {
            get { return null; }
        }

        public void Complete(TextArea textArea, ISegment completionSegment,
            EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(Offset, Length, Text, OffsetChangeMappingType.RemoveAndInsert);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CompletionData))
            {
                return false;
            }

            CompletionData data = (CompletionData)obj;

            return data.Text.Equals(Text);
        }

        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }
    }
}
