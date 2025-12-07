/* Copyright(C) 2025 guillaume.taze@proton.me

This program is free software : you can redistribute it and /or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.If not, see < https://www.gnu.org/licenses/>.
*/


using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System;
using System.Windows.Media;

namespace MethodSeparators
{
	internal sealed class MethodSeparator_TextAdornment : IDisposable
	{
        private string FilePath;

        private readonly IAdornmentLayer layer;
		private readonly IWpfTextView view;

		/// <summary>
		/// Initializes a new instance of the <see cref="MethodSeparator_TextAdornment"/> class.
		/// </summary>
		/// <param name="view">Text view to create the adornment for</param>
		public MethodSeparator_TextAdornment(IWpfTextView view, string InFilePath)
		{
			FilePath = InFilePath;

			// Get the ITextBuffer from the IWpfTextView
			this.view = view;
			this.layer = this.view.GetAdornmentLayer("MethodSeparator_TextAdornment");

			this.view.LayoutChanged += OnLayoutChanged;
			this.view.Closed += (sender, e) =>
			{
				this.Dispose();
			};
		}

		// Implement IDisposable.
		// Do not make this method virtual.
		// A derived class should not be able to override this method.
		public void Dispose()
		{
			this.view.LayoutChanged -= OnLayoutChanged;
			GC.SuppressFinalize(this);
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
		internal void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs textViewLayoutChangedEventArgs)
		{
			int minLine = int.MaxValue ;
			int maxLine = int.MinValue;
			foreach (ITextViewLine line in textViewLayoutChangedEventArgs.NewOrReformattedLines)
			{
				minLine = Math.Min(line.Start.GetContainingLineNumber(), minLine);
                maxLine = Math.Max(line.Start.GetContainingLineNumber(), maxLine);
            }
            this.CreateVisuals(minLine, maxLine);
        }

		private void CreateVisuals(int StartLineNumber, int EndLineNumber)
		{
			IWpfTextViewLineCollection textViewLines = this.view.TextViewLines;

			//layer.RemoveAdornmentsByVisualSpan(textViewLines.FormattedSpan);

            for (int LineNumber = StartLineNumber; LineNumber <= EndLineNumber; ++LineNumber)
			{
				bool isFunctionDef = false;
                // if next line is a function definition then draw horizontal line
                if (LineNumber + 1 < this.view.TextSnapshot.LineCount)
                {
                    ITextSnapshotLine nextLine = this.view.TextSnapshot.GetLineFromLineNumber(LineNumber + 1);
                    string nextLineText = nextLine.GetText().Trim();

                    // Simple heuristic for C++ function definition:
                    // - Line ends with '{' or optionally with ')' (for single-line function signatures)
                    // - Contains '(' and ')'
                    // - Does not start with '//' (not a comment)
                    // - Does not contain ';' (not a declaration or prototype)
                    isFunctionDef =
                        !string.IsNullOrEmpty(nextLineText) &&
                        !nextLineText.StartsWith("//") &&
                        !nextLineText.StartsWith("#") &&
                        !nextLineText.StartsWith("else if") &&
                        nextLineText.Contains("(") &&
                        nextLineText.Contains(")") &&
                        !nextLineText.Contains(";") &&
                        (nextLineText.EndsWith("{") || nextLineText.EndsWith(")") || nextLineText.EndsWith("noexcept") || nextLineText.EndsWith("const") || nextLineText.EndsWith("final") || nextLineText.EndsWith("override"));

                    if (isFunctionDef)
					{
                        int firstParenthesisIndex = nextLineText.IndexOf('(');
                        // check that the line has a return type or access modifier
                        string beforeParenthesis = nextLineText.Substring(0, firstParenthesisIndex).Trim();
						string[] tokens = beforeParenthesis.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
						isFunctionDef = tokens.Length >= 2 || beforeParenthesis.Contains("::"); // at least return type and function name
                    }
                }

                if (!isFunctionDef)
                    continue;

                {
					if (LineNumber - 1 < 0)
						continue;

                    ITextSnapshotLine SnapshotNextLine = this.view.TextSnapshot.GetLineFromLineNumber(LineNumber + 1);
					// Create horizontal line addornment
					// Assume 'layer' is your IAdornmentLayer, and 'textViewLine' is the IWpfTextViewLine you want to adorn
					Geometry geometry = textViewLines.GetMarkerGeometry(SnapshotNextLine.Extent);
					if (geometry != null)
					{
                        System.Windows.Shapes.Line line = new System.Windows.Shapes.Line
						{
							X1 = geometry.Bounds.Left,
							X2 = geometry.Bounds.Left + this.view.ViewportWidth,
							Y1 = geometry.Bounds.Top - 2,
							Y2 = geometry.Bounds.Top - 2,
							Stroke = Brushes.DarkSlateGray,        // Set your desired color
							StrokeThickness = 2          // Set your desired thickness
						};

						// Add the line to the adornment layer
						layer.AddAdornment(
							AdornmentPositioningBehavior.TextRelative,
                            SnapshotNextLine.Extent,
							null, // tag
							line,
							null  // removal callback
						);
					}

				}
			}
		}
	}
}
