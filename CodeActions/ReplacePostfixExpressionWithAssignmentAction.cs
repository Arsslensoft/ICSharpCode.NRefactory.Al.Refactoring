//
// ReplacePostfixExpressionWithAssignmentAction.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.Al.Refactoring
{
	[ContextAction("Replace postfix expression with assignment", Description = "Replace postfix expression with assignment")]
	public class ReplacePostfixExpressionWithAssignmentAction : SpecializedCodeAction<UnaryOperatorExpression>
	{
		protected override CodeAction GetAction(RefactoringContext context, UnaryOperatorExpression node)
		{
			if (node.Operator != UnaryOperatorType.PostIncrement && node.Operator != UnaryOperatorType.PostDecrement)
				return null;
			string desc = node.Operator == UnaryOperatorType.PostIncrement ? context.TranslateString("Replace with '{0} += 1'") : context.TranslateString("Replace with '{0} -= 1'");
			return new CodeAction(
				string.Format(desc, AlUtil.GetInnerMostExpression(node.Expression)),
				s => s.Replace(node, new AssignmentExpression (
					AlUtil.GetInnerMostExpression(node.Expression).Clone(),
					node.Operator == UnaryOperatorType.PostIncrement ? AssignmentOperatorType.Add : AssignmentOperatorType.Subtract,
					new PrimitiveExpression(1)
				)),
				node.OperatorToken
			);
		}
	}
}

