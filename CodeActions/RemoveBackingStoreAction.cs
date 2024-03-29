﻿// 
// RemoveBackingStore.cs
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
using System.Linq;
using ICSharpCode.NRefactory.Al.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.Al.Refactoring
{
	[ContextAction("Remove backing store for property", Description = "Removes the backing store of a property and creates an auto property.")]
	public class RemoveBackingStoreAction : CodeActionProvider
	{
		public override IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var property = context.GetNode<PropertyDeclaration>();
			if (property == null || !property.NameToken.Contains(context.Location))
				yield break;

			var field = GetBackingField(context, property);
			if (!IsValidField(field, property.GetParent<TypeDeclaration>())) {
				yield break;
			}
			// create new auto property
			var newProperty = (PropertyDeclaration)property.Clone();
			newProperty.Getter.Body = BlockStatement.Null;
			newProperty.Setter.Body = BlockStatement.Null;
			
			yield return new CodeAction(context.TranslateString("Convert to auto property"), script => {
				script.Rename((IEntity)field, newProperty.Name);
				var oldField = context.RootNode.GetNodeAt<FieldDeclaration>(field.Region.Begin);
				if (oldField.Variables.Count == 1) {
					script.Remove(oldField);
				} else {
					var newField = (FieldDeclaration)oldField.Clone();
					foreach (var init in newField.Variables) {
						if (init.Name == field.Name) {
							init.Remove();
							break;
						}
					}
					script.Replace(oldField, newField);
				}
				script.Replace (property, newProperty);
			}, property.NameToken);
		}

		static bool IsValidField(IField field, TypeDeclaration declaringType)
		{
			if (field == null || field.Attributes.Count > 0)
				return false;
			foreach (var m in declaringType.Members.OfType<FieldDeclaration>()) {
				foreach (var i in m.Variables) {
					if (i.StartLocation == field.BodyRegion.Begin) {
						if (!i.Initializer.IsNull)
							return false;
						break;
					}
				}
			}
			return true;
		}

//		void ReplaceBackingFieldReferences (MDRefactoringContext context, IField backingStore, PropertyDeclaration property)
//		{
//			using (var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)) {
//				foreach (var memberRef in MonoDevelop.Ide.FindInFiles.ReferenceFinder.FindReferences (backingStore, monitor)) {
//					if (property.Contains (memberRef.Line, memberRef.Column))
//						continue;
//					if (backingStore.Location.Line == memberRef.Line && backingStore.Location.Column == memberRef.Column)
//						continue;
//					context.Do (new TextReplaceChange () {
//						FileName = memberRef.FileName,
//						Offset = memberRef.Position,
//						RemovedChars = memberRef.Name.Length,
//						InsertedText = property.Name
//					});
//				}
//			}
//		}
//
		static readonly Version csharp3 = new Version(3, 0);
		
		internal static IField GetBackingField (BaseRefactoringContext context, PropertyDeclaration propertyDeclaration)
		{
			// automatic properties always need getter & setter
			if (propertyDeclaration == null || propertyDeclaration.Getter.IsNull || propertyDeclaration.Setter.IsNull || propertyDeclaration.Getter.Body.IsNull || propertyDeclaration.Setter.Body.IsNull)
				return null;
			if (!context.Supports(csharp3) || propertyDeclaration.HasModifier (ICSharpCode.NRefactory.Al.Modifiers.Abstract) || ((TypeDeclaration)propertyDeclaration.Parent).ClassType == ClassType.Interface)
				return null;
			var getterField = ScanGetter (context, propertyDeclaration);
			if (getterField == null)
				return null;
			var setterField = ScanSetter (context, propertyDeclaration);
			if (setterField == null)
				return null;
			if (!getterField.Equals(setterField))
				return null;
			return getterField;
		}
		
		internal static IField ScanGetter (BaseRefactoringContext context, PropertyDeclaration propertyDeclaration)
		{
			if (propertyDeclaration.Getter.Body.Statements.Count != 1)
				return null;
			var returnStatement = propertyDeclaration.Getter.Body.Statements.First () as ReturnStatement;
			if (returnStatement == null)
				return null;
			if (!IsPossibleExpression(returnStatement.Expression))
				return null;
			var result = context.Resolve (returnStatement.Expression);
			if (result == null || !(result is MemberResolveResult))
				return null;
			return ((MemberResolveResult)result).Member as IField;
		}
		
		internal static IField ScanSetter (BaseRefactoringContext context, PropertyDeclaration propertyDeclaration)
		{
			if (propertyDeclaration.Setter.Body.Statements.Count != 1)
				return null;
			var setAssignment = propertyDeclaration.Setter.Body.Statements.First () as ExpressionStatement;
			var assignment = setAssignment != null ? setAssignment.Expression as AssignmentExpression : null;
			if (assignment == null || assignment.Operator != AssignmentOperatorType.Assign)
				return null;
			var idExpr = assignment.Right as IdentifierExpression;
			if (idExpr == null || idExpr.Identifier != "value")
				return null;
			if (!IsPossibleExpression(assignment.Left))
				return null;
			var result = context.Resolve (assignment.Left);
			if (result == null || !(result is MemberResolveResult))
				return null;
			return ((MemberResolveResult)result).Member as IField;
		}

		static bool IsPossibleExpression(Expression left)
		{
			if (left is IdentifierExpression)
				return true;
			var mr = left as MemberReferenceExpression;
			if (mr == null)
				return false;
			return mr.Target is ThisReferenceExpression;
		}
	}
}

