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
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Media;

namespace MethodSeparators
{
	internal sealed class MethodSeparator_TextAdornment : IDisposable
	{
		private string FilePath;

		private readonly IAdornmentLayer layer;
		private readonly IWpfTextView view;

		List<int> updatedLines = new List<int>();

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
			updatedLines.Clear();
			foreach (ITextViewLine textViewLine in textViewLayoutChangedEventArgs.NewOrReformattedLines)
			{
				updatedLines.Add(textViewLine.Start.GetContainingLineNumber());
			}
			RefreshVisuals();
		}

		enum ESeparationType
		{
			None,
			Function,
			Struct,
			Class,
			Enum,
		}

		private ESeparationType CategorizeNextLine(string text)
		{
			// Simple heuristic for C++ function definition:
			// - Line ends with '{' or optionally with ')' (for single-line function signatures)
			// - Contains '(' and ')'
			// - Does not start with '//' (not a comment)
			// - Does not contain ';' (not a declaration or prototype)
			bool isFunctionDef =
				!string.IsNullOrEmpty(text) &&
				!text.StartsWith("//") &&
				!text.StartsWith("#") &&
				!text.StartsWith("else if") &&
				!text.StartsWith(",") &&
				text.Contains("(") &&
				text.Contains(")") &&
				!text.Contains(";") &&
				(text.EndsWith("{") || text.EndsWith(")") || text.EndsWith("noexcept") || text.EndsWith("const") || text.EndsWith("final") || text.EndsWith("override"));

			if (isFunctionDef)
			{
				int firstParenthesisIndex = text.IndexOf('(');
				// check that the line has a return type or access modifier
				string beforeParenthesis = text.Substring(0, firstParenthesisIndex).Trim();
				string[] tokens = beforeParenthesis.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				isFunctionDef = tokens.Length >= 2 || beforeParenthesis.Contains("::"); // at least return type and function name
			}

			if (isFunctionDef)
			{
				return ESeparationType.Function;
			}

			if (text.StartsWith("class "))
			{
				return ESeparationType.Class;
			}

			if (text.StartsWith("struct"))
			{
				return ESeparationType.Struct;
			}

			if (text.StartsWith("enum"))
			{
				return ESeparationType.Enum;
			}

			return ESeparationType.None;
		}

		private void RefreshVisuals()
		{
			double lineSeparatorThickness = Options.GeneralOptions.Instance.LineSeparatorThickness;
			foreach (int LineNumber in updatedLines)
			{
				ESeparationType separationType = ESeparationType.None;
				// if next line is a function definition then draw horizontal line
				if (LineNumber < 0 || LineNumber >= this.view.TextSnapshot.LineCount)
					continue;

				ITextSnapshotLine nextLine = this.view.TextSnapshot.GetLineFromLineNumber(LineNumber);
				string nextLineText = nextLine.GetText();
				separationType = CategorizeNextLine(nextLineText.Trim());

				if (separationType == ESeparationType.None)
					continue;

				{
					ITextSnapshotLine SnapshotNextLine = this.view.TextSnapshot.GetLineFromLineNumber(LineNumber);
					int startOffset = nextLineText.Length - nextLineText.TrimStart().Length;
					SnapshotSpan span = new SnapshotSpan(SnapshotNextLine.Start.Add(startOffset), nextLineText.Length - startOffset);
					Geometry geometry = this.view.TextViewLines.GetMarkerGeometry(span);
					if (geometry != null)
					{
						System.Windows.Shapes.Line line = new System.Windows.Shapes.Line
						{
							X1 = geometry.Bounds.Left,
							X2 = geometry.Bounds.Left + this.view.ViewportWidth,
							Y1 = geometry.Bounds.Top - 2,
							Y2 = geometry.Bounds.Top - 2,
							Stroke = Brushes.DarkSlateGray,
							StrokeThickness = lineSeparatorThickness
						};
						layer.AddAdornment(
							AdornmentPositioningBehavior.TextRelative,
							SnapshotNextLine.Extent,
							null,
							line,
							null
						);
					}
				}
			}
		}
	}
}
