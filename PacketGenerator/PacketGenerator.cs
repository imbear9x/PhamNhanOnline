using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
public sealed class PacketGenerator : IIncrementalGenerator
{
    private const int FirstAutoPacketId = 1;

    private static readonly SymbolDisplayFormat MemberTypeDisplayFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly DiagnosticDescriptor DuplicatePacketIdDescriptor = new(
        id: "PG1001",
        title: "Duplicate packet id",
        messageFormat: "Packet '{0}' and packet '{1}' both use id '{2}', packet '{0}' was assigned auto id '{3}'",
        category: "PacketGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidPacketIdDescriptor = new(
        id: "PG1002",
        title: "Invalid packet id",
        messageFormat: "Packet '{0}' uses invalid id '{1}', packet '{0}' was assigned auto id '{2}'",
        category: "PacketGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var packets = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax classDecl && classDecl.AttributeLists.Count > 0,
                static (ctx, _) => GetPacket(ctx))
            .Where(static packet => packet is not null)
            .Select(static (packet, _) => packet!);

        context.RegisterSourceOutput(packets.Collect(), static (spc, packetList) =>
        {
            var packetsByType = packetList
                .GroupBy(static p => p.FullyQualifiedTypeName, StringComparer.Ordinal)
                .Select(static g => g.First())
                .OrderBy(static p => p.FullyQualifiedTypeName, StringComparer.Ordinal)
                .ToList();

            foreach (var packet in packetsByType)
            {
                if (packet.IsPartial)
                {
                    GeneratePacket(spc, packet);
                }
            }

            var assignments = BuildPacketAssignments(spc, packetsByType);
            GenerateRoutingRegistry(spc, assignments);
        });
    }

    private static PacketInfo? GetPacket(GeneratorSyntaxContext ctx)
    {
        var classSyntax = (ClassDeclarationSyntax)ctx.Node;
        if (ctx.SemanticModel.GetDeclaredSymbol(classSyntax) is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        var packetAttributeSymbol = ctx.SemanticModel.Compilation.GetTypeByMetadataName("GameShared.Attributes.PacketAttribute");
        if (packetAttributeSymbol is null)
        {
            return null;
        }

        var packetAttribute = classSymbol
            .GetAttributes()
            .FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, packetAttributeSymbol));

        if (packetAttribute is null)
        {
            return null;
        }

        int? explicitId = null;
        if (packetAttribute.ConstructorArguments.Length > 0 &&
            packetAttribute.ConstructorArguments[0].Value is int packetId)
        {
            explicitId = packetId;
        }

        var namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();
        var accessibility = GetAccessibilityKeyword(classSymbol.DeclaredAccessibility);
        var typeParameterList = BuildTypeParameterList(classSymbol);
        var typeConstraintClauses = BuildTypeConstraintClauses(classSymbol);
        var isPartial = classSymbol.DeclaringSyntaxReferences.Any(static syntaxReference =>
            syntaxReference.GetSyntax() is ClassDeclarationSyntax declaration &&
            declaration.Modifiers.Any(SyntaxKind.PartialKeyword));

        var members = classSymbol
            .GetMembers()
            .Where(static member => member.Kind is SymbolKind.Field or SymbolKind.Property)
            .Select(CreateMemberInfo)
            .Where(static member => member is not null)
            .Select(static member => member!)
            .ToList();

        return new PacketInfo(
            className: classSymbol.Name,
            namespaceName: namespaceName,
            fullyQualifiedTypeName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            displayTypeName: classSymbol.ToDisplayString(),
            accessibility: accessibility,
            typeParameterList: typeParameterList,
            typeConstraintClauses: typeConstraintClauses,
            explicitId: explicitId,
            location: classSyntax.Identifier.GetLocation(),
            isPartial: isPartial,
            members: members);
    }

    private static string GetAccessibilityKeyword(Accessibility accessibility) =>
        accessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => string.Empty
        };

    private static string BuildTypeParameterList(INamedTypeSymbol classSymbol)
    {
        if (classSymbol.TypeParameters.Length == 0)
        {
            return string.Empty;
        }

        return "<" + string.Join(", ", classSymbol.TypeParameters.Select(static parameter => parameter.Name)) + ">";
    }

    private static IReadOnlyList<string> BuildTypeConstraintClauses(INamedTypeSymbol classSymbol)
    {
        var clauses = new List<string>();

        foreach (var typeParameter in classSymbol.TypeParameters)
        {
            var parts = new List<string>();

            if (typeParameter.HasUnmanagedTypeConstraint)
            {
                parts.Add("unmanaged");
            }
            else if (typeParameter.HasValueTypeConstraint)
            {
                parts.Add("struct");
            }
            else if (typeParameter.HasReferenceTypeConstraint)
            {
                parts.Add(typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated
                    ? "class?"
                    : "class");
            }
            else if (typeParameter.HasNotNullConstraint)
            {
                parts.Add("notnull");
            }

            parts.AddRange(typeParameter.ConstraintTypes.Select(static constraintType =>
                constraintType.ToDisplayString(MemberTypeDisplayFormat)));

            if (typeParameter.HasConstructorConstraint)
            {
                parts.Add("new()");
            }

            if (parts.Count > 0)
            {
                clauses.Add($"where {typeParameter.Name} : {string.Join(", ", parts)}");
            }
        }

        return clauses;
    }

    private static string CreatePacketHintName(PacketInfo packet)
    {
        var normalizedTypeName = packet.FullyQualifiedTypeName.Replace("global::", string.Empty);
        var safeTypeName = new string(normalizedTypeName
            .Select(static character => char.IsLetterOrDigit(character) ? character : '_')
            .ToArray());

        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(packet.FullyQualifiedTypeName));
        var shortHash = string.Concat(hashBytes.Take(8).Select(static b => b.ToString("x2")));

        return $"{safeTypeName}_{shortHash}.g.cs";
    }

    private static MemberInfo? CreateMemberInfo(ISymbol symbol)
    {
        if (symbol is IFieldSymbol field)
        {
            if (field.IsConst || field.IsStatic || field.IsImplicitlyDeclared)
            {
                return null;
            }

            return CreateMemberInfo(field.Name, field.Type);
        }

        if (symbol is IPropertySymbol property)
        {
            if (property.IsStatic || property.IsIndexer || property.SetMethod is null || property.IsImplicitlyDeclared)
            {
                return null;
            }

            return CreateMemberInfo(property.Name, property.Type);
        }

        return null;
    }

    private static MemberInfo? CreateMemberInfo(string name, ITypeSymbol type)
    {
        var typeName = type.ToDisplayString(MemberTypeDisplayFormat);

        return TryGetReadExpression(type, out var readExpression) &&
               TryGetWriteStatement(type, name, out var writeStatement)
            ? new MemberInfo(name, typeName, readExpression, writeStatement)
            : null;
    }

    private static bool TryGetReadExpression(ITypeSymbol type, out string readExpression)
    {
        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            var underlyingType = namedType.TypeArguments[0];
            if (TryGetReadExpression(underlyingType, out var underlyingRead))
            {
                var nullableTypeName = type.ToDisplayString(MemberTypeDisplayFormat);
                readExpression = $"({nullableTypeName})({underlyingRead})";
                return true;
            }

            readExpression = string.Empty;
            return false;
        }

        readExpression = string.Empty;

        if (type.SpecialType == SpecialType.System_String)
        {
            readExpression = "global::GameShared.Packets.PacketReader.ReadString(reader)";
            return true;
        }

        if (type.SpecialType == SpecialType.System_Int32)
        {
            readExpression = "global::GameShared.Packets.PacketReader.ReadInt(reader)";
            return true;
        }

        if (type.SpecialType == SpecialType.System_Boolean)
        {
            readExpression = "global::GameShared.Packets.PacketReader.ReadBool(reader)";
            return true;
        }

        if (type.SpecialType == SpecialType.System_Single)
        {
            readExpression = "global::GameShared.Packets.PacketReader.ReadFloat(reader)";
            return true;
        }

        if (type.SpecialType == SpecialType.System_Double)
        {
            readExpression = "global::GameShared.Packets.PacketReader.ReadDouble(reader)";
            return true;
        }

        if (type.SpecialType == SpecialType.System_Int64)
        {
            readExpression = "global::GameShared.Packets.PacketReader.ReadLong(reader)";
            return true;
        }

        if (type.SpecialType == SpecialType.System_Byte)
        {
            readExpression = "global::GameShared.Packets.PacketReader.ReadByte(reader)";
            return true;
        }

        if (type.SpecialType == SpecialType.System_Int16)
        {
            readExpression = "reader.ReadInt16()";
            return true;
        }

        if (type.SpecialType == SpecialType.System_UInt16)
        {
            readExpression = "reader.ReadUInt16()";
            return true;
        }

        if (type.SpecialType == SpecialType.System_UInt32)
        {
            readExpression = "reader.ReadUInt32()";
            return true;
        }

        if (type.SpecialType == SpecialType.System_UInt64)
        {
            readExpression = "reader.ReadUInt64()";
            return true;
        }

        if (type.SpecialType == SpecialType.System_Char)
        {
            readExpression = "reader.ReadChar()";
            return true;
        }

        if (type.SpecialType == SpecialType.System_Decimal)
        {
            readExpression = "reader.ReadDecimal()";
            return true;
        }

        if (type.ToDisplayString() == "System.Guid")
        {
            readExpression = "global::GameShared.Packets.PacketReader.ReadGuid(reader)";
            return true;
        }

        return false;
    }

    private static bool TryGetWriteStatement(ITypeSymbol type, string memberName, out string writeStatement)
    {
        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return TryGetWriteStatement(namedType.TypeArguments[0], $"{memberName}.Value", out writeStatement);
        }

        writeStatement = string.Empty;

        if (type.SpecialType == SpecialType.System_String)
        {
            writeStatement = $"global::GameShared.Packets.PacketWriter.Write(writer, {memberName} ?? string.Empty);";
            return true;
        }

        if (type.SpecialType is SpecialType.System_Int32 or
            SpecialType.System_Boolean or
            SpecialType.System_Single or
            SpecialType.System_Double or
            SpecialType.System_Int64 or
            SpecialType.System_Byte)
        {
            writeStatement = $"global::GameShared.Packets.PacketWriter.Write(writer, {memberName});";
            return true;
        }

        if (type.SpecialType is SpecialType.System_Int16 or
            SpecialType.System_UInt16 or
            SpecialType.System_UInt32 or
            SpecialType.System_UInt64 or
            SpecialType.System_Char or
            SpecialType.System_Decimal)
        {
            writeStatement = $"writer.Write({memberName});";
            return true;
        }

        if (type.ToDisplayString() == "System.Guid")
        {
            writeStatement = $"global::GameShared.Packets.PacketWriter.Write(writer, {memberName});";
            return true;
        }

        return false;
    }

    private static List<PacketAssignment> BuildPacketAssignments(
        SourceProductionContext context,
        IReadOnlyList<PacketInfo> packets)
    {
        var assignments = new List<PacketAssignment>();
        var usedIds = new Dictionary<int, PacketInfo>();
        var pendingAutoIds = new List<PacketInfo>();

        foreach (var packet in packets)
        {
            if (!packet.ExplicitId.HasValue)
            {
                pendingAutoIds.Add(packet);
                continue;
            }

            var explicitId = packet.ExplicitId.Value;
            if (explicitId < 0)
            {
                pendingAutoIds.Add(packet);
                continue;
            }

            if (usedIds.TryGetValue(explicitId, out var existingPacket))
            {
                pendingAutoIds.Add(packet);
                continue;
            }

            usedIds[explicitId] = packet;
            assignments.Add(new PacketAssignment(packet, explicitId));
        }

        var nextAutoId = FirstAutoPacketId;
        foreach (var packet in pendingAutoIds)
        {
            while (usedIds.ContainsKey(nextAutoId))
            {
                nextAutoId++;
            }

            var assignedId = nextAutoId;
            usedIds[assignedId] = packet;
            assignments.Add(new PacketAssignment(packet, assignedId));
            nextAutoId++;
        }

        foreach (var packet in packets)
        {
            if (!packet.ExplicitId.HasValue)
            {
                continue;
            }

            var explicitId = packet.ExplicitId.Value;
            var assigned = assignments.First(a => a.Packet.FullyQualifiedTypeName == packet.FullyQualifiedTypeName);
            if (explicitId < 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidPacketIdDescriptor,
                    packet.Location,
                    packet.DisplayTypeName,
                    explicitId,
                    assigned.Id));
                continue;
            }

            var duplicate = packets.FirstOrDefault(other =>
                other.FullyQualifiedTypeName != packet.FullyQualifiedTypeName &&
                other.ExplicitId.HasValue &&
                other.ExplicitId.Value == explicitId);

            if (duplicate is not null && assigned.Id != explicitId)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DuplicatePacketIdDescriptor,
                    packet.Location,
                    packet.DisplayTypeName,
                    duplicate.DisplayTypeName,
                    explicitId,
                    assigned.Id));
            }
        }

        return assignments
            .OrderBy(static assignment => assignment.Id)
            .ToList();
    }

    private static void GeneratePacket(SourceProductionContext context, PacketInfo packet)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System.IO;");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(packet.NamespaceName))
        {
            sb.AppendLine($"namespace {packet.NamespaceName};");
            sb.AppendLine();
        }

        var declaration = string.IsNullOrWhiteSpace(packet.Accessibility)
            ? $"partial class {packet.ClassName}{packet.TypeParameterList}"
            : $"{packet.Accessibility} partial class {packet.ClassName}{packet.TypeParameterList}";

        sb.AppendLine(declaration);

        foreach (var constraintClause in packet.TypeConstraintClauses)
        {
            sb.AppendLine(constraintClause);
        }

        sb.AppendLine("{");
        sb.AppendLine("    private ulong _mask;");
        sb.AppendLine();

        for (var i = 0; i < packet.Members.Count; i++)
        {
            var member = packet.Members[i];
            sb.AppendLine($"    public bool Has{member.Name} => (_mask & (1UL << {i})) != 0;");
            sb.AppendLine();
            sb.AppendLine($"    public bool TryGet{member.Name}(out {member.TypeName} value)");
            sb.AppendLine("    {");
            sb.AppendLine($"        value = {member.Name};");
            sb.AppendLine($"        return Has{member.Name};");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("    public void Serialize(BinaryWriter writer)");
        sb.AppendLine("    {");
        sb.AppendLine("        ulong mask = 0;");
        sb.AppendLine();

        for (var i = 0; i < packet.Members.Count; i++)
        {
            var member = packet.Members[i];
            sb.AppendLine(
                $"        if (!global::System.Collections.Generic.EqualityComparer<{member.TypeName}>.Default.Equals({member.Name}, default!)) mask |= 1UL << {i};");
        }

        sb.AppendLine();
        sb.AppendLine("        writer.Write(mask);");
        sb.AppendLine();

        for (var i = 0; i < packet.Members.Count; i++)
        {
            var member = packet.Members[i];
            sb.AppendLine($"        if ((mask & (1UL << {i})) != 0)");
            sb.AppendLine($"            {member.WriteStatement}");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public void Deserialize(BinaryReader reader)");
        sb.AppendLine("    {");
        sb.AppendLine("        _mask = reader.ReadUInt64();");
        sb.AppendLine();

        for (var i = 0; i < packet.Members.Count; i++)
        {
            var member = packet.Members[i];
            sb.AppendLine($"        if ((_mask & (1UL << {i})) != 0)");
            sb.AppendLine($"            {member.Name} = {member.ReadExpression};");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource(CreatePacketHintName(packet), SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateRoutingRegistry(SourceProductionContext context, IReadOnlyList<PacketAssignment> assignments)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("namespace GameShared.Packets;");
        sb.AppendLine();
        sb.AppendLine("internal static class PacketGeneratedRegistry");
        sb.AppendLine("{");
        sb.AppendLine("    public static bool TryGetId(IPacket packet, out int id)");
        sb.AppendLine("    {");
        sb.AppendLine("        switch (packet)");
        sb.AppendLine("        {");

        foreach (var assignment in assignments)
        {
            sb.AppendLine($"            case {assignment.Packet.FullyQualifiedTypeName}:");
            sb.AppendLine($"                id = {assignment.Id};");
            sb.AppendLine("                return true;");
        }

        sb.AppendLine("            default:");
        sb.AppendLine("                id = default;");
        sb.AppendLine("                return false;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static IPacket? Create(int id)");
        sb.AppendLine("    {");
        sb.AppendLine("        return id switch");
        sb.AppendLine("        {");

        foreach (var assignment in assignments)
        {
            sb.AppendLine($"            {assignment.Id} => new {assignment.Packet.FullyQualifiedTypeName}(),");
        }

        sb.AppendLine("            _ => null");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("PacketGeneratedRegistry.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private sealed class PacketInfo
    {
        public PacketInfo(
            string className,
            string namespaceName,
            string fullyQualifiedTypeName,
            string displayTypeName,
            string accessibility,
            string typeParameterList,
            IReadOnlyList<string> typeConstraintClauses,
            int? explicitId,
            Location location,
            bool isPartial,
            IReadOnlyList<MemberInfo> members)
        {
            ClassName = className;
            NamespaceName = namespaceName;
            FullyQualifiedTypeName = fullyQualifiedTypeName;
            DisplayTypeName = displayTypeName;
            Accessibility = accessibility;
            TypeParameterList = typeParameterList;
            TypeConstraintClauses = typeConstraintClauses;
            ExplicitId = explicitId;
            Location = location;
            IsPartial = isPartial;
            Members = members;
        }

        public string ClassName { get; }

        public string NamespaceName { get; }

        public string FullyQualifiedTypeName { get; }

        public string DisplayTypeName { get; }

        public string Accessibility { get; }

        public string TypeParameterList { get; }

        public IReadOnlyList<string> TypeConstraintClauses { get; }

        public int? ExplicitId { get; }

        public Location Location { get; }

        public bool IsPartial { get; }

        public IReadOnlyList<MemberInfo> Members { get; }
    }

    private sealed class MemberInfo
    {
        public MemberInfo(string name, string typeName, string readExpression, string writeStatement)
        {
            Name = name;
            TypeName = typeName;
            ReadExpression = readExpression;
            WriteStatement = writeStatement;
        }

        public string Name { get; }

        public string TypeName { get; }

        public string ReadExpression { get; }

        public string WriteStatement { get; }
    }

    private sealed class PacketAssignment
    {
        public PacketAssignment(PacketInfo packet, int id)
        {
            Packet = packet;
            Id = id;
        }

        public PacketInfo Packet { get; }

        public int Id { get; }
    }
}
