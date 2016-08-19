﻿using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using TeamCoding.Extensions;

namespace TeamCoding.VisualStudio.TextAdornment
{
    /// <summary>
    /// TextAdornment places red boxes behind all the "a"s in the editor window
    /// </summary>
    internal sealed class TextAdornment
    {
        /// <summary>
        /// The layer of the adornment.
        /// </summary>
        private readonly IAdornmentLayer Layer;

        /// <summary>
        /// Text view where the adornment is created.
        /// </summary>
        private readonly IWpfTextView View;

        /// <summary>
        /// Adornment brush.
        /// </summary>
        private readonly Brush brush;

        /// <summary>
        /// Adornment pen.
        /// </summary>
        private readonly Pen pen;
        public TextAdornment(IWpfTextView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            Layer = view.GetAdornmentLayer("TextAdornment");
            View = view;
            View.LayoutChanged += OnLayoutChanged;

            // Create the pen and brush to color the box behind the a's
            brush = new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0x00, 0xff));
            brush.Freeze();

            var penBrush = new SolidColorBrush(Colors.Red);
            penBrush.Freeze();
            pen = new Pen(penBrush, 0.5);
            pen.Freeze();
        }

        /// <summary>
        /// Handles whenever the text displayed in the view changes by adding the adornment to any reformatted lines
        /// </summary>
        /// <remarks><para>This event is raised whenever the rendered text displayed in the <see cref="ITextView"/> changes.</para>
        /// <para>It is raised whenever the view does a layout (which happens when DisplayTextLineContainingBufferPosition is called or in response to text or classification changes).</para>
        /// <para>It is also raised whenever the view scrolls horizontally or when its size changes.</para>
        /// </remarks>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        internal async void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            var syntaxTree = await e.NewSnapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxTreeAsync();
            var newOrChangedNodes =
                e.NewOrReformattedLines.Select(l => l.Extent).SelectMany(extent => syntaxTree.GetRoot().DescendantNodes(new TextSpan(extent.Start, extent.Length))).Distinct();
            
            var CaretMemberHashCodeToDataPointString = TeamCodingPackage.Current.RemoteModelChangeManager.GetOpenFiles()
                                                                    .Where(of => of.CaretMemberHashCode != null)
                                                                    .Select(of => new { CaretMemberHashCode = of.CaretMemberHashCode[0], of.IdeUserIdentity })
                                                                    .GroupBy(of => of.CaretMemberHashCode)
                                                                    .ToDictionary(g => g.Key, g => g.Select(of => of.IdeUserIdentity).Distinct());
            var nodesWithHashCode = newOrChangedNodes.Select(n => new { Node = n, Hash = n.GetTreePositionHashCode() }).ToArray();
            foreach (var node in nodesWithHashCode)
            {
                if (CaretMemberHashCodeToDataPointString.ContainsKey(node.Hash))
                {
                    // TODO: Render something indicating the user is here!

                }
            }
        }

        /// <summary>
        /// Adds the scarlet box behind the 'a' characters within the given line
        /// </summary>
        /// <param name="line">Line to add the adornments</param>
        private void CreateVisuals(ITextViewLine line)
        {
            IWpfTextViewLineCollection textViewLines = View.TextViewLines;
            // Loop through each character, and place a box around any 'a'
            for (int charIndex = line.Start; charIndex < line.End; charIndex++)
            {
                if (View.TextSnapshot[charIndex] == 'a')
                {
                    SnapshotSpan span = new SnapshotSpan(View.TextSnapshot, Span.FromBounds(charIndex, charIndex + 1));
                    Geometry geometry = textViewLines.GetMarkerGeometry(span);
                    if (geometry != null)
                    {
                        var drawing = new GeometryDrawing(brush, pen, geometry);
                        drawing.Freeze();

                        var drawingImage = new DrawingImage(drawing);
                        drawingImage.Freeze();

                        var image = new Image
                        {
                            Source = drawingImage,
                        };

                        // Align the image with the top of the bounds of the text geometry
                        Canvas.SetLeft(image, geometry.Bounds.Left);
                        Canvas.SetTop(image, geometry.Bounds.Top);

                        Layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
                    }
                }
            }
        }
    }
}
