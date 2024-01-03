using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;

namespace NonSucking.Framework.Serialization.Advanced;

internal class KnownSimpleTypeSerializer
{
    private enum KnownTypes
    {
        None,
        IpAddress,
        Guid,
        BigInteger
    }
    private static KnownTypes GetKnownType(Type type)
    {
        if (type == typeof(IPAddress))
        {
            return KnownTypes.IpAddress;
        }
        if (type == typeof(Guid))
        {
            return KnownTypes.Guid;
        }
        if (type == typeof(BigInteger))
        {
            return KnownTypes.BigInteger;
        }
        return KnownTypes.None;
    }

    private static void GetIpAddressSize(BaseGenerator generator, LocalBuilder addressFamilyVar)
    {
        generator.EmitIf(gen =>
                         {
                             var throwNotSupported = typeof(NotSupportedException).GetConstructor(
                                 BindingFlags.Instance | BindingFlags.Public,
                                 Type.EmptyTypes)
                                 ?? throw new InvalidProgramException("NotSupportedException is missing an empty constructor");
                             gen.Il.Emit(OpCodes.Newobj, throwNotSupported);
                             gen.Il.Emit(OpCodes.Throw);
                         },
            (
                OpCodes.Bne_Un,
                gen =>
                {
                    gen.Il.Emit(OpCodes.Ldloc, addressFamilyVar);
                    gen.Il.Emit(OpCodes.Ldc_I4, (int)AddressFamily.InterNetworkV6);
                },
                gen =>
                {
                    gen.Il.Emit(OpCodes.Ldc_I4, 16);
                }
            ),
            (
                OpCodes.Bne_Un,
                gen =>
                {
                    gen.Il.Emit(OpCodes.Ldloc, addressFamilyVar);
                    gen.Il.Emit(OpCodes.Ldc_I4, (int)AddressFamily.InterNetwork);
                },
                gen =>
                {
                    gen.Il.Emit(OpCodes.Ldc_I4, 4);
                }
            )
        );
    }

    private static MethodInfo? GetTryWriteBytes<T>(Type? discardType = null)
    {
        var paramTypes = discardType is not null ? new[] { typeof(Span<byte>), discardType.MakeByRefType() } : new[] { typeof(Span<byte>) };
        return typeof(T).GetMethod(
            "TryWriteBytes",
            BindingFlags.Instance | BindingFlags.Public,
            paramTypes);
    }
    private static MethodInfo? GetTryWriteBytesBigInt(Type discardType)
    {
        var paramTypes = new[] { typeof(Span<byte>), discardType.MakeByRefType(), typeof(bool), typeof(bool)};
        return typeof(BigInteger).GetMethod(
            "TryWriteBytes",
            BindingFlags.Instance | BindingFlags.Public,
            paramTypes);
    }
    private static void TryWriteBytes(BaseGenerator generator, MethodInfo tryWriteBytes, LocalBuilder buffer, LocalBuilder? discard = null)
    {
        generator.CurrentContext.GetValueRef!(generator.Il);
        generator.Il.Emit(OpCodes.Ldloc, buffer);
        if (discard is not null)
        {
            generator.Il.Emit(OpCodes.Ldloca, discard);
        }

        foreach (var _ in tryWriteBytes.GetParameters().Skip(discard is not null ? 2 : 1))
        {
            generator.Il.Emit(OpCodes.Ldc_I4_0);
        }

        generator.Il.Emit(OpCodes.Callvirt, tryWriteBytes);
        generator.Il.Emit(OpCodes.Pop);
    }

    private static LocalBuilder GetSerializableBuffer(SerializeGenerator generator, Type type, KnownTypes knownType)
    {
        var writeInt = Helper.GetWrite(generator, typeof(int));
        
        switch (knownType)
        {
            case KnownTypes.IpAddress:
            {
                var addressFamilyProp = type.GetProperty("AddressFamily", BindingFlags.Instance | BindingFlags.Public)!;
                var addressFamilyVar = generator.Il.DeclareLocal(typeof(AddressFamily));
                generator.CurrentContext.GetValueRef!(generator.Il);
                generator.Il.Emit(OpCodes.Callvirt, addressFamilyProp.GetMethod!);
                generator.Il.Emit(OpCodes.Stloc, addressFamilyVar);
                generator.CurrentContext.GetReaderWriter(generator.Il);
                generator.Il.Emit(OpCodes.Ldloc, addressFamilyVar);
                generator.Il.Emit(OpCodes.Callvirt, writeInt);

                var tryWriteBytes = GetTryWriteBytes<IPAddress>(typeof(int));

                LocalBuilder bufferVar;
                if (tryWriteBytes is null)
                {
                    bufferVar = GetSimpleByteBuffer(generator, type, "GetAddressBytes");
                }
                else
                {
                    GetIpAddressSize(generator, addressFamilyVar);

                    var bufferSizeVar = generator.Il.DeclareLocal(typeof(int));
                
                    generator.Il.Emit(OpCodes.Dup);
                    generator.Il.Emit(OpCodes.Stloc, bufferSizeVar);
                    generator.Il.Emit(OpCodes.Localloc);
                    generator.Il.Emit(OpCodes.Ldloc, bufferSizeVar);

                    bufferVar = CreateSpanByteBuffer(generator);

                    var discardInt = bufferSizeVar; // Reuse bufferSizeVar for out discard
                
                    TryWriteBytes(generator, tryWriteBytes, bufferVar, discardInt);
                }

                return bufferVar;
            }
            case KnownTypes.Guid:
            {
                var tryWriteBytes = GetTryWriteBytes<Guid>();

                LocalBuilder bufferVar;
                if (tryWriteBytes is null)
                {
                    bufferVar = GetSimpleByteBuffer(generator, type, "ToByteArray");
                }
                else
                {
                    generator.Il.Emit(OpCodes.Ldc_I4, 16);
                    generator.Il.Emit(OpCodes.Localloc);
                    generator.Il.Emit(OpCodes.Ldc_I4, 16);

                    bufferVar = CreateSpanByteBuffer(generator);
                    TryWriteBytes(generator, tryWriteBytes, bufferVar);
                }

                return bufferVar;
            }
            case KnownTypes.BigInteger:
            {
                var getByteCount = type.GetMethod("GetByteCount", BindingFlags.Instance | BindingFlags.Public)!;
                generator.CurrentContext.GetValueRef!(generator.Il);
                if (getByteCount.GetParameters().Length == 1)
                    generator.Il.Emit(OpCodes.Ldc_I4_0);
                generator.Il.Emit(OpCodes.Callvirt, getByteCount);
                var bufferSizeVar = generator.Il.DeclareLocal(typeof(int));
                generator.Il.Emit(OpCodes.Stloc, bufferSizeVar);
                
                generator.CurrentContext.GetReaderWriter(generator.Il);
                generator.Il.Emit(OpCodes.Ldloc, bufferSizeVar);
                generator.Il.Emit(OpCodes.Callvirt, writeInt);
                
                var tryWriteBytes = GetTryWriteBytesBigInt(bufferSizeVar.LocalType);

                LocalBuilder bufferVar;
                if (tryWriteBytes is null)
                {
                    bufferVar = GetSimpleByteBuffer(generator, type, "ToByteArray");
                }
                else
                {
                    generator.Il.Emit(OpCodes.Ldloc, bufferSizeVar);
                    generator.Il.Emit(OpCodes.Localloc);
                    generator.Il.Emit(OpCodes.Ldloc, bufferSizeVar);

                    bufferVar = CreateSpanByteBuffer(generator);
                    TryWriteBytes(generator, tryWriteBytes, bufferVar, bufferSizeVar);
                }

                return bufferVar;
            }
            default:
                throw new NotSupportedException($"{knownType} is not a valid type to be serialized.");
        }
    }

    private static LocalBuilder CreateSpanByteBuffer(BaseGenerator generator)
    {
        var byteSpanCtor = typeof(Span<byte>).GetConstructor(BindingFlags.Instance | BindingFlags.Public,
            new[] { typeof(void*), typeof(int) })
            ?? throw new InvalidProgramException("Span<byte> is missing constructor(void*, int).");

        var bufferVar = generator.Il.DeclareLocal(typeof(Span<byte>));
        generator.Il.Emit(OpCodes.Newobj, byteSpanCtor);

        generator.Il.Emit(OpCodes.Stloc, bufferVar);
        return bufferVar;
    }

    private static LocalBuilder GetSimpleByteBuffer(BaseGenerator generator, Type type, string methodName)
    {
        var getAddressBytes = type.GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes);
        if (getAddressBytes is null)
            throw new NotSupportedException(
                $"{type} type is missing 'TryWriteBytes(Span<byte>, out int)' or '{methodName}' method");

        var bufferVar = generator.Il.DeclareLocal(getAddressBytes.ReturnType);

        generator.CurrentContext.GetValueRef!(generator.Il);
        generator.Il.Emit(OpCodes.Callvirt, getAddressBytes);

        generator.Il.Emit(OpCodes.Stloc, bufferVar);
        return bufferVar;
    }

    public static bool Serialize(SerializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var type = generator.CurrentContext.ValueType.Type;

        var knownType = GetKnownType(type);

        if (knownType == KnownTypes.None)
            return false;

        var buffer = GetSerializableBuffer(generator, type, knownType);

        var writeMethod = generator.CurrentContext.WriterType.GetMethod("Write",
            BindingFlags.Public | BindingFlags.Instance, new[] { buffer.LocalType });

        if (writeMethod is null && buffer.LocalType != typeof(ReadOnlySpan<byte>))
        {
            writeMethod ??= generator.CurrentContext.WriterType.GetMethod("Write",
                BindingFlags.Public | BindingFlags.Instance, new[] { typeof(ReadOnlySpan<byte>) });
        }

        if (writeMethod is null)
            throw new NotImplementedException($"Missing Write implementation on writer for type {buffer.LocalType}");

        generator.CurrentContext.GetReaderWriter(generator.Il);
        generator.Il.Emit(OpCodes.Ldloc, buffer);
        generator.Il.Emit(OpCodes.Callvirt, writeMethod);

        return true;
    }
    public static bool Deserialize(DeserializeGenerator generator, TypeNameCache.TypeConfig typeConfig, int baseRecursionDepth = int.MaxValue)
    {
        var type = generator.CurrentContext.ValueType.Type;
        var readInt = Helper.GetRead(generator, typeof(int));

        var knownType = GetKnownType(type);

        if (knownType == KnownTypes.None)
            return false;

        var ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance,
            new[] { typeof(ReadOnlySpan<byte>), typeof(bool), typeof(bool) });
        bool useSpans = true;
        if (ctor is null)
        {
            useSpans = false;
            ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public,
                new[] { typeof(byte[]) });
        }

        if (ctor is null)
        {
            throw new NotSupportedException($"No matching {knownType} ctor found taking either 'byte[]' or 'ReadOnlySpan<byte>'");
        }
        
        switch (knownType)
        {
            case KnownTypes.IpAddress:
                var addressFamilyVar = generator.Il.DeclareLocal(typeof(AddressFamily));

                generator.CurrentContext.GetReaderWriter(generator.Il);
                generator.Il.Emit(OpCodes.Callvirt, readInt);
                generator.Il.Emit(OpCodes.Stloc, addressFamilyVar);

                GetIpAddressSize(generator, addressFamilyVar);

                break;
            case KnownTypes.Guid:
                generator.Il.Emit(OpCodes.Ldc_I4, 16);
                break;
            case KnownTypes.BigInteger:
                generator.CurrentContext.GetReaderWriter(generator.Il);
                generator.Il.Emit(OpCodes.Callvirt, readInt);
                break;
            default:
                throw new NotSupportedException($"{knownType} is not supported to be serialized.");
        }
        
        LocalBuilder bufferVar;
        var bufferSizeVar = generator.Il.DeclareLocal(typeof(int));
        if (useSpans)
        {
            generator.Il.Emit(OpCodes.Dup);
            generator.Il.Emit(OpCodes.Stloc, bufferSizeVar);
            generator.Il.Emit(OpCodes.Localloc);
            generator.Il.Emit(OpCodes.Ldloc, bufferSizeVar);

            bufferVar = CreateSpanByteBuffer(generator);
            var readBytes = MethodResolver.GetBestMatch(generator.CurrentContext.ReaderType, "ReadBytes",
                BindingFlags.Public | BindingFlags.Instance,
                new[] { typeof(Span<byte>) }, null)
                            ?? throw new InvalidOperationException($"{generator.CurrentContext.ReaderType} does not have a valid void ReadBytes(Span<byte>) overload.");
            generator.CurrentContext.GetReaderWriter(generator.Il);
            generator.Il.Emit(OpCodes.Ldloc, bufferVar);
            generator.EmitCall(readBytes);
        }
        else
        {
            var readBytes = MethodResolver.GetBestMatch(generator.CurrentContext.ReaderType, "ReadBytes",
                BindingFlags.Public | BindingFlags.Instance,
                new[] { typeof(int) }, typeof(byte[]))
                            ?? throw new InvalidOperationException($"{generator.CurrentContext.ReaderType} does not have a valid byte[] ReadBytes(int) overload.");
            generator.Il.Emit(OpCodes.Stloc, bufferSizeVar);
            generator.CurrentContext.GetReaderWriter(generator.Il);
            generator.Il.Emit(OpCodes.Ldloc, bufferSizeVar);
            generator.EmitCall(readBytes);

            bufferVar = generator.Il.DeclareLocal(typeof(byte[]));
            generator.Il.Emit(OpCodes.Stloc, bufferVar);
        }

        generator.Il.Emit(OpCodes.Ldloc, bufferVar);
        foreach (var _ in ctor.GetParameters().Skip(1))
        {
            generator.Il.Emit(OpCodes.Ldc_I4_0);
        }
        generator.Il.Emit(OpCodes.Newobj, ctor);

        generator.CurrentContext.SetValue!(generator.Il);

        return true;
    }
}