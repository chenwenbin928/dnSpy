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

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Roslyn.Shared.Text;
using dnSpy.Roslyn.Shared.Text.Classification;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Roslyn.Shared.Intellisense.Completions {
	[Export(typeof(IUIElementProvider<Completion, ICompletionSession>))]
	[Name(PredefinedUIElementProviderNames.RoslynCompletionToolTipProvider)]
	[ContentType(ContentTypes.RoslynCode)]
	sealed class CompletionToolTipProvider : IUIElementProvider<Completion, ICompletionSession> {
		readonly IContentType contentType;
		readonly ITaggedTextElementProviderService taggedTextElementProviderService;
		WeakReference lastAsyncToolTipContentWeakReference;

		[ImportingConstructor]
		CompletionToolTipProvider(IContentTypeRegistryService contentTypeRegistryService, ITaggedTextElementProviderService taggedTextElementProviderService) {
			this.contentType = contentTypeRegistryService.GetContentType(RoslynContentTypes.CompletionToolTipRoslyn);
			this.taggedTextElementProviderService = taggedTextElementProviderService;
		}

		public UIElement GetUIElement(Completion itemToRender, ICompletionSession context, UIElementType elementType) {
			if (elementType != UIElementType.Tooltip)
				return null;

			var lastAsyncToolTipContent = lastAsyncToolTipContentWeakReference?.Target as AsyncToolTipContent;
			if (lastAsyncToolTipContent?.Session == context) {
				lastAsyncToolTipContent.Cancel();
				lastAsyncToolTipContentWeakReference = null;
			}

			var roslynCompletion = itemToRender as RoslynCompletion;
			if (roslynCompletion == null)
				return null;
			var roslynCollection = context.SelectedCompletionSet as RoslynCompletionSet;
			Debug.Assert(roslynCollection != null);
			if (roslynCollection == null)
				return null;

			var result = new AsyncToolTipContent(this, roslynCollection, roslynCompletion, context, taggedTextElementProviderService);
			lastAsyncToolTipContentWeakReference = result.IsDisposed ? null : new WeakReference(result);
			return result;
		}

		sealed class AsyncToolTipContent : ContentControl {
			public ICompletionSession Session { get; }

			readonly CompletionToolTipProvider owner;
			readonly CancellationTokenSource cancellationTokenSource;
			readonly ITaggedTextElementProviderService taggedTextElementProviderService;

			public AsyncToolTipContent(CompletionToolTipProvider owner, RoslynCompletionSet completionSet, RoslynCompletion completion, ICompletionSession session, ITaggedTextElementProviderService taggedTextElementProviderService) {
				this.owner = owner;
				this.Session = session;
				this.cancellationTokenSource = new CancellationTokenSource();
				this.taggedTextElementProviderService = taggedTextElementProviderService;
				this.Session.Dismissed += Session_Dismissed;
				Unloaded += AsyncToolTipContent_Unloaded;
				GetDescriptionAsync(completionSet, completion, cancellationTokenSource.Token)
				.ContinueWith(t => {
					var ex = t.Exception;
					Dispose();
				}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
			}

			async Task GetDescriptionAsync(RoslynCompletionSet completionSet, RoslynCompletion completion, CancellationToken cancellationToken) {
				var description = await completionSet.GetDescriptionAsync(completion, cancellationToken);
				if (description == null || description.TaggedParts.IsDefault || description.TaggedParts.Length == 0)
					InitializeDefaultDocumentation();
				else
					Content = CreateContent(description);
			}

			object CreateContent(CompletionDescription description) {
				using (var elemProvider = taggedTextElementProviderService.Create(owner.contentType, AppearanceCategoryConstants.CodeCompletionToolTip))
					return elemProvider.Create(description.TaggedParts);
			}

			void InitializeDefaultDocumentation() => Visibility = Visibility.Collapsed;
			void AsyncToolTipContent_Unloaded(object sender, RoutedEventArgs e) => Cancel();
			void Session_Dismissed(object sender, EventArgs e) => Cancel();

			public void Cancel() {
				if (disposed)
					return;
				cancellationTokenSource.Cancel();
				Dispose();
			}

			void Dispose() {
				if (disposed)
					return;
				disposed = true;
				cancellationTokenSource.Dispose();
				Session.Dismissed -= Session_Dismissed;
				Unloaded -= AsyncToolTipContent_Unloaded;
				if (owner.lastAsyncToolTipContentWeakReference?.Target == this)
					owner.lastAsyncToolTipContentWeakReference = null;
			}
			bool disposed;

			public bool IsDisposed => disposed;
		}
	}
}
