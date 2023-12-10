using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NonSucking.Framework.Serialization.Advanced;

internal class BaseGenerator
{
    public BaseGenerator(BaseGeneratorContext currentContext)
    {
        CurrentContext = currentContext;
    }
    
    public void PushContext(BaseGeneratorContext context)
    {
        Contexts.Push(context);
        CurrentContext = context;
    }

    public BaseGeneratorContext PopContext()
    {
        var popped = Contexts.Pop();
        CurrentContext = Contexts.Peek();
        return popped;
    }
    public void EmitCall(MethodInfo method)
    {
        Il.Emit(method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, method);
    }
    public void EmitIfInverted(OpCode invertedBranch, Action<BaseGenerator>? ifScope, Action<BaseGenerator>? elseScope)
    {
        EmitIf(InvertBranch(invertedBranch), elseScope, ifScope);
    }

    public void EmitIf(OpCode branchCode, Action<BaseGenerator>? ifScope,
        Action<BaseGenerator>? elseScope)
    {
        var elseLabel = elseScope is null ? (Label?)null : Il.DefineLabel();
        var endLabel = Il.DefineLabel();
        Il.Emit(branchCode, elseLabel ?? endLabel);
        ifScope?.Invoke(this);
        if (elseLabel is not null)
        {
            if (ifScope != null)
                Il.Emit(OpCodes.Br, endLabel);
            Il.MarkLabel(elseLabel.Value);
            elseScope?.Invoke(this);
        }
        Il.MarkLabel(endLabel);
    }
    public void EmitIf(Action<BaseGenerator>? elseScope, params (OpCode branchCode, Action<BaseGenerator> check, Action<BaseGenerator> body)[] ifs)
    {
        if (ifs.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ifs));
        }
        
        var nested = elseScope;

        for (int i = ifs.Length - 1; i >= 0; i--)
        {
            var (elifBranchCode, elifCheck, elifBody) = ifs[i];
            var nestedCopy = nested;
            nested = gen =>
                     {
                         elifCheck(gen);
                         gen.EmitIf(elifBranchCode, elifBody, nestedCopy);
                     };
        }

        nested!(this);
    }
    public void EmitIf(OpCode branchCode, Action<BaseGenerator>? ifScope,
        Action<BaseGenerator>? elseScope, params (OpCode branchCode, Action<BaseGenerator> check, Action<BaseGenerator> body)[] elseIfs)
    {
        var nested = elseScope;

        for (int i = elseIfs.Length - 1; i >= 0; i--)
        {
            var (elifBranchCode, elifCheck, elifBody) = elseIfs[i];
            var nestedCopy = nested;
            nested = gen =>
                     {
                         elifCheck(gen);
                         gen.EmitIf(elifBranchCode, elifBody, nestedCopy);
                     };
        }
        
        EmitIf(branchCode, ifScope,
            gen =>
            {
                nested?.Invoke(gen);
            }
        );
    }

    public void EmitFor(Func<BaseGenerator, LocalBuilder?> init, Action<BaseGenerator, LocalBuilder?> check, Action<BaseGenerator, LocalBuilder?> increment, Action<BaseGenerator, Label, Label, LocalBuilder?> body)
    {
        var forLoopStart = Il.DefineLabel();
        var forLoopEnd = Il.DefineLabel();
        var variable = init(this);
        Il.MarkLabel(forLoopStart);
        check(this, variable);
        Il.Emit(OpCodes.Brfalse, forLoopEnd);
        body(this, forLoopStart, forLoopEnd, variable);
        increment(this, variable);
        Il.Emit(OpCodes.Br, forLoopStart);
        Il.MarkLabel(forLoopEnd);
    }

    public void EmitWhile(Action<BaseGenerator> check, Action<BaseGenerator, Label, Label> body)
    {
        var loopStart = Il.DefineLabel();
        var loopEnd = Il.DefineLabel();
        Il.MarkLabel(loopStart);
        check(this);
        Il.Emit(OpCodes.Brfalse, loopEnd);
        body(this, loopStart, loopEnd);
        Il.Emit(OpCodes.Br, loopStart);
        Il.MarkLabel(loopEnd);
    }

    public void EmitDo(Action<BaseGenerator> check, Action<BaseGenerator, Label, Label> body)
    {
        var loopStart = Il.DefineLabel();
        var loopEnd = Il.DefineLabel();
        Il.MarkLabel(loopStart);
        body(this, loopStart, loopEnd);
        check(this);
        Il.Emit(OpCodes.Brtrue, loopStart);
        Il.MarkLabel(loopEnd);
    }

    public void EmitTryCatchFinally(Action<BaseGenerator, Label> body, Action<BaseGenerator, Label>? finalBlock, params (Type? filter, Action<BaseGenerator, Label> bodyGenerator)[]? exceptionBlocks)
    {
        var exBlock = Il.BeginExceptionBlock();
        var exitLabel = Il.DefineLabel();
        body(this, exBlock);
        if (exceptionBlocks is not null && exceptionBlocks.Length > 0)
        {
            if (exceptionBlocks.Length > 1)
                Il.BeginExceptFilterBlock();
            foreach (var (filter, bodyGenerator) in exceptionBlocks)
            {
                Il.BeginCatchBlock(filter);
                bodyGenerator(this, exBlock);
            }
            if (finalBlock is not null)
                Il.BeginFinallyBlock();
        }
        else
            Il.BeginFinallyBlock();

        finalBlock?.Invoke(this, exitLabel);
        
        Il.MarkLabel(exitLabel);
        Il.EndExceptionBlock();
    }

    public void EmitIncrement()
    {
        Il.Emit(OpCodes.Ldc_I4_1);
        Il.Emit(OpCodes.Add);
    }

    public void EmitIncrement(LocalBuilder var)
    {
        Il.Emit(OpCodes.Ldloc, var);
        EmitIncrement();
        Il.Emit(OpCodes.Stloc, var);
    }
    
    public void EmitForEach(Type enumerableType, Action<BaseGenerator> getEnumerable, Action<BaseGenerator, Label, Label, LocalBuilder> body)
    {
        var getEnumerator = Helper.GetMethodIncludingInterfaces(enumerableType, "GetEnumerator", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (getEnumerator is null)
            throw new ArgumentException($"Not a valid enumerable type(no GetEnumerator found): '{enumerableType.FullName}'");
        var enumeratorType = getEnumerator.ReturnType;
        var moveNext = Helper.GetMethodIncludingInterfaces(enumeratorType, "MoveNext", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (moveNext is null || moveNext.ReturnType != typeof(bool))
            throw new ArgumentException($"Not a valid enumerator type(no valid MoveNext returning bool found): '{enumeratorType.FullName}'");
        var getCurrent = Helper.GetPropertyIncludingInterfaces(enumeratorType, "Current", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)?.GetMethod;
        if (getCurrent is null)
            throw new ArgumentException(
                $"Not a valid enumerator(no valid Current get method found): '{enumeratorType.FullName}'");
        var dispose = Helper.GetMethodIncludingInterfaces(enumeratorType, "Dispose", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (dispose is null)
            throw new ArgumentException(
                $"Not a valid enumerator(no valid Dispose method found): '{enumeratorType.FullName}'");
        var enumeratorVariable = Il.DeclareLocal(enumeratorType);
        var currentVariable = Il.DeclareLocal(getCurrent.ReturnType);
        getEnumerable(this);
        Il.Emit(OpCodes.Callvirt, getEnumerator);
        Il.Emit(OpCodes.Stloc, enumeratorVariable);
        EmitTryCatchFinally(
            (_, _) =>
            {
                EmitWhile(
                    gen =>
                    {
                        gen.EmitLoadLocRef(enumeratorVariable);
                        gen.Il.Emit(OpCodes.Callvirt, moveNext);
                    },
                    (gen, loopStart, loopEnd) =>
                    {
                        gen.EmitLoadLocRef(enumeratorVariable);
                        gen.Il.Emit(OpCodes.Callvirt, getCurrent);
                        gen.Il.Emit(OpCodes.Stloc, currentVariable);
                        body(gen, loopStart, loopEnd, currentVariable);
                    }
                );
            },
            (gen, exitLabel) =>
            {
                // if (!enumeratorVariable.LocalType.IsValueType)
                // {
                //     gen.EmitLoadLocRef(enumeratorVariable);
                //     gen.IL.Emit(OpCodes.Brfalse, exitLabel);
                // }
                //
                // gen.EmitLoadLocRef(enumeratorVariable);
                // gen.IL.Emit(OpCodes.Callvirt, dispose);
            });
    }

    private void EmitLoadLocRef(LocalBuilder var)
    {
        Il.Emit(var.LocalType.IsValueType ? OpCodes.Ldloca : OpCodes.Ldloc, var);
    }
    
    public static MethodInfo GetDelegateInvoker(Type type)
    {
        var invoke = type.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance);
        if (invoke is null)
            throw new ArgumentException($"No valid invoke method found for type: {type.FullName}");
        return invoke;
    }
    private static OpCode InvertBranch(OpCode code)
    {
        if (code == OpCodes.Brfalse_S)
            return OpCodes.Brtrue_S;
        if (code == OpCodes.Brfalse)
            return OpCodes.Brtrue;
        if (code == OpCodes.Brtrue_S)
            return OpCodes.Brfalse_S;
        if (code == OpCodes.Brtrue)
            return OpCodes.Brfalse;
        throw new ArgumentOutOfRangeException(nameof(code));
    }
    
    public Dictionary<BaseGeneratorContext.TypeContext, BaseGeneratorContext> ContextMap { get; } = new();
    public BaseGeneratorContext CurrentContext { get; private set; }
    public ILGenerator Il => CurrentContext.Il;
    public Stack<BaseGeneratorContext> Contexts { get; } = new();
    public bool IsTopLevel => CurrentContext.IsTopLevel;

}