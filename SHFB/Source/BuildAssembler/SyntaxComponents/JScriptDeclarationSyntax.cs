// Copyright � Microsoft Corporation.
// This source file is subject to the Microsoft Permissive License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.

// Change history:
// 12/20/2013 - EFW - Updated the syntax generator to be discoverable via MEF

using System;
using System.Globalization;
using System.Xml.XPath;

using Sandcastle.Core.BuildAssembler.SyntaxGenerator;

namespace Microsoft.Ddue.Tools
{
    /// <summary>
    /// This class generates declaration syntax sections for JScript
    /// </summary>
    public sealed class JScriptDeclarationSyntaxGenerator : SyntaxGeneratorTemplate
    {
        #region Syntax generator factory for MEF
        //=====================================================================

        /// <summary>
        /// This is used to create a new instance of the syntax generator
        /// </summary>
        [SyntaxGeneratorExport("JScript", "JScript", "cs", AlternateIds = "jscript#, jscript.net",
          SortOrder = 70, Version = AssemblyInfo.Version, Copyright = AssemblyInfo.Copyright,
          Description = "Generates JScript declaration syntax sections")]
        public sealed class Factory : ISyntaxGeneratorFactory
        {
            /// <inheritdoc />
            public SyntaxGeneratorCore Create()
            {
                return new JScriptDeclarationSyntaxGenerator();
            }
        }
        #endregion

        /// <inheritdoc />
        public override void WriteNamespaceSyntax(XPathNavigator reflection, SyntaxWriter writer)
        {
            string name = (string)reflection.Evaluate(apiNameExpression);

            writer.WriteKeyword("package");
            writer.WriteString(" ");
            writer.WriteIdentifier(name);
        }

        /// <inheritdoc />
        public override void WriteClassSyntax(XPathNavigator reflection, SyntaxWriter writer)
        {
            if(IsUnsupportedGeneric(reflection, writer))
                return;

            string name = (string)reflection.Evaluate(apiNameExpression);
            bool isAbstract = (bool)reflection.Evaluate(apiIsAbstractTypeExpression);
            bool isSealed = (bool)reflection.Evaluate(apiIsSealedTypeExpression);

            WriteAccessModifier(reflection, writer);

            if(isSealed)
            {
                writer.WriteKeyword("final");
                writer.WriteString(" ");
            }
            else if(isAbstract)
            {
                writer.WriteKeyword("abstract");
                writer.WriteString(" ");
            }
            writer.WriteKeyword("class");
            writer.WriteString(" ");
            writer.WriteString(name);
            WriteBaseClass(reflection, writer);
            WriteInterfaceList(reflection, writer);
        }

        /// <inheritdoc />
        public override void WriteStructureSyntax(XPathNavigator reflection, SyntaxWriter writer)
        {
            if(IsUnsupportedGeneric(reflection, writer))
                return;
            writer.WriteMessage("UnsupportedStructure_" + Language);
        }

        /// <inheritdoc />
        public override void WriteInterfaceSyntax(XPathNavigator reflection, SyntaxWriter writer)
        {
            if(IsUnsupportedGeneric(reflection, writer))
                return;

            string name = (string)reflection.Evaluate(apiNameExpression);

            WriteAccessModifier(reflection, writer);
            writer.WriteKeyword("interface");
            writer.WriteString(" ");
            writer.WriteString(name);
            WriteInterfaceList("extends", reflection, writer);
        }

        /// <inheritdoc />
        public override void WriteDelegateSyntax(XPathNavigator reflection, SyntaxWriter writer)
        {
            if(IsUnsupportedGeneric(reflection, writer))
                return;
            writer.WriteMessage("UnsupportedDelegate_" + Language);
        }

        /// <inheritdoc />
        public override void WriteEnumerationSyntax(XPathNavigator reflection, SyntaxWriter writer)
        {
            string name = (string)reflection.Evaluate(apiNameExpression);

            WriteAccessModifier(reflection, writer);
            writer.WriteKeyword("enum");
            writer.WriteString(" ");
            writer.WriteString(name);
            // no JScript support for underlying types
        }

        /// <inheritdoc />
        public override void WriteConstructorSyntax(XPathNavigator reflection, SyntaxWriter writer)
        {
            bool isStatic = (bool)reflection.Evaluate(apiIsStaticExpression);
            if(isStatic)
            {
                writer.WriteMessage("UnsupportedStaticConstructor_" + Language);
            }
            else
            {
                if(IsUnsupportedUnsafe(reflection, writer))
                    return;
                XPathNavigator declaringType = reflection.SelectSingleNode(apiContainingTypeExpression);

                WriteAccessModifier(reflection, writer);
                writer.WriteKeyword("function");
                writer.WriteString(" ");
                WriteTypeReference(declaringType, writer);
                WriteParameterList(reflection, writer);
            }
        }

        /// <inheritdoc />
        public override void WriteNormalMethodSyntax(XPathNavigator reflection, SyntaxWriter writer)
        {
            if(IsUnsupportedUnsafe(reflection, writer))
                return;
            if(IsUnsupportedGeneric(reflection, writer))
                return;
            if(IsUnsupportedExplicit(reflection, writer))
                return;
            if(IsUnsupportedVarargs(reflection, writer))
                return;

            string name = (string)reflection.Evaluate(apiNameExpression);
            XPathNavigator returnType = reflection.SelectSingleNode(apiReturnTypeExpression);

            WriteProcedureModifiers(reflection, writer);
            writer.WriteKeyword("function");
            writer.WriteString(" ");
            writer.WriteIdentifier(name);
            WriteParameterList(reflection, writer);
            if(returnType != null)
            {
                writer.WriteString(" : ");
                WriteTypeReference(returnType, writer);
            }
        }

        /// <inheritdoc />
        public override void WritePropertySyntax(XPathNavigator reflection, SyntaxWriter writer)
        {
            if(IsUnsupportedUnsafe(reflection, writer))
                return;
            if(IsUnsupportedExplicit(reflection, writer))
                return;

            if(reflection.Select(apiParametersExpression).Count > 0)
            {
                writer.WriteMessage("UnsupportedIndex_" + Language);
                return;
            }

            string name = (string)reflection.Evaluate(apiNameExpression);
            bool isGettable = (bool)reflection.Evaluate(apiIsReadPropertyExpression);
            bool isSettable = (bool)reflection.Evaluate(apiIsWritePropertyExpression);
            XPathNavigator type = reflection.SelectSingleNode(apiReturnTypeExpression);

            if(isGettable)
            {
                string getVisibility = (string)reflection.Evaluate(apiGetVisibilityExpression);

                if(!String.IsNullOrEmpty(getVisibility))
                {
                    WriteVisibility(getVisibility, writer);
                    writer.WriteString(" ");
                }

                WriteProcedureModifiers(reflection, writer);
                writer.WriteKeyword("function get");
                writer.WriteString(" ");
                writer.WriteIdentifier(name);
                writer.WriteString(" () : ");
                WriteTypeReference(type, writer);
                writer.WriteLine();
            }

            if(isSettable)
            {
                string setVisibility = (string)reflection.Evaluate(apiSetVisibilityExpression);

                if(!String.IsNullOrEmpty(setVisibility))
                {
                    WriteVisibility(setVisibility, writer);
                    writer.WriteString(" ");
                }

                WriteProcedureModifiers(reflection, writer);
                writer.WriteKeyword("function set");
                writer.WriteString(" ");
                writer.WriteIdentifier(name);
                writer.WriteString(" (");
                writer.WriteParameter("value");
                writer.WriteString(" : ");
                WriteTypeReference(type, writer);
                writer.WriteString(")");
            }

        }

        /// <inheritdoc />
        public override void WriteFieldSyntax(XPathNavigator reflection, SyntaxWriter writer)
        {
            if(IsUnsupportedUnsafe(reflection, writer))
                return;

            string name = (string)reflection.Evaluate(apiNameExpression);
            bool isStatic = (bool)reflection.Evaluate(apiIsStaticExpression);
            bool isLiteral = (bool)reflection.Evaluate(apiIsLiteralFieldExpression);
            bool isInitOnly = (bool)reflection.Evaluate(apiIsInitOnlyFieldExpression);
            XPathNavigator type = reflection.SelectSingleNode(apiReturnTypeExpression);

            WriteAccessModifier(reflection, writer);

            if(isStatic)
            {
                if(isLiteral)
                    writer.WriteKeyword("const");
                else
                    writer.WriteKeyword("static");

                writer.WriteString(" ");
            }

            if(isInitOnly)
            {
                writer.WriteKeyword("final");
                writer.WriteString(" ");
            }

            writer.WriteKeyword("var");
            writer.WriteString(" ");
            writer.WriteIdentifier(name);
            writer.WriteString(" : ");
            WriteTypeReference(type, writer);
        }

        /// <inheritdoc />
        public override void WriteOperatorSyntax(XPathNavigator reflection, SyntaxWriter writer)
        {
            writer.WriteMessage("UnsupportedOperator_" + Language);
        }

        /// <inheritdoc />
        public override void WriteCastSyntax(XPathNavigator reflection, SyntaxWriter writer)
        {
            writer.WriteMessage("UnsupportedCast_" + Language);
        }

        /// <inheritdoc />
        public override void WriteEventSyntax(XPathNavigator reflection, SyntaxWriter writer)
        {
            writer.WriteMessage("UnsupportedEvent_" + Language);
        }

        private static void WriteBaseClass(XPathNavigator reflection, SyntaxWriter writer)
        {
            XPathNavigator baseClass = reflection.SelectSingleNode(apiBaseClassExpression);

            if((baseClass != null) && !((bool)baseClass.Evaluate(typeIsObjectExpression)))
            {
                writer.WriteString(" ");
                writer.WriteKeyword("extends");
                writer.WriteString(" ");
                WriteTypeReference(baseClass, writer);
            }
        }

        private static void WriteInterfaceList(XPathNavigator reflection, SyntaxWriter writer)
        {
            WriteInterfaceList("implements", reflection, writer);
        }

        private static void WriteInterfaceList(string keyword, XPathNavigator reflection, SyntaxWriter writer)
        {
            XPathNodeIterator implements = reflection.Select(apiImplementedInterfacesExpression);

            if(implements.Count == 0)
                return;

            writer.WriteString(" ");
            writer.WriteKeyword(keyword);
            writer.WriteString(" ");

            while(implements.MoveNext())
            {
                XPathNavigator implement = implements.Current;
                WriteTypeReference(implement, writer);
                if(implements.CurrentPosition < implements.Count)
                    writer.WriteString(", ");
            }
        }

        private static void WriteProcedureModifiers(XPathNavigator reflection, SyntaxWriter writer)
        {
            // interface members don't get modified
            string typeSubgroup = (string)reflection.Evaluate(apiContainingTypeSubgroupExpression);

            if(typeSubgroup == "interface")
                return;

            string subgroup = (string)reflection.Evaluate(apiSubgroupExpression);

            if(subgroup != "property")
                WriteAccessModifier(reflection, writer);

            // instance or virtual, static or abstract, etc.
            bool isStatic = (bool)reflection.Evaluate(apiIsStaticExpression);
            bool isVirtual = (bool)reflection.Evaluate(apiIsVirtualExpression);
            bool isAbstract = (bool)reflection.Evaluate(apiIsAbstractProcedureExpression);
            bool isFinal = (bool)reflection.Evaluate(apiIsFinalExpression);
            bool isOverride = (bool)reflection.Evaluate(apiIsOverrideExpression);

            if(isStatic)
            {
                writer.WriteKeyword("static");
                writer.WriteString(" ");
            }
            else
            {
                // all members are virtual in JScript, so no virtual keyword is required
                if(isVirtual)
                {
                    if(isAbstract)
                    {
                        writer.WriteKeyword("abstract");
                        writer.WriteString(" ");
                    }
                    else
                    {
                        if(isOverride)
                        {
                            writer.WriteKeyword("override");
                            writer.WriteString(" ");
                        }
                        if(isFinal)
                        {
                            writer.WriteKeyword("final");
                            writer.WriteString(" ");
                        }
                    }
                }
            }
        }

        private static void WriteParameterList(XPathNavigator reflection, SyntaxWriter writer)
        {
            WriteParameterList(reflection, writer, true);
        }

        private static void WriteParameterList(XPathNavigator reflection, SyntaxWriter writer, bool newlines)
        {
            XPathNodeIterator parameters = reflection.Select(apiParametersExpression);

            writer.WriteString("(");

            if(newlines && (parameters.Count > 0))
                writer.WriteLine();

            while(parameters.MoveNext())
            {
                XPathNavigator parameter = parameters.Current;

                if(newlines)
                    writer.WriteString("\t");

                WriteParameter(parameter, writer);

                if(parameters.CurrentPosition < parameters.Count)
                    writer.WriteString(", ");

                if(newlines)
                    writer.WriteLine();
            }

            writer.WriteString(")");
        }

        // JScript has no in, out, optional, or reference parameters
        private static void WriteParameter(XPathNavigator parameter, SyntaxWriter writer)
        {
            string name = (string)parameter.Evaluate(parameterNameExpression);
            XPathNavigator type = parameter.SelectSingleNode(parameterTypeExpression);
            bool isParamArray = (bool)parameter.Evaluate(parameterIsParamArrayExpression);

            if(isParamArray)
                writer.WriteString("... ");

            writer.WriteParameter(name);
            writer.WriteString(" : ");
            WriteTypeReference(type, writer);
        }

        private static void WriteAccessModifier(XPathNavigator reflection, SyntaxWriter writer)
        {
            string visibility = reflection.Evaluate(apiVisibilityExpression).ToString();

            switch(visibility)
            {
                case "public":
                    writer.WriteKeyword("public");
                    break;

                case "family":
                    writer.WriteKeyword("protected");
                    break;

                case "assembly":
                    writer.WriteKeyword("internal");
                    break;

                case "private":
                    writer.WriteKeyword("private");
                    break;

                case "family and assembly":
                case "family or assembly":
                    // These aren't handled in JScript
                    return;
            }

            writer.WriteString(" ");
        }

        private static void WriteTypeReference(XPathNavigator reference, SyntaxWriter writer)
        {
            switch(reference.LocalName)
            {
                case "arrayOf":
                    int rank = Convert.ToInt32(reference.GetAttribute("rank", String.Empty),
                        CultureInfo.InvariantCulture);

                    XPathNavigator element = reference.SelectSingleNode(typeExpression);
                    WriteTypeReference(element, writer);
                    writer.WriteString("[");

                    for(int i = 1; i < rank; i++)
                        writer.WriteString(",");

                    writer.WriteString("]");
                    break;

                case "pointerTo":
                    XPathNavigator pointee = reference.SelectSingleNode(typeExpression);
                    WriteTypeReference(pointee, writer);
                    writer.WriteString("*");
                    break;
                case "referenceTo":
                    XPathNavigator referee = reference.SelectSingleNode(typeExpression);
                    WriteTypeReference(referee, writer);
                    break;
                case "type":
                    string id = reference.GetAttribute("api", String.Empty);
                    WriteNormalTypeReference(id, writer);
                    XPathNodeIterator typeModifiers = reference.Select(typeModifiersExpression);
                    while(typeModifiers.MoveNext())
                    {
                        WriteTypeReference(typeModifiers.Current, writer);
                    }
                    break;
                case "template":
                    string name = reference.GetAttribute("name", String.Empty);
                    writer.WriteString(name);
                    XPathNodeIterator modifiers = reference.Select(typeModifiersExpression);
                    while(modifiers.MoveNext())
                    {
                        WriteTypeReference(modifiers.Current, writer);
                    }
                    break;
                case "specialization":
                    writer.WriteString("<");
                    XPathNodeIterator arguments = reference.Select(specializationArgumentsExpression);
                    while(arguments.MoveNext())
                    {
                        if(arguments.CurrentPosition > 1)
                            writer.WriteString(", ");
                        WriteTypeReference(arguments.Current, writer);
                    }
                    writer.WriteString(">");
                    break;
            }
        }

        private static void WriteNormalTypeReference(string api, SyntaxWriter writer)
        {
            switch(api)
            {
                case "T:System.Boolean":
                    writer.WriteReferenceLink(api, "boolean");
                    break;
                case "T:System.Byte":
                    writer.WriteReferenceLink(api, "byte");
                    break;
                case "T:System.SByte":
                    writer.WriteReferenceLink(api, "sbyte");
                    break;
                case "T:System.Char":
                    writer.WriteReferenceLink(api, "char");
                    break;
                case "T:System.Int16":
                    writer.WriteReferenceLink(api, "short");
                    break;
                case "T:System.Int32":
                    writer.WriteReferenceLink(api, "int");
                    break;
                case "T:System.Int64":
                    writer.WriteReferenceLink(api, "long");
                    break;
                case "T:System.UInt16":
                    writer.WriteReferenceLink(api, "ushort");
                    break;
                case "T:System.UInt32":
                    writer.WriteReferenceLink(api, "uint");
                    break;
                case "T:System.UInt64":
                    writer.WriteReferenceLink(api, "ulong");
                    break;
                case "T:System.Single":
                    writer.WriteReferenceLink(api, "float");
                    break;
                case "T:System.Double":
                    writer.WriteReferenceLink(api, "double");
                    break;
                case "T:System.Decimal":
                    writer.WriteReferenceLink(api, "decimal");
                    break;
                default:
                    writer.WriteReferenceLink(api);
                    break;
            }
        }

        private static void WriteVisibility(string visibility, SyntaxWriter writer)
        {
            switch(visibility)
            {
                case "public":
                    writer.WriteKeyword("public");
                    break;
                case "family":
                    writer.WriteKeyword("protected");
                    break;
                case "family or assembly":
                    // this isn't handled in JScript
                    break;
                case "assembly":
                    writer.WriteKeyword("internal");
                    break;
                case "private":
                    writer.WriteKeyword("private");
                    break;
                case "family and assembly":
                    // this isn't handled in JScript
                    break;
            }
        }
    }
}