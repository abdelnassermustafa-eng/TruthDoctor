using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A2416LifecycleAtomicityTests
{
    private static readonly Dictionary<ushort, OpCode> Opcodes =
        BuildOpcodeMap();

    private static Dictionary<ushort, OpCode> BuildOpcodeMap()
    {
        var result = new Dictionary<ushort, OpCode>();

        foreach (FieldInfo field in typeof(OpCodes).GetFields(
            BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode opcode)
            {
                result[unchecked((ushort)opcode.Value)] = opcode;
            }
        }

        return result;
    }

    // Decode actual call instructions rather than searching raw bytes.
    // Searching token bytes alone can mistake unrelated instructions
    // or operand data for a method call.
    private static IReadOnlyList<MethodBase> CalledMethods(
        MethodInfo method)
    {
        byte[]? bytes = method.GetMethodBody()?.GetILAsByteArray();
        Assert.NotNull(bytes);

        var calls = new List<MethodBase>();
        int offset = 0;

        while (offset < bytes.Length)
        {
            ushort code = bytes[offset++];

            if (code == 0xFE)
            {
                Assert.True(offset < bytes.Length);
                code = (ushort)(0xFE00 | bytes[offset++]);
            }

            Assert.True(
                Opcodes.TryGetValue(code, out OpCode opcode),
                $"Unknown IL opcode: 0x{code:X4}");

            int operandOffset = offset;

            switch (opcode.OperandType)
            {
                case OperandType.InlineNone:
                    break;

                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    offset += 1;
                    break;

                case OperandType.InlineVar:
                    offset += 2;
                    break;

                case OperandType.InlineI:
                case OperandType.InlineBrTarget:
                case OperandType.ShortInlineR:
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                    offset += 4;
                    break;

                case OperandType.InlineI8:
                case OperandType.InlineR:
                    offset += 8;
                    break;

                case OperandType.InlineSwitch:
                    Assert.True(offset + 4 <= bytes.Length);
                    int count = BitConverter.ToInt32(bytes, offset);
                    Assert.True(count >= 0);
                    offset += 4 + checked(count * 4);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported IL operand: {opcode.OperandType}");
            }

            Assert.True(
                offset <= bytes.Length,
                "Unexpected truncated method IL.");

            if (opcode == OpCodes.Call || opcode == OpCodes.Callvirt)
            {
                int token = BitConverter.ToInt32(bytes, operandOffset);

                MethodBase? called = method.Module.ResolveMethod(
                    token,
                    method.DeclaringType?.GetGenericArguments(),
                    method.GetGenericArguments());

                Assert.NotNull(called);
                calls.Add(called);
            }
        }

        return calls;
    }

    private static MethodInfo LifecycleMethod(
        string name,
        params Type[] parameters)
    {
        MethodInfo? method = typeof(ProviderConnectionLifecycle)
            .GetMethod(name, parameters);

        Assert.NotNull(method);
        return method;
    }

    private static bool CallsCatalogMethod(
        MethodInfo source,
        string name)
    {
        foreach (MethodBase called in CalledMethods(source))
        {
            if (called.DeclaringType == typeof(ProviderConnectionCatalog)
                && called.Name == name)
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void TransitionMustUseRegistrationScopedExecution()
    {
        MethodInfo method = LifecycleMethod(
            "Transition",
            typeof(string),
            typeof(ProviderConnectionState));

        Assert.True(
            CallsCatalogMethod(method, "ExecuteForRegistration"),
            "Transition must execute through the catalog registration scope.");
    }

    [Fact]
    public void GetStateMustUseRegistrationScopedExecution()
    {
        MethodInfo method = LifecycleMethod(
            "GetState", typeof(string));

        Assert.True(
            CallsCatalogMethod(method, "ExecuteForRegistration"),
            "GetState must execute through the catalog registration scope.");
    }

    [Fact]
    public void LifecycleMustAvoidUnscopedGenerationLookup()
    {
        MethodInfo transition = LifecycleMethod(
            "Transition",
            typeof(string),
            typeof(ProviderConnectionState));

        MethodInfo getState = LifecycleMethod(
            "GetState", typeof(string));

        Assert.False(
            CallsCatalogMethod(
                transition, "GetRegistrationGeneration")
            || CallsCatalogMethod(
                getState, "GetRegistrationGeneration"),
            "Lifecycle methods must not use unscoped generation lookup.");
    }

    [Fact]
    public void ReRegistrationStillResetsVisibleState()
    {
        var catalog = new ProviderConnectionCatalog();

        var identity = new ProviderConnectionIdentity(
            "aws-primary", "aws");

        catalog.Register(identity);

        var lifecycle = new ProviderConnectionLifecycle(catalog);

        lifecycle.Transition(
            "aws-primary",
            ProviderConnectionState.Configured);

        Assert.True(catalog.Unregister("aws-primary"));

        // The same identity instance is deliberately reused.
        catalog.Register(identity);

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            lifecycle.GetState("aws-primary"));
    }

    [Fact]
    public void InvalidDirectTransitionStillRejects()
    {
        var catalog = new ProviderConnectionCatalog();

        catalog.Register(new ProviderConnectionIdentity(
            "aws-primary", "aws"));

        var lifecycle = new ProviderConnectionLifecycle(catalog);

        Assert.Throws<InvalidOperationException>(
            () => lifecycle.Transition(
                "aws-primary",
                ProviderConnectionState.Connected));

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            lifecycle.GetState("aws-primary"));
    }
}
