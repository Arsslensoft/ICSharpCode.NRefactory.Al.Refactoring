﻿// 
// ReplaceEmptyString.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Threading;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.Al.Refactoring
{
	[ContextAction("Use string.Empty", Description = "Replaces \"\" with string.Empty")]
	public class ReplaceEmptyStringAction : CodeActionProvider
	{
		public override IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var expr = GetEmptyString(context);
			if (expr == null) {
				yield break;
			}
			yield return new CodeAction(context.TranslateString("Use string.Empty"), script => {
				script.Replace(expr, new PrimitiveType ("string").Member("Empty"));
			}, expr);
		}
		
		static PrimitiveExpression GetEmptyString (RefactoringContext context)
		{
			var node = context.GetNode<PrimitiveExpression> ();
			if (node == null || !(node.Value is string) || node.Value.ToString () != "")
				return null;
			return  node;
		}
	}
}
