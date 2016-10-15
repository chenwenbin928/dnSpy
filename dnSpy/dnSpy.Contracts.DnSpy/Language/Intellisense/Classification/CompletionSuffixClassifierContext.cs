﻿/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Language.Intellisense.Classification {
	/// <summary>
	/// Context needed to classify <see cref="Completion.DisplayText"/>
	/// </summary>
	public sealed class CompletionSuffixClassifierContext : CompletionClassifierContext {
		/// <summary>
		/// Returns <see cref="CompletionClassifierKind.Suffix"/>
		/// </summary>
		public override CompletionClassifierKind Kind => CompletionClassifierKind.Suffix;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="completionSet">Completion set</param>
		/// <param name="completion">Completion to classify</param>
		/// <param name="suffix">Text to classify</param>
		public CompletionSuffixClassifierContext(CompletionSet completionSet, Completion completion, string suffix)
			: base(completionSet, completion, suffix) {
		}
	}
}
