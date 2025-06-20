﻿using Microsoft.EntityFrameworkCore.Storage;
using BoolTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.BoolTypeMapping;
using ByteArrayTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.ByteArrayTypeMapping;
using ByteTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.ByteTypeMapping;
using CharTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.CharTypeMapping;
using DateTimeOffsetTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.DateTimeOffsetTypeMapping;
using DateTimeTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.DateTimeTypeMapping;
using DecimalTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.DecimalTypeMapping;
using DoubleTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.DoubleTypeMapping;
using FloatTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.FloatTypeMapping;
using GuidTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.GuidTypeMapping;
using IntTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.IntTypeMapping;
using LongTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.LongTypeMapping;
using SByteTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.SByteTypeMapping;
using ShortTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.ShortTypeMapping;
using StringTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.StringTypeMapping;
using TimeSpanTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.TimeSpanTypeMapping;
using UIntTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.UIntTypeMapping;
using ULongTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.ULongTypeMapping;
using UShortTypeMapping = EntityFrameworkCore.LibSql.Storage.TypeMappings.UShortTypeMapping;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlTypeMappingSource : RelationalTypeMappingSource
{
    private readonly Dictionary<Type, RelationalTypeMapping> _clrTypeMappings;
    private readonly Dictionary<string, RelationalTypeMapping> _storeTypeMappings;

    public LibSqlTypeMappingSource(
        TypeMappingSourceDependencies dependencies,
        RelationalTypeMappingSourceDependencies relationalDependencies)
        : base(dependencies, relationalDependencies)
    {
        _clrTypeMappings = new Dictionary<Type, RelationalTypeMapping>
        {
            { typeof(string), new StringTypeMapping("TEXT") },
            { typeof(byte[]), new ByteArrayTypeMapping("BLOB") },
            { typeof(bool), new BoolTypeMapping("INTEGER") },
            { typeof(byte), new ByteTypeMapping("INTEGER") },
            { typeof(char), new CharTypeMapping("INTEGER") },
            { typeof(short), new ShortTypeMapping("INTEGER") },
            { typeof(int), new IntTypeMapping("INTEGER") },
            { typeof(long), new LongTypeMapping("INTEGER") },
            { typeof(sbyte), new SByteTypeMapping("INTEGER") },
            { typeof(ushort), new UShortTypeMapping("INTEGER") },
            { typeof(uint), new UIntTypeMapping("INTEGER") },
            { typeof(ulong), new ULongTypeMapping("INTEGER") },
            { typeof(float), new FloatTypeMapping("REAL") },
            { typeof(double), new DoubleTypeMapping("REAL") },
            { typeof(decimal), new DecimalTypeMapping("TEXT") },
            { typeof(DateTime), new DateTimeTypeMapping("TEXT") },
            { typeof(DateTimeOffset), new DateTimeOffsetTypeMapping("TEXT") },
            { typeof(TimeSpan), new TimeSpanTypeMapping("TEXT") },
            { typeof(Guid), new GuidTypeMapping("TEXT") }
        };

        _storeTypeMappings = new Dictionary<string, RelationalTypeMapping>(StringComparer.OrdinalIgnoreCase)
        {
            { "INTEGER", new LongTypeMapping("INTEGER") },
            { "REAL", new DoubleTypeMapping("REAL") },
            { "TEXT", new StringTypeMapping("TEXT") },
            { "BLOB", new ByteArrayTypeMapping("BLOB") },
            { "NUMERIC", new StringTypeMapping("NUMERIC") }
        };

        Console.WriteLine($"DEBUG LibSqlTypeMappingSource: Initialized with {_clrTypeMappings.Count} CLR mappings and {_storeTypeMappings.Count} store mappings");
    }

    protected override RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
    {
        var clrType = mappingInfo.ClrType;
        var storeTypeName = mappingInfo.StoreTypeName;

        Console.WriteLine($"DEBUG LibSqlTypeMappingSource.FindMapping: CLR={clrType?.Name ?? "null"}, Store={storeTypeName ?? "null"}, Size={mappingInfo.Size}, Precision={mappingInfo.Precision}, Scale={mappingInfo.Scale}");

        // First check for CLR type mappings
        if (clrType != null)
        {
            var nonNullableType = Nullable.GetUnderlyingType(clrType) ?? clrType;
            if (_clrTypeMappings.TryGetValue(nonNullableType, out var clrMapping))
            {
                Console.WriteLine($"DEBUG LibSqlTypeMappingSource: Found CLR mapping for {nonNullableType.Name}: {clrMapping.StoreType} -> {clrMapping.GetType().Name}");
                return clrMapping;
            }
            else
            {
                Console.WriteLine($"DEBUG LibSqlTypeMappingSource: No CLR mapping found for {nonNullableType.Name}");
            }
        }

        // Then check for store type mappings
        if (storeTypeName != null && _storeTypeMappings.TryGetValue(storeTypeName, out var storeMapping))
        {
            Console.WriteLine($"DEBUG LibSqlTypeMappingSource: Found store mapping for {storeTypeName}: {storeMapping.ClrType.Name} -> {storeMapping.GetType().Name}");
            return storeMapping;
        }
        else if (storeTypeName != null)
        {
            Console.WriteLine($"DEBUG LibSqlTypeMappingSource: No store mapping found for {storeTypeName}");
        }

        // Fall back to base implementation
        var baseMapping = base.FindMapping(mappingInfo);
        if (baseMapping != null)
        {
            Console.WriteLine($"DEBUG LibSqlTypeMappingSource: Using base mapping: {baseMapping.GetType().Name} -> {baseMapping.StoreType}");
        }
        else
        {
            Console.WriteLine($"DEBUG LibSqlTypeMappingSource: No base mapping found either!");
        }
        
        return baseMapping;
    }
}